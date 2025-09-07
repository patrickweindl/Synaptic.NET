using System.ClientModel;
using System.Globalization;
using OpenAI.Chat;
using Synaptic.NET.Core;
using Synaptic.NET.Domain.Abstractions.Augmentation;
using Synaptic.NET.Domain.Abstractions.Management;
using Synaptic.NET.Domain.Resources.Configuration;
using Synaptic.NET.Domain.Resources.Storage;
using Synaptic.NET.Domain.StructuredResponses;
using Synaptic.NET.OpenAI;
using Synaptic.NET.OpenAI.Clients;
using Synaptic.NET.OpenAI.StructuredResponses;

namespace Synaptic.NET.Augmentation.Services;

public class MemoryAugmentationService : IMemoryAugmentationService
{
    private readonly GptClientBase _client;
    private readonly IMetricsCollectorProvider _metricsCollectorProvider;
    private readonly ICurrentUserService _currentUserService;

    public MemoryAugmentationService(
        SynapticServerSettings settings,
        OpenAiClientFactory clientFactory,
        ICurrentUserService currentUserService,
        IMetricsCollectorProvider metricsCollectorProvider)
    {
        _currentUserService = currentUserService;
        _client = clientFactory.GetClient(settings.OpenAiMemoryAugmentationModel);
        _metricsCollectorProvider = metricsCollectorProvider;
    }

    public async Task<string> GenerateMemoryDescriptionAsync(string memoryContent)
    {
        List<ChatMessage> messages = new()
        {
            ChatMessage.CreateSystemMessage(PromptTemplates.GetMemorySummarySystemPrompt()),
            ChatMessage.CreateUserMessage(memoryContent)
        };

        DateTime start = DateTime.UtcNow;
        Log.Information("[Augmentation] Calling model for memory summary. Input: {Input} at {Start}", memoryContent, start);
        var structuredResponseSchema = CompletionOptionsHelper.CreateStructuredResponseOptions<MemorySummary>();
        ClientResult<ChatCompletion> chatCompletion = await _client.CompleteChatAsync(messages, structuredResponseSchema);

        MemorySummary? summary = CompletionOptionsHelper.ParseModelResponse<MemorySummary>(chatCompletion);
        _metricsCollectorProvider.TokenMetrics.IncrementTokenCountsFromChatCompletion(_currentUserService.GetCurrentUser(), "Memory Summarization",
            chatCompletion.Value);
        string output = summary?.Summary ?? string.Empty;
        Log.Information(
            "[Augmentation] Model call finished after {Duration:c}. Output: {Output}",
            DateTime.UtcNow - start, output);

        return output;
    }

    public async Task<string> GenerateStoreDescriptionAsync(string storeIdentifier, List<string> records)
    {
        string memoryString = string.Join(Environment.NewLine, records.Select(m => m));

        if (_client.GetEncoder().CountTokens(memoryString) > MaxTokensForSummarization)
        {
            memoryString = memoryString.Substring(0, MaxTokensForSummarization);
        }
        var systemPrompt = PromptTemplates.GetStoreSummarySystemPrompt();
        var userPrompt = PromptTemplates.GetStoreSummaryUserPrompt(storeIdentifier, memoryString);

        List<ChatMessage> messages = new()
        {
            ChatMessage.CreateSystemMessage(systemPrompt),
            ChatMessage.CreateUserMessage(userPrompt)
        };

        DateTime start = DateTime.UtcNow;
        Log.Information("[Augmentation] Calling model for memory store summary. Input: {StoreIdentifier} at {Start}",
            storeIdentifier, start);
        var response = await _client.CompleteChatAsync(messages);
        _metricsCollectorProvider.TokenMetrics.IncrementTokenCountsFromChatCompletion(_currentUserService.GetCurrentUser(), "Store Summary", response.Value);
        string output = response.Value.Content[0].Text.Trim();
        Log.Information(
            "[Augmentation] Model call finished after {Duration:c}. Output: {Output}",
            DateTime.UtcNow - start, output);

        return output;
    }

    private const int MaxTokensForSummarization = 100000;
    public async Task<string> GenerateStoreDescriptionAsync(string storeIdentifier, List<Memory> records)
    {
        string memoryString = string.Join(Environment.NewLine, records.Select(m => m.Description));

        if (_client.GetEncoder().CountTokens(memoryString) > MaxTokensForSummarization)
        {
            memoryString = memoryString.Substring(0, MaxTokensForSummarization);
        }
        var systemPrompt = PromptTemplates.GetStoreSummarySystemPrompt();
        var userPrompt = PromptTemplates.GetStoreSummaryUserPrompt(storeIdentifier, memoryString);

        List<ChatMessage> messages = new()
        {
            ChatMessage.CreateSystemMessage(systemPrompt),
            ChatMessage.CreateUserMessage(userPrompt)
        };

        DateTime start = DateTime.UtcNow;
        Log.Information("[Augmentation] Calling model for memory store summary. Input: {StoreIdentifier} at {Start}",
            storeIdentifier, start);
        var response = await _client.CompleteChatAsync(messages);
        _metricsCollectorProvider.TokenMetrics.IncrementTokenCountsFromChatCompletion(_currentUserService.GetCurrentUser(), "Store Summary", response.Value);
        string output = response.Value.Content[0].Text.Trim();
        Log.Information(
            "[Augmentation] Model call finished after {Duration:c}. Output: {Output}",
            DateTime.UtcNow - start, output);

        return output;
    }

    public async Task<string> GenerateStoreTitleAsync(string storeDescription, List<Memory> memories)
    {
        string memoryString = string.Join(Environment.NewLine, memories.Select(m => m.Description));

        if (_client.GetEncoder().CountTokens(memoryString) > MaxTokensForSummarization)
        {
            memoryString = memoryString.Substring(0, MaxTokensForSummarization);
        }
        var systemPrompt = PromptTemplates.GetStoreTitleSystemPrompt();
        var userPrompt = PromptTemplates.GetStoreTitleUserPrompt(storeDescription, memoryString);

        List<ChatMessage> messages = new()
        {
            ChatMessage.CreateSystemMessage(systemPrompt),
            ChatMessage.CreateUserMessage(userPrompt)
        };

        DateTime start = DateTime.UtcNow;
        Log.Information("[Augmentation] Calling model for memory store title. Input: {StoreDescription} at {Start}",
            storeDescription, start);
        var response = await _client.CompleteChatAsync(messages);
        _metricsCollectorProvider.TokenMetrics.IncrementTokenCountsFromChatCompletion(_currentUserService.GetCurrentUser(), "Store Summary", response.Value);
        string output = response.Value.Content[0].Text.Trim();
        Log.Information(
            "[Augmentation] Model call finished after {Duration:c}. Output: {Output}",
            DateTime.UtcNow - start, output);

        return output;
    }

    public async Task<IEnumerable<(Guid, double)>> RankMemoriesAsync(string query, MemoryStore store, CancellationToken cancellationToken = default)
    {
        var memoryChunks = store.Memories.Chunk(20);
        var rankTasks = memoryChunks.Select(async chunk => await RankMemoryChunkAsync(query, store, chunk, cancellationToken));
        var result = await Task.WhenAll(rankTasks);
        return result.SelectMany(r => r);
    }

    private async Task<IEnumerable<(Guid, double)>> RankMemoryChunkAsync(string query, MemoryStore store, IEnumerable<Memory> chunk, CancellationToken token = default)
    {
        var ranking = new List<(Guid memoryId, double relevance)>();
        string memoryDescriptions = string.Join("\n", chunk.Select(r =>
            $"Identifier: {r.Identifier} | Description: {r.Description} | Date: {r.CreatedAt:yyyy-MM-dd} | Tags: {r.Tags} | Content: {r.Content}"));

        string systemPrompt = PromptTemplates.GetVectorSearchSystemPrompt();
        string userPrompt = PromptTemplates.GetVectorSearchUserPrompt(query, store.Description, memoryDescriptions);

        var messages = new List<ChatMessage>
        {
            ChatMessage.CreateSystemMessage(systemPrompt),
            ChatMessage.CreateUserMessage(userPrompt)
        };
        var response = await _client.CompleteChatAsync(messages, cancellationToken: token);
        _metricsCollectorProvider.TokenMetrics.IncrementTokenCountsFromChatCompletion(_currentUserService.GetCurrentUser(), "Augmented Search", response.Value);
        string suggestion = response.Value.Content[0].Text ?? string.Empty;

        if (string.IsNullOrEmpty(suggestion))
        {
            return ranking;
        }

        string[] orderedSuggestions = suggestion.Split('%', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        foreach (string result in orderedSuggestions)
        {
            try
            {
                string[] splits = result.Split("__");
                string identifier = splits[0];
                double weight = double.Parse(splits[1], NumberStyles.Any, CultureInfo.InvariantCulture);
                Guid id = Guid.Parse(identifier);
                ranking.Add((id, weight));
            }
            catch (Exception)
            {
                Log.Error("Failed to parse a model response for augmented search!.");
            }
        }
        return ranking;
    }
}
