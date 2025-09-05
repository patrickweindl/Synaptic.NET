using Microsoft.SemanticKernel.Memory;
using Synaptic.NET.Domain.Abstractions.Augmentation;
using Synaptic.NET.Domain.Abstractions.Storage;
using Synaptic.NET.Domain.Resources.Configuration;
using Synaptic.NET.OpenAI;
using Synaptic.NET.OpenAI.Clients;

namespace Synaptic.NET.Augmentation.Services;

public class MemoryQueryResultReranker : IMemoryQueryResultReranker
{
    private GptClientBase _client;
    private IMemoryProvider _memoryProvider;
    public MemoryQueryResultReranker(OpenAiClientFactory openAiClientFactory, SynapticServerSettings settings, IMemoryProvider memoryProvider)
    {
        _client = openAiClientFactory.GetClient(settings.OpenAiAugmentedSearchModel);
        _memoryProvider = memoryProvider;
    }

    public IEnumerable<MemoryQueryResult> Rerank(IReadOnlyList<MemoryQueryResult> results)
    {
        return results;
    }
}
