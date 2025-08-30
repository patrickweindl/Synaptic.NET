using System.Globalization;
using System.Text;
using OpenAI.Chat;
using Synaptic.NET.Core;
using Synaptic.NET.Domain;
using Synaptic.NET.Domain.Helpers;
using Synaptic.NET.Domain.Resources;
using Synaptic.NET.OpenAI;
using Synaptic.NET.OpenAI.Clients;

namespace Synaptic.NET.Augmentation.Services;

public class WeightedMemoryStoreRouter : IMemoryStoreRouter
{
    private readonly GptClientBase _chatClient;
    private readonly IMetricsCollectorProvider _metricsCollectorProvider;
    private readonly ICurrentUserService _currentUserService;

    public WeightedMemoryStoreRouter(ICurrentUserService currentUserService, IMetricsCollectorProvider metricsCollectorProvider, OpenAiClientFactory gptClientFactory, SynapticServerSettings settings)
    {
        _currentUserService = currentUserService;
        _metricsCollectorProvider = metricsCollectorProvider;
        _chatClient = gptClientFactory.GetClient(settings.OpenAiMemoryRoutingModel);
    }

    public async Task<List<MemoryStoreRoutingResult>> RankStoresAsync(string query, IEnumerable<MemoryStore> availableStores)
    {
        var storeChunks = availableStores.Chunk(3);
        List<MemoryStoreRoutingResult> ranking = new();
        await Parallel.ForEachAsync(storeChunks, new ParallelOptions { MaxDegreeOfParallelism = 16 }, async (chunk, token) =>
        {
            StringBuilder availableStoresStringBuilder = new();
            foreach (var store in chunk)
            {
                availableStoresStringBuilder.AppendLine("---");
                var storeMemories = store.Memories;
                var tags = storeMemories.SelectMany(m => m.Tags).Distinct().ToList();
                availableStoresStringBuilder.AppendLine($"Store: {store.Title} Description: {store.Description} Tags in Store: {string.Join(",", tags)}");
                availableStoresStringBuilder.AppendLine("---");
            }

            string systemPrompt = PromptTemplates.GetStoreRouterSystemPrompt();
            string userPrompt = PromptTemplates.GetStoreRouterUserPrompt(query, availableStoresStringBuilder.ToString());

            List<ChatMessage> messages = new()
            {
                ChatMessage.CreateSystemMessage(systemPrompt),
                ChatMessage.CreateUserMessage(userPrompt)
            };

            DateTime start = DateTime.UtcNow;
            Log.Information("[StoreRouter] Calling model to rank stores. Input query: {Query} at {Start}", query, start);
            var response = await _chatClient.CompleteChatAsync(messages, cancellationToken: token);
            _metricsCollectorProvider.TokenMetrics.IncrementTokenCountsFromChatCompletion(_currentUserService.GetCurrentUser(), "Store Ranking", response.Value);
            string suggestion = response.Value.Content[0].Text ?? string.Empty;
            Log.Information("[StoreRouter] Model call finished after {Duration:c}. Output: {Output}", DateTime.UtcNow - start, suggestion);
            string[] ordered = suggestion.Split('%', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

            foreach (string result in ordered)
            {
                string[] splits = result.Split("__");
                string identifier = splits.First();
                if (!Guid.TryParse(identifier, out var guid))
                {
                    continue;
                }
                double weight = double.Parse(splits.Last(), NumberStyles.Any, CultureInfo.InvariantCulture);
                ranking.Add(new MemoryStoreRoutingResult(guid, weight));
            }
        });
        ranking = ranking.OrderByDescending(r => r.Relevance).ToList();

        return ranking;
    }

    public async Task<MemoryStoreRoutingResult> RouteMemoryToStoreAsync(Memory memory, IEnumerable<MemoryStore> availableStores)
    {
        List<MemoryStoreRoutingResult> ranking = new();
        var storeChunks = availableStores.Chunk(3);
        await Parallel.ForEachAsync(storeChunks, new ParallelOptions { MaxDegreeOfParallelism = 16 }, async (chunk, token) =>
        {
            StringBuilder availableStoresStringBuilder = new();
            foreach (var store in chunk)
            {
                availableStoresStringBuilder.AppendLine("---");
                var storeMemories = store.Memories;
                var tags = storeMemories.SelectMany(m => m.Tags).Distinct().ToList();
                availableStoresStringBuilder.AppendLine(
                    $"Store: {store.Title} Description: {store.Description} Tags in Store: {string.Join(",", tags)}");
                availableStoresStringBuilder.AppendLine("---");
            }

            var systemPrompt = PromptTemplates.GetMemoryRouterSystemPrompt();
            var userPrompt =
                PromptTemplates.GetMemoryRouterUserPrompt(memory.Identifier.ToString(), memory.Content, availableStoresStringBuilder.ToString());

            List<ChatMessage> messages = new()
            {
                ChatMessage.CreateSystemMessage(systemPrompt), ChatMessage.CreateUserMessage(userPrompt)
            };

            DateTime start = DateTime.UtcNow;
            Log.Information("[StoreRouter] Calling model to route memory to stores.");
            var response = await _chatClient.CompleteChatAsync(messages, cancellationToken: token);
            _metricsCollectorProvider.TokenMetrics.IncrementTokenCountsFromChatCompletion(_currentUserService.GetCurrentUser(), "Store Routing", response.Value);
            string suggestion = response.Value.Content[0].Text ?? string.Empty;
            Log.Information("[StoreRouter] Model call finished after {Duration:c}. Output: {Output}", DateTime.UtcNow - start,
                suggestion);
            var ordered = suggestion.Split('%', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

            foreach (string result in ordered)
            {
                string[] splits = result.Split("__");
                string identifier = splits.First();
                if (!Guid.TryParse(identifier, out var guid))
                {
                    continue;
                }
                double weight = double.Parse(splits.Last(), NumberStyles.Any, CultureInfo.InvariantCulture);
                ranking.Add(new MemoryStoreRoutingResult(guid, weight));
            }

            ranking = ranking.OrderByDescending(r => r.Relevance).ToList();

            if (ranking.Count == 0)
            {
                throw new InvalidOperationException("Could not route memory to any store. The model did not return any results.");
            }

            Log.Information("[StoreRouter] Memory routed to store with weight {Weight}.", ranking[0].Relevance);
            ranking.AddRange(ranking);
        });
        return new MemoryStoreRoutingResult(ranking[0].Identifier, ranking[0].Relevance);
    }
}
