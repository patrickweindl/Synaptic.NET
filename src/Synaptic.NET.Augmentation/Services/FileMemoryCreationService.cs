using OpenAI.Chat;
using Synaptic.NET.Core;
using Synaptic.NET.Domain.Abstractions.Augmentation;
using Synaptic.NET.Domain.Abstractions.Management;
using Synaptic.NET.Domain.Resources;
using Synaptic.NET.Domain.Resources.Configuration;
using Synaptic.NET.Domain.Resources.Management;
using Synaptic.NET.Domain.Scopes;
using Synaptic.NET.Domain.StructuredResponses;
using Synaptic.NET.OpenAI;
using Synaptic.NET.OpenAI.Clients;
using Synaptic.NET.OpenAI.StructuredResponses;

namespace Synaptic.NET.Augmentation.Services;

public class FileMemoryCreationService : IFileMemoryCreationService
{
    private readonly GptClientBase _gptClient;
    private readonly IMetricsCollectorProvider _metricsCollectorProvider;
    private readonly ICurrentUserService _currentUserService;
    public FileMemoryCreationService(ICurrentUserService currentUserService, IMetricsCollectorProvider metricsCollectorProvider, OpenAiClientFactory openAiClientFactory, SynapticServerSettings serverSettings)
    {
        _gptClient = openAiClientFactory.GetClient(serverSettings.OpenAiSettings.RagCreationModel);
        _metricsCollectorProvider = metricsCollectorProvider;
        _currentUserService = currentUserService;
    }
    public Task<FileProcessor> GetFileProcessor(ScopeFactory scopeFactory, User user)
    {
        return Task.FromResult(new FileProcessor(scopeFactory, user));
    }

    public async Task<MemorySummaries> CreateMemoriesFromPdfFileAsync(string fileName, string base64Pdf)
    {
        List<ChatMessage> messages =
        [
            ChatMessage.CreateSystemMessage(PromptTemplates.GetFileProcessingSystemPrompt()),
            ChatMessage.CreateUserMessage(ChatMessageContentPart.CreateFilePart(
                BinaryData.FromBytes(Convert.FromBase64String(base64Pdf)),
                "application/pdf", fileName))
        ];
        DateTime start = DateTime.UtcNow;
        var structuredResponse = CompletionOptionsHelper.CreateStructuredResponseOptions<MemorySummaries>();
        structuredResponse.Temperature = 0.2f;
        Log.Debug("[File Memory Creation Service] Acquiring model response...");
        var response = await _gptClient.CompleteChatAsync(messages, options: structuredResponse);
        await _metricsCollectorProvider.TokenMetrics.IncrementTokenCountsFromChatCompletionAsync(await _currentUserService.GetCurrentUserAsync(), "PDF Processing", response.Value);
        DateTime responseAcquisition = DateTime.Now;

        Log.Debug($"[File Memory Creation Service] LLM Processing complete after {(responseAcquisition - start).TotalSeconds} seconds.");

        var summaries = CompletionOptionsHelper.ParseModelResponse<MemorySummaries>(response);
        if (summaries == null)
        {
            Log.Error("[File Memory Creation Service] Failed to parse model response to memory summaries!");
            return new MemorySummaries { Summaries = new List<MemorySummary>() };
        }
        DateTime parsingFinish = DateTime.Now;
        Log.Debug($"[File Memory Creation Service] JSON successfully parsed after {(parsingFinish - responseAcquisition).TotalSeconds} seconds.");
        return summaries;
    }


    public async Task<MemorySummaries> CreateMemoriesFromBase64String(string fileName, string base64String)
    {
        List<ChatMessage> messages =
        [
            ChatMessage.CreateSystemMessage(PromptTemplates.GetFileProcessingSystemPrompt()),
            ChatMessage.CreateUserMessage(ChatMessageContentPart.CreateTextPart(base64String))
        ];

        var structuredResponse = CompletionOptionsHelper.CreateStructuredResponseOptions<MemorySummaries>();
        DateTime start = DateTime.UtcNow;
        var response = await _gptClient.CompleteChatAsync(messages, options: structuredResponse);
        await _metricsCollectorProvider.TokenMetrics.IncrementTokenCountsFromChatCompletionAsync(await _currentUserService.GetCurrentUserAsync(), "File Processing", response.Value);
        DateTime responseAcquisition = DateTime.Now;
        Log.Information($"Processing complete after {(responseAcquisition - start).TotalSeconds} seconds.");

        var summaries = CompletionOptionsHelper.ParseModelResponse<MemorySummaries>(response);
        if (summaries == null)
        {
            Log.Error("Failed to parse model response!");
            return new MemorySummaries { Summaries = new List<MemorySummary>() };
        }

        DateTime parsingFinish = DateTime.Now;
        Log.Information($"JSON successfully parsed after {(parsingFinish - responseAcquisition).TotalSeconds} seconds.");
        return summaries;
    }
}
