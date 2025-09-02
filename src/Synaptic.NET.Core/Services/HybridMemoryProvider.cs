using Synaptic.NET.Domain.Resources;
using Synaptic.NET.Qdrant;

namespace Synaptic.NET.Core.Services;

public class HybridMemoryProvider : IMemoryProvider
{
    private readonly SynapticDbContext _dbContext;
    private readonly QdrantMemoryClient _qdrantMemoryClient;
    public HybridMemoryProvider(SynapticDbContext synapticDbContext, QdrantMemoryClient qdrantMemoryClient)
    {
        _dbContext = synapticDbContext;
        _qdrantMemoryClient = qdrantMemoryClient;
    }
    public Task<IReadOnlyDictionary<Guid, string>> GetStoreIdentifiersAndDescriptionsAsync() => throw new NotImplementedException();

    public Task<List<MemoryStore>> GetStoresAsync() => throw new NotImplementedException();

    public Task<MemoryStore?> GetCollectionAsync(Guid collectionIdentifier) => throw new NotImplementedException();

    public Task<MemoryStore?> GetCollectionAsync(string collectionTitle) => throw new NotImplementedException();

    public Task<IEnumerable<MemorySearchResult>> SearchAsync(string query, int limit = 10, double relevanceThreshold = 0.5) => throw new NotImplementedException();

    public Task<bool> CreateCollectionAsync(string collectionTitle, string storeDescription) => throw new NotImplementedException();

    public Task<bool> CreateMemoryEntryAsync(Memory memory) => throw new NotImplementedException();

    public Task<bool> CreateMemoryEntryAsync(Guid collectionIdentifier, Memory memory, string storeDescription = "") => throw new NotImplementedException();

    public Task<bool> CreateMemoryEntryAsync(string collectionTitle, Memory memory, string storeDescription = "") => throw new NotImplementedException();

    public Task<bool> ReplaceCollectionAsync(Guid collectionIdentifier, MemoryStore newStore) => throw new NotImplementedException();

    public Task<bool> ReplaceCollectionAsync(string collectionTitle, MemoryStore newStore) => throw new NotImplementedException();

    public Task<bool> ReplaceMemoryEntryAsync(Guid entryIdentifier, Memory newMemory) => throw new NotImplementedException();

    public Task<bool> ReplaceMemoryEntryAsync(Guid collectionIdentifier, Guid entryIdentifier, Memory newMemory) => throw new NotImplementedException();

    public Task<bool> ReplaceMemoryEntryAsync(string collectionTitle, Guid entryIdentifier, Memory newMemory) => throw new NotImplementedException();

    public Task<bool> UpdateCollectionAsync(Guid collectionIdentifier, MemoryStore newStore) => throw new NotImplementedException();

    public Task<bool> UpdateCollectionAsync(string collectionTitle, MemoryStore newStore) => throw new NotImplementedException();

    public Task<bool> UpdateMemoryEntryAsync(Guid entryIdentifier, Memory newMemory) => throw new NotImplementedException();

    public Task<bool> UpdateMemoryEntryAsync(Guid collectionIdentifier, Guid entryIdentifier, Memory newMemory) => throw new NotImplementedException();

    public Task<bool> UpdateMemoryEntryAsync(string collectionTitle, Guid entryIdentifier, Memory newMemory) => throw new NotImplementedException();

    public Task<bool> DeleteCollectionAsync(Guid collectionIdentifier) => throw new NotImplementedException();

    public Task<bool> DeleteCollectionAsync(string collectionTitle) => throw new NotImplementedException();

    public Task<bool> DeleteMemoryEntryAsync(Guid collectionIdentifier, Guid entryIdentifier) => throw new NotImplementedException();

    public Task<bool> DeleteMemoryEntryAsync(Guid entryIdentifier) => throw new NotImplementedException();

    public Task<bool> DeleteMemoryEntryAsync(Guid collectionIdentifier, string entryTitle) => throw new NotImplementedException();
}
