using Microsoft.EntityFrameworkCore;
using Synaptic.NET.Domain.Abstractions.Management;
using Synaptic.NET.Domain.Abstractions.Storage;
using Synaptic.NET.Domain.Resources;
using Synaptic.NET.Domain.Resources.Storage;
using Synaptic.NET.Qdrant;

namespace Synaptic.NET.Core.Services;

/// <summary>
/// Provides an <see cref="IMemoryProvider"/> implementation that relies on EF for basic data retrieval but Vector Search for free text queries.
///
/// This implementation keeps a <see cref="QdrantMemoryClient"/> connected server's Qdrant instance in sync with the EF database.
/// </summary>
public class HybridMemoryProvider : IMemoryProvider
{
    private readonly SynapticDbContext _dbContext;
    private readonly QdrantMemoryClient _qdrantMemoryClient;
    private readonly ICurrentUserService _currentUserService;

    public HybridMemoryProvider(ICurrentUserService currentUserService, SynapticDbContext synapticDbContext, QdrantMemoryClient qdrantMemoryClient)
    {
        _currentUserService = currentUserService;
        _dbContext = synapticDbContext;
        _qdrantMemoryClient = qdrantMemoryClient;
    }

    public Task<IReadOnlyDictionary<Guid, string>> GetStoreIdentifiersAndDescriptionsAsync() =>
        throw new NotImplementedException();

    public async Task<List<MemoryStore>> GetStoresAsync()
    {
        return await _dbContext.MemoryStores.ToListAsync();
    }

    public Task<MemoryStore?> GetCollectionAsync(Guid collectionIdentifier)
    {
        return Task.FromResult(_dbContext.MemoryStores.FirstOrDefault(s => s.StoreId == collectionIdentifier));
    }

    public Task<MemoryStore?> GetCollectionAsync(string collectionTitle)
    {
        return Task.FromResult(_dbContext.MemoryStores.FirstOrDefault(s => s.Title == collectionTitle));
    }

    public async Task<IEnumerable<MemorySearchResult>> SearchAsync(string query, int limit = 10, double relevanceThreshold = 0.5)
    {
        var vectorResults = await _qdrantMemoryClient.SearchAsync(query, limit, relevanceThreshold, _currentUserService.GetCurrentUser().Id);
        // TODO: Add augmented search.
        return vectorResults;
    }

    public Task<bool> CreateCollectionAsync(string collectionTitle, string storeDescription)
    {
        _dbContext.MemoryStores.Add(new MemoryStore
        {
            Title = collectionTitle,
            Description = storeDescription,
            StoreId = Guid.NewGuid(),
            OwnerUser = _currentUserService.GetCurrentUser()
        });
        return Task.FromResult(true);
    }

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
