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
        _client = openAiClientFactory.GetClient(settings.OpenAiSettings.MemoryAugmentationModel);
    }


    public async IAsyncEnumerable<MemorySearchResult> Rerank(IAsyncEnumerable<MemorySearchResult> results)
    {
        await foreach (var result in results)
        {
            // Rerank.
            yield return result;
        }
    }

#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously
    public async IAsyncEnumerable<MemorySearchResult> Rerank(IEnumerable<MemorySearchResult> results)
#pragma warning restore CS1998 // Async method lacks 'await' operators and will run synchronously
    {
        foreach (var result in results)
        {
            // Rerank.
            yield return result;
        }
    }
}
