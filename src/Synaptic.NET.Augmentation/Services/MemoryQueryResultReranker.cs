using Synaptic.NET.Domain.Abstractions.Augmentation;
using Synaptic.NET.Domain.Resources.Configuration;
using Synaptic.NET.Domain.Resources.Storage;
using Synaptic.NET.OpenAI;
using Synaptic.NET.OpenAI.Clients;

namespace Synaptic.NET.Augmentation.Services;

public class MemoryQueryResultReranker : IMemoryQueryResultReranker
{
    private GptClientBase _client;
    public MemoryQueryResultReranker(OpenAiClientFactory openAiClientFactory, SynapticServerSettings settings)
    {
        _client = openAiClientFactory.GetClient(settings.OpenAiAugmentedSearchModel);
    }

    public async Task<IEnumerable<MemorySearchResult>> Rerank(IReadOnlyList<MemorySearchResult> results)
    {
        return results;
    }

    public async Task<IEnumerable<MemorySearchResult>> Rerank(IEnumerable<MemorySearchResult> results)
    {
        return results;
    }
}
