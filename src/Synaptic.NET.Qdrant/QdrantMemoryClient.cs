using Microsoft.Extensions.AI;
using Microsoft.Extensions.VectorData;
using Microsoft.SemanticKernel.Connectors.Qdrant;
using OpenAI;
using OpenAI.Embeddings;
using Qdrant.Client;
using Synaptic.NET.Domain.Abstractions.Augmentation;
using Synaptic.NET.Domain.Abstractions.Management;
using Synaptic.NET.Domain.Resources.Configuration;
using Synaptic.NET.Domain.Resources.Storage;

namespace Synaptic.NET.Qdrant;

public class QdrantMemoryClient
{
    private readonly EmbeddingClient _embeddingGenerator;
    private readonly QdrantClient _client;
    private readonly QdrantVectorStore _store;
    private readonly IMemoryAugmentationService _memoryAugmentationService;
    public QdrantMemoryClient(SynapticServerSettings settings, IMemoryAugmentationService augmentationService)
    {
        _embeddingGenerator = new OpenAIClient(settings.OpenAiApiKey)
            .GetEmbeddingClient(settings.OpenAiEmbeddingModel);

        string ip;
        int port;
        if (settings.QdrantServerUrl.Contains(":"))
        {
            var url = Uri.TryCreate(settings.QdrantServerUrl, UriKind.Absolute, out var uri) ? uri : throw new InvalidOperationException();

            ip = $"{uri.Host}";
            port = url.Port;
        }
        else
        {
            ip = settings.QdrantServerUrl;
            port = 6334;
        }

        _client = new QdrantClient(ip, port);
        _ = Task.Run(async () => await _client.HealthAsync()).Result;
        _store = new QdrantVectorStore(
            _client,
            false, new QdrantVectorStoreOptions
            {
                EmbeddingGenerator = _embeddingGenerator.AsIEmbeddingGenerator(),
                HasNamedVectors = true
            });

        _memoryAugmentationService = augmentationService;
    }

    public async Task<IEnumerable<MemorySearchResult>> SearchAsync(string query, int top, double relevanceThreshold, Guid userIdentifier, CancellationToken cancellationToken = default)
    {
        string userId = userIdentifier.ToString();
        using var collection = _store.GetCollection<Guid, VectorMemory>(userIdentifier.ToString());
        await collection.EnsureCollectionExistsAsync(cancellationToken);
        var contentResult = await collection.SearchAsync(query, top, options: new VectorSearchOptions<VectorMemory>
        {
            VectorProperty = m => m.ContentEmbedding,
            Filter = m => m.VectorOwnerIdentifier == userId
        },cancellationToken: cancellationToken).ToListAsync(cancellationToken);


        var titleResult = await collection.SearchAsync(query, top, options: new VectorSearchOptions<VectorMemory>
        {
            VectorProperty = m => m.TitleEmbedding,
            Filter = m => m.VectorOwnerIdentifier == userId
        }, cancellationToken: cancellationToken).ToListAsync(cancellationToken);

        var descriptionResult = await collection.SearchAsync(query, top, options: new VectorSearchOptions<VectorMemory>
        {
            VectorProperty = m => m.DescriptionEmbedding,
            Filter = m => m.VectorOwnerIdentifier == userId
        }, cancellationToken: cancellationToken).ToListAsync(cancellationToken);


        return contentResult.Concat(titleResult).Concat(descriptionResult)
            .Where(v => v.Score > relevanceThreshold)
            .OrderByDescending(r => r.Score)
            .DistinctBy(v => v.Record.Identifier)
            .Take(top)
            .Select(v => new MemorySearchResult { Memory = v.Record, Relevance = v.Score ?? 0 });
    }

    public async Task<IEnumerable<MemorySearchResult>> SearchInStoreAsync(string query, int top, double relevanceThreshold, Guid collectionIdentifier, Guid userIdentifier, CancellationToken cancellationToken = default)
    {
        using var collection = _store.GetCollection<Guid, VectorMemory>(userIdentifier.ToString());
        await collection.EnsureCollectionExistsAsync(cancellationToken);

        var contentResult = await collection.SearchAsync(query, top, options: new VectorSearchOptions<VectorMemory>
        {
            VectorProperty = m => m.ContentEmbedding,
            Filter = m => m.VectorStoreIdentifier == collectionIdentifier.ToString() && m.VectorOwnerIdentifier == userIdentifier.ToString()
        },cancellationToken: cancellationToken).ToListAsync(cancellationToken);

        var titleResult = await collection.SearchAsync(query, top, options: new VectorSearchOptions<VectorMemory>
        {
            VectorProperty = m => m.TitleEmbedding,
            Filter = m => m.VectorStoreIdentifier == collectionIdentifier.ToString() && m.VectorOwnerIdentifier == userIdentifier.ToString()
        }, cancellationToken: cancellationToken).ToListAsync(cancellationToken);

        var descriptionResult = await collection.SearchAsync(query, top, options: new VectorSearchOptions<VectorMemory>
        {
            VectorProperty = m => m.DescriptionEmbedding,
            Filter = m => m.VectorStoreIdentifier == collectionIdentifier.ToString() && m.VectorOwnerIdentifier == userIdentifier.ToString()
        }, cancellationToken: cancellationToken).ToListAsync(cancellationToken);


        return contentResult.Concat(titleResult).Concat(descriptionResult)
            .Where(v => v.Score > relevanceThreshold)
            .OrderBy(r => r.Score)
            .Take(top)
            .Select(v => new MemorySearchResult { Memory = v.Record, Relevance = v.Score ?? 0 });
    }

    public async Task DeleteMemoryStoreAsync(Guid userIdentifier, MemoryStore memoryStore, CancellationToken cancellationToken = default)
    {
        var deletionTasks = memoryStore.Memories.Select(async m =>
        {
            using var collection = _store.GetCollection<Guid, Memory>(userIdentifier.ToString());
            await collection.EnsureCollectionExistsAsync(cancellationToken);
            await collection.DeleteAsync(m.Identifier, cancellationToken);
        });

        await Task.WhenAll(deletionTasks);
    }

    public async Task DeleteMemoryAsync(Guid userIdentifier, Guid memoryIdentifier, CancellationToken cancellationToken = default)
    {
        using var collection = _store.GetCollection<Guid, Memory>(userIdentifier.ToString());
        await collection.EnsureCollectionExistsAsync(cancellationToken);
        await collection.DeleteAsync(memoryIdentifier, cancellationToken);
    }

    public async Task UpsertMemoryAsync(IManagedIdentity identity, Memory memory, CancellationToken cancellationToken = default)
    {
        using var collection = _store.GetCollection<Guid, Memory>(identity.Id.ToString());
        await collection.EnsureCollectionExistsAsync(cancellationToken);
        await _client.CreateAliasAsync(identity.DisplayName, identity.Id.ToString(), cancellationToken: cancellationToken);

        if (string.IsNullOrEmpty(memory.Description))
        {
            memory.Description = await _memoryAugmentationService.GenerateMemoryDescriptionAsync(memory.Content);
        }

        var vectorMemory = await VectorMemory.FromMemory(memory, _embeddingGenerator, cancellationToken);
        await collection.UpsertAsync(vectorMemory, cancellationToken);
    }

    public async Task UpsertMemoryStoreAsync(IManagedIdentity identity, MemoryStore memoryStore, CancellationToken cancellationToken = default)
    {
        foreach (var memory in memoryStore.Memories)
        {
            memory.StoreId = memoryStore.StoreId;
            memory.Store = memoryStore;
        }

        var upsertTasks = memoryStore.Memories.Select(async m =>
        {
            await UpsertMemoryAsync(identity, m, cancellationToken);
        });

        await Task.WhenAll(upsertTasks);
    }
}
