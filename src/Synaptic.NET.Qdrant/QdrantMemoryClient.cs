using System.Linq.Expressions;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.VectorData;
using Microsoft.SemanticKernel.Connectors.Qdrant;
using OpenAI;
using OpenAI.Embeddings;
using Qdrant.Client;
using Synaptic.NET.Domain.Abstractions.Augmentation;
using Synaptic.NET.Domain.Abstractions.Management;
using Synaptic.NET.Domain.Resources.Configuration;
using Synaptic.NET.Domain.Resources.Management;
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
        _embeddingGenerator = new OpenAIClient(settings.OpenAiSettings.ApiKey)
            .GetEmbeddingClient(settings.OpenAiSettings.EmbeddingModel);

        string ip;
        int port;
        if (settings.ServerSettings.QdrantUrl.Contains(":"))
        {
            var url = Uri.TryCreate(settings.ServerSettings.QdrantUrl, UriKind.Absolute, out var uri) ? uri : throw new InvalidOperationException();

            ip = $"{uri.Host}";
            port = url.Port;
        }
        else
        {
            ip = settings.ServerSettings.QdrantUrl;
            port = 6334;
        }

        if (string.IsNullOrEmpty(settings.ServerSettings.QdrantApiKey) || settings.ServerSettings.QdrantApiKey == "your_secret_api_key_here")
        {
            _client = new QdrantClient(ip, port);
        }
        else
        {
            _client = new QdrantClient(ip, port, apiKey: settings.ServerSettings.QdrantApiKey);
        }

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

    static Expression<Func<VectorMemory, bool>> BuildOwnerFilter(string userId, IEnumerable<string> groupUserIds)
    {
        var param = Expression.Parameter(typeof(VectorMemory), "m");
        var prop  = Expression.Property(param, nameof(VectorMemory.VectorOwnerIdentifier));

        // m => m.VectorOwnerIdentifier == userId
        Expression body = Expression.Equal(prop, Expression.Constant(userId, typeof(string)));

        foreach (var id in groupUserIds)
        {
            var eq = Expression.Equal(prop, Expression.Constant(id, typeof(string)));
            body = Expression.OrElse(body, eq);
        }

        return Expression.Lambda<Func<VectorMemory, bool>>(body, param);
    }

    public async Task<IEnumerable<MemorySearchResult>> SearchAsync(string query, int top, double relevanceThreshold, IManagedIdentity owner, CancellationToken cancellationToken = default)
    {
        using var collection = _store.GetCollection<Guid, VectorMemory>(owner.Id.ToString());
        await collection.EnsureCollectionExistsAsync(cancellationToken);

        // If the searched identity is a group, the filter could either be for ownership of the memory to the group or for any of the users that are part of the group.
        var allowedGroupUserIds = owner is Group g
            ? g.Memberships.Select(m => m.UserId.ToString("D").ToLowerInvariant())
            : Enumerable.Empty<string>();

        var filter = BuildOwnerFilter(userId: owner.Id.ToString("D").ToLowerInvariant(),
            groupUserIds: allowedGroupUserIds);

        var contentResult = await collection.SearchAsync(query, top, options: new VectorSearchOptions<VectorMemory>
        {
            VectorProperty = m => m.ContentEmbedding,
            Filter = filter
        }, cancellationToken: cancellationToken).ToListAsync(cancellationToken);


        var titleResult = await collection.SearchAsync(query, top, options: new VectorSearchOptions<VectorMemory>
        {
            VectorProperty = m => m.TitleEmbedding,
            Filter = filter
        }, cancellationToken: cancellationToken).ToListAsync(cancellationToken);

        var descriptionResult = await collection.SearchAsync(query, top, options: new VectorSearchOptions<VectorMemory>
        {
            VectorProperty = m => m.DescriptionEmbedding,
            Filter = filter
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
        }, cancellationToken: cancellationToken).ToListAsync(cancellationToken);

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
            using var collection = _store.GetCollection<Guid, VectorMemory>(userIdentifier.ToString());
            await collection.EnsureCollectionExistsAsync(cancellationToken);
            await collection.DeleteAsync(m.Identifier, cancellationToken);
        });

        await Task.WhenAll(deletionTasks);
    }

    public async Task DeleteMemoryAsync(Guid userIdentifier, Guid memoryIdentifier, CancellationToken cancellationToken = default)
    {
        using var collection = _store.GetCollection<Guid, VectorMemory>(userIdentifier.ToString());
        await collection.EnsureCollectionExistsAsync(cancellationToken);
        await collection.DeleteAsync(memoryIdentifier, cancellationToken);
    }

    public async Task UpsertMemoryAsync(IManagedIdentity identity, Memory memory, CancellationToken cancellationToken = default)
    {
        using var collection = _store.GetCollection<Guid, VectorMemory>(identity.Id.ToString());
        await collection.EnsureCollectionExistsAsync(cancellationToken);
        await _client.CreateAliasAsync(identity.Identifier, identity.Id.ToString(), cancellationToken: cancellationToken);

        if (string.IsNullOrEmpty(memory.Description))
        {
            // Requirement as this cannot be empty as long as descriptions are used for embeddings.
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
