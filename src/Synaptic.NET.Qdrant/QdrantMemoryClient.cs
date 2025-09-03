using System.Linq.Expressions;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.VectorData;
using Microsoft.SemanticKernel.Connectors.Qdrant;
using Microsoft.SemanticKernel.Memory;
using OpenAI;
using OpenAI.Embeddings;
using Qdrant.Client;
using Synaptic.NET.Domain;
using Synaptic.NET.Domain.Resources;

namespace Synaptic.NET.Qdrant;

public class QdrantMemoryClient
{
    private readonly EmbeddingClient _embeddingGenerator;
    private readonly QdrantClient _client;
    private readonly QdrantVectorStore _store;
    public QdrantMemoryClient(SynapticServerSettings settings)
    {
        _embeddingGenerator = new OpenAIClient(settings.OpenAiApiKey)
            .GetEmbeddingClient(settings.OpenAiEmbeddingModel);

        string ip;
        int port;
        if (settings.QdrantServerUrl.Contains(":"))
        {
            ip = settings.QdrantServerUrl.Split(":").First();
            port = int.Parse(settings.QdrantServerUrl.Split(":").Last());
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
    }

    public async Task<IEnumerable<MemorySearchResult>> SearchAsync(string query, int top, double relevanceThreshold, Guid userIdentifier, CancellationToken cancellationToken = default)
    {
        using var collection = _store.GetCollection<Guid, Memory>(userIdentifier.ToString());
        await collection.EnsureCollectionExistsAsync(cancellationToken);
        var result = await collection.SearchAsync(query, top, options: new VectorSearchOptions<Memory>
        {
            Filter = m => m.Owner == userIdentifier
        }, cancellationToken: cancellationToken).ToListAsync(cancellationToken);
        return result
            .Where(v => v.Score >= relevanceThreshold)
            .OrderBy(r => r.Score)
            .Take(top)
            .Select(v => new MemorySearchResult { Memory = v.Record, Relevance = v.Score ?? 0 });
    }

    public async Task<IEnumerable<MemorySearchResult>> SearchInStoreAsync(string query, int top, double relevanceThreshold, Guid collectionIdentifier, Guid userIdentifier, CancellationToken cancellationToken = default)
    {
        using var collection = _store.GetCollection<Guid, Memory>(userIdentifier.ToString());
        await collection.EnsureCollectionExistsAsync(cancellationToken);
        var result = await collection.SearchAsync(query, top, options: new VectorSearchOptions<Memory>
        {
            Filter = m => m.StoreId == collectionIdentifier
        },cancellationToken: cancellationToken).ToListAsync(cancellationToken);
        return result
            .Where(v => v.Score > relevanceThreshold)
            .OrderBy(r => r.Score)
            .Take(top)
            .Select(v => new MemorySearchResult { Memory = v.Record, Relevance = v.Score ?? 0 });
    }

    public async Task DeleteMemoryStoreAsync(Guid userIdentifier, MemoryStore memoryStore, CancellationToken cancellationToken = default)
    {
        using var collection = _store.GetCollection<Guid, Memory>(userIdentifier.ToString());
        await collection.EnsureCollectionExistsAsync(cancellationToken);
        await Parallel.ForEachAsync(memoryStore.Memories, cancellationToken, async (memory, _) =>
        {
            await collection.DeleteAsync(memory.Identifier, cancellationToken);
        });
    }

    public async Task DeleteMemoryAsync(Guid userIdentifier, Guid memoryIdentifier, CancellationToken cancellationToken = default)
    {
        using var collection = _store.GetCollection<Guid, Memory>(userIdentifier.ToString());
        await collection.EnsureCollectionExistsAsync(cancellationToken);
        await collection.DeleteAsync(memoryIdentifier, cancellationToken);
    }

    public async Task UpsertMemoryAsync(Guid userIdentifier, Memory memory, CancellationToken cancellationToken = default)
    {
        using var collection = _store.GetCollection<Guid, Memory>(userIdentifier.ToString());
        await collection.EnsureCollectionExistsAsync(cancellationToken);
        var contentEmbeddingResult = await _embeddingGenerator.GenerateEmbeddingAsync(memory.Content, cancellationToken: cancellationToken);
        var descriptionEmbeddingResult =
            await _embeddingGenerator.GenerateEmbeddingAsync(memory.Description, cancellationToken: cancellationToken);
        var titleEmbedding = await _embeddingGenerator.GenerateEmbeddingAsync(memory.Title, cancellationToken: cancellationToken);

        memory.TitleEmbedding = titleEmbedding.Value.ToFloats();
        memory.ContentEmbedding = contentEmbeddingResult.Value.ToFloats();
        memory.DescriptionEmbedding = descriptionEmbeddingResult.Value.ToFloats();
        await collection.UpsertAsync(memory, cancellationToken);
    }

    public async Task UpsertMemoryStoreAsync(Guid userIdentifier, MemoryStore memoryStore, CancellationToken cancellationToken = default)
    {
        using var collection = _store.GetCollection<Guid, Memory>(userIdentifier.ToString());
        await Parallel.ForEachAsync(memoryStore.Memories, cancellationToken, async (memory, _) =>
        {
            await collection.EnsureCollectionExistsAsync(cancellationToken);
            var contentEmbeddingResult = await _embeddingGenerator.GenerateEmbeddingAsync(memory.Content, cancellationToken: cancellationToken);
            var descriptionEmbeddingResult =
                await _embeddingGenerator.GenerateEmbeddingAsync(memory.Description, cancellationToken: cancellationToken);
            var titleEmbedding = await _embeddingGenerator.GenerateEmbeddingAsync(memory.Title, cancellationToken: cancellationToken);

            memory.TitleEmbedding = titleEmbedding.Value.ToFloats();
            memory.ContentEmbedding = contentEmbeddingResult.Value.ToFloats();
            memory.DescriptionEmbedding = descriptionEmbeddingResult.Value.ToFloats();
            await collection.UpsertAsync(memory, cancellationToken);
        });
    }
}
