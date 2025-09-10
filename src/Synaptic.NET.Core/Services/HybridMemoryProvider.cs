using System.Diagnostics.CodeAnalysis;
using Microsoft.EntityFrameworkCore;
using Synaptic.NET.Domain.Abstractions.Augmentation;
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
    private readonly IMemoryStoreRouter _storeRouter;
    private readonly IMemoryAugmentationService _augmentationService;
    private readonly IMemoryQueryResultReranker _reranker;

    public HybridMemoryProvider(ICurrentUserService currentUserService, SynapticDbContext synapticDbContext, QdrantMemoryClient qdrantMemoryClient, IMemoryStoreRouter storeRouter, IMemoryAugmentationService augmentationService, IMemoryQueryResultReranker reranker)
    {
        _currentUserService = currentUserService;
        _dbContext = synapticDbContext;
        _qdrantMemoryClient = qdrantMemoryClient;
        _storeRouter = storeRouter;
        _augmentationService = augmentationService;
        _reranker = reranker;
        _dbContext.SetCurrentUser(currentUserService);
    }

    public async Task<IReadOnlyDictionary<Guid, string>> GetStoreIdentifiersAndDescriptionsAsync()
    {
        var result = await _dbContext.MemoryStores.ToDictionaryAsync(s => s.StoreId, s => s.Description);
        return result;
    }

    public async Task<List<MemoryStore>> GetStoresAsync()
    {
        return _dbContext.MemoryStores.ToList();
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
        var userVectorResults = await _qdrantMemoryClient.SearchAsync(query, limit, relevanceThreshold, _currentUserService.GetCurrentUser().Id);

        foreach (var group in _currentUserService.GetCurrentUser().Memberships.Select(m => m.Group))
        {
            var groupResults = await _qdrantMemoryClient.SearchAsync(query, limit, relevanceThreshold, group.Id);
            userVectorResults = userVectorResults.Concat(groupResults);
        }

        var resultList = userVectorResults.ToList();
        List<MemorySearchResult> results;
        if (resultList.Any())
        {
            var reranked = (await _reranker.Rerank(resultList)).ToList();
            Log.Information($"[Memory Search] Vector search was sufficiently returning results. Reranking results with {reranked.Count} results.");
            results = reranked.OrderBy(r => r.Relevance).Take(limit).ToList();
        }
        else
        {
            Log.Information($"[Memory Search] Vector search did not result in any results. Falling back to augmented search...");
            var augmentedResults = await SearchAugmented(query, limit, relevanceThreshold);
            results = augmentedResults.OrderBy(r => r.Relevance).Take(limit).ToList();
        }

        foreach (var result in results)
        {
            if (result.Memory.Store != null)
            {
                continue;
            }

            if (await _dbContext.MemoryStores.FirstOrDefaultAsync(s => s.StoreId == result.Memory.StoreId) is not
                { } existingStore)
            {
                continue;
            }

            result.Memory.Store = existingStore;

            if (_dbContext.DbUser() is not { } user || user.Id != _currentUserService.GetCurrentUser().Id)
            {
                continue;
            }

            if (result.Memory.OwnerUser == null)
            {
                result.Memory.OwnerUser = user;
            }
        }
        return results;
    }

    private async Task<IEnumerable<MemorySearchResult>> SearchAugmented(string query, int limit = 10,
        double relevanceThreshold = 0.5)
    {
        var groupStores = _currentUserService.GetCurrentUser().Memberships.Select(m => m.Group).SelectMany(g => g.Stores).ToList();

        var allStores = _dbContext.MemoryStores.ToList().Concat(groupStores).ToList();

        var storeRankings = (await _storeRouter.RankStoresAsync(query, allStores)).ToList();

        var topScore = storeRankings.FirstOrDefault()?.Relevance ?? double.MinValue;

        if (Math.Abs(topScore - double.MinValue) < 0.1)
        {
            return Enumerable.Empty<MemorySearchResult>();
        }

        storeRankings = storeRankings.Where(r => r.Relevance >= topScore * 0.5).ToList();

        var rankedStores = allStores.Where(s => storeRankings.Any(r => r.Identifier == s.StoreId)).ToList();

        CancellationToken token = new CancellationTokenSource().Token;
        var searchTasks = rankedStores.Select(async s => await AugmentedSearchInStore(s, query, limit, relevanceThreshold, token));
        // TODO: Figure out a way to cancel the search tasks when the limit is reached.
        var results = (await Task.WhenAll(searchTasks)).SelectMany(r => r).ToList();
        return results.OrderBy(r => r.Relevance).Take(limit);
    }

    private async Task<IEnumerable<MemorySearchResult>> AugmentedSearchInStore(MemoryStore chunk, string query, int limit = 10, double relevanceThreshold = 0.5,
        CancellationToken token = default)
    {
        var results = new List<MemorySearchResult>();
        var vectorResults = await _qdrantMemoryClient.SearchInStoreAsync(query, limit, relevanceThreshold, chunk.StoreId, _currentUserService.GetCurrentUser().Id, token);

        var rankings = await _augmentationService.RankMemoriesAsync(query, chunk, token);
        foreach (var ranking in rankings.Where(r => r.Item2 >= relevanceThreshold))
        {
            if (results.Count >= limit)
            {
                break;
            }
            if (chunk.Memories.FirstOrDefault(m => m.Identifier == ranking.Item1) is { } memory)
            {
                results.Add(new MemorySearchResult
                {
                    Memory = memory,
                    Relevance = ranking.Item2
                });
            }
        }

        var concatResults = results.Concat(vectorResults).Where(r => r.Relevance >= relevanceThreshold).Take(limit);

        return concatResults;
    }

    public async Task<MemoryStore?> CreateCollectionAsync(MemoryStore store)
    {
        if (_dbContext.MemoryStores.FirstOrDefault(s => s.StoreId == store.StoreId) is { } existingStore)
        {
            foreach (var memory in store.Memories)
            {
                existingStore.Memories.Add(memory);

            }
            await _qdrantMemoryClient.UpsertMemoryStoreAsync(_currentUserService.GetCurrentUser(), store);
            _dbContext.MemoryStores.Update(existingStore);

            await _dbContext.SaveChangesAsync();
            return existingStore;
        }
        _dbContext.MemoryStores.Add(store);
        await _dbContext.SaveChangesAsync();
        await _qdrantMemoryClient.UpsertMemoryStoreAsync(_currentUserService.GetCurrentUser(), store);
        return store;
    }

    public Task<bool> CreateCollectionAsync(string collectionTitle, string storeDescription, [MaybeNullWhen(false)] out MemoryStore store)
    {
        var newStore = new MemoryStore
        {
            Title = collectionTitle,
            Description = storeDescription,
            StoreId = Guid.NewGuid(),
            OwnerUser = _currentUserService.GetCurrentUser(),
            UserId = _currentUserService.GetCurrentUser().Id
        };
        _dbContext.MemoryStores.Add(newStore);
        store = newStore;
        _dbContext.SaveChanges();
        return Task.FromResult(true);
    }

    public async Task<bool> CreateMemoryEntryAsync(Memory memory)
    {
        var targetStoreRoutingResult = await _storeRouter.RouteMemoryToStoreAsync(memory, _dbContext.MemoryStores);
        var targetStore = _dbContext.MemoryStores.FirstOrDefault(s => s.StoreId == targetStoreRoutingResult.Identifier);

        if (targetStore == null)
        {
            string title = await _augmentationService.GenerateStoreTitleAsync(string.Empty, [memory]);
            string description = await _augmentationService.GenerateStoreDescriptionAsync(title, [memory]);
            await CreateCollectionAsync(title, description, out targetStore);
        }

        memory.StoreId = targetStore.StoreId;
        memory.Store = targetStore;
        _dbContext.Memories.Add(memory);
        await _dbContext.SaveChangesAsync();
        await _qdrantMemoryClient.UpsertMemoryAsync(_currentUserService.GetCurrentUser(), memory);
        return true;
    }

    public async Task<bool> CreateMemoryEntryAsync(Guid collectionIdentifier, Memory memory, string storeDescription = "")
    {
        var targetStore = _dbContext.MemoryStores.FirstOrDefault(s => s.StoreId == collectionIdentifier);
        if (targetStore == null)
        {
            string title = await _augmentationService.GenerateStoreTitleAsync(storeDescription, [memory]);
            await CreateCollectionAsync(title, storeDescription, out targetStore);
        }
        memory.StoreId = targetStore.StoreId;
        memory.Store = targetStore;
        _dbContext.Memories.Add(memory);
        await _dbContext.SaveChangesAsync();
        await _qdrantMemoryClient.UpsertMemoryAsync(_currentUserService.GetCurrentUser(), memory);
        return true;
    }

    public async Task<bool> CreateMemoryEntryAsync(string collectionTitle, Memory memory, string storeDescription = "")
    {
        var targetStore = _dbContext.MemoryStores.FirstOrDefault(s => s.Title == collectionTitle);
        if (targetStore == null)
        {
            await CreateCollectionAsync(collectionTitle, storeDescription, out targetStore);
        }
        memory.StoreId = targetStore.StoreId;
        memory.Store = targetStore;
        _dbContext.Memories.Add(memory);
        _dbContext.MemoryStores.Update(targetStore);
        await _dbContext.SaveChangesAsync();
        await _qdrantMemoryClient.UpsertMemoryAsync(_currentUserService.GetCurrentUser(), memory);
        return true;
    }

    public async Task<bool> ReplaceCollectionAsync(Guid collectionIdentifier, MemoryStore newStore)
    {
        var existingStore = await _dbContext.MemoryStores
            .Include(s => s.Memories)
            .FirstOrDefaultAsync(s => s.StoreId == collectionIdentifier);

        if (existingStore == null)
        {
            return false;
        }

        await _qdrantMemoryClient.DeleteMemoryStoreAsync(_currentUserService.GetCurrentUser().Id, existingStore);

        _dbContext.MemoryStores.Remove(existingStore);

        newStore.StoreId = collectionIdentifier;
        newStore.OwnerUser = _currentUserService.GetCurrentUser();
        _dbContext.MemoryStores.Add(newStore);

        await _dbContext.SaveChangesAsync();

        await _qdrantMemoryClient.UpsertMemoryStoreAsync(_currentUserService.GetCurrentUser(), newStore);

        return true;
    }

    public async Task<bool> ReplaceCollectionAsync(string collectionTitle, MemoryStore newStore)
    {
        var existingStore = await _dbContext.MemoryStores
            .Include(s => s.Memories)
            .FirstOrDefaultAsync(s => s.Title == collectionTitle);

        if (existingStore == null)
        {
            return false;
        }

        await _qdrantMemoryClient.DeleteMemoryStoreAsync(_currentUserService.GetCurrentUser().Id, existingStore);

        _dbContext.MemoryStores.Remove(existingStore);

        newStore.StoreId = existingStore.StoreId;
        newStore.OwnerUser = _currentUserService.GetCurrentUser();
        _dbContext.MemoryStores.Add(newStore);

        await _dbContext.SaveChangesAsync();

        await _qdrantMemoryClient.UpsertMemoryStoreAsync(_currentUserService.GetCurrentUser(), newStore);

        return true;
    }

    public async Task<bool> ReplaceMemoryEntryAsync(Guid entryIdentifier, Memory newMemory)
    {
        var existingMemory = await _dbContext.Memories
            .FirstOrDefaultAsync(m => m.Identifier == entryIdentifier);

        if (existingMemory == null)
        {
            return false;
        }

        await _qdrantMemoryClient.DeleteMemoryAsync(_currentUserService.GetCurrentUser().Id, entryIdentifier);

        _dbContext.Memories.Remove(existingMemory);

        newMemory.Identifier = entryIdentifier;
        newMemory.StoreId = existingMemory.StoreId;
        newMemory.Owner = _currentUserService.GetCurrentUser().Id;
        newMemory.UpdatedAt = DateTimeOffset.UtcNow;

        _dbContext.Memories.Add(newMemory);

        await _dbContext.SaveChangesAsync();

        await _qdrantMemoryClient.UpsertMemoryAsync(_currentUserService.GetCurrentUser(), newMemory);

        return true;
    }

    public async Task<bool> ReplaceMemoryEntryAsync(Guid collectionIdentifier, Guid entryIdentifier, Memory newMemory)
    {
        var existingMemory = await _dbContext.Memories
            .FirstOrDefaultAsync(m => m.Identifier == entryIdentifier && m.StoreId == collectionIdentifier);

        if (existingMemory == null)
        {
            return false;
        }

        await _qdrantMemoryClient.DeleteMemoryAsync(_currentUserService.GetCurrentUser().Id, entryIdentifier);

        _dbContext.Memories.Remove(existingMemory);

        newMemory.Identifier = entryIdentifier;
        newMemory.StoreId = collectionIdentifier;
        newMemory.Owner = _currentUserService.GetCurrentUser().Id;
        newMemory.UpdatedAt = DateTimeOffset.UtcNow;

        _dbContext.Memories.Add(newMemory);

        await _dbContext.SaveChangesAsync();

        await _qdrantMemoryClient.UpsertMemoryAsync(_currentUserService.GetCurrentUser(), newMemory);

        return true;
    }

    public async Task<bool> ReplaceMemoryEntryAsync(string collectionTitle, Guid entryIdentifier, Memory newMemory)
    {
        var targetStore = await _dbContext.MemoryStores
            .FirstOrDefaultAsync(s => s.Title == collectionTitle);

        if (targetStore == null)
        {
            return false;
        }

        var existingMemory = await _dbContext.Memories
            .FirstOrDefaultAsync(m => m.Identifier == entryIdentifier && m.StoreId == targetStore.StoreId);

        if (existingMemory == null)
        {
            return false;
        }

        await _qdrantMemoryClient.DeleteMemoryAsync(_currentUserService.GetCurrentUser().Id, entryIdentifier);

        _dbContext.Memories.Remove(existingMemory);

        newMemory.Identifier = entryIdentifier;
        newMemory.StoreId = targetStore.StoreId;
        newMemory.Owner = _currentUserService.GetCurrentUser().Id;
        newMemory.UpdatedAt = DateTimeOffset.UtcNow;

        _dbContext.Memories.Add(newMemory);

        await _dbContext.SaveChangesAsync();

        await _qdrantMemoryClient.UpsertMemoryAsync(_currentUserService.GetCurrentUser(), newMemory);

        return true;
    }

    public async Task<bool> UpdateCollectionAsync(Guid collectionIdentifier, MemoryStore newStore)
    {
        var existingStore = await _dbContext.MemoryStores
            .FirstOrDefaultAsync(s => s.StoreId == collectionIdentifier);

        if (existingStore == null)
        {
            return false;
        }

        existingStore.Title = newStore.Title;
        existingStore.Description = newStore.Description;

        _dbContext.MemoryStores.Update(existingStore);
        await _dbContext.SaveChangesAsync();

        return true;
    }

    public async Task<bool> UpdateCollectionAsync(string collectionTitle, MemoryStore newStore)
    {
        var existingStore = await _dbContext.MemoryStores
            .FirstOrDefaultAsync(s => s.Title == collectionTitle);

        if (existingStore == null)
        {
            return false;
        }

        existingStore.Title = newStore.Title;
        existingStore.Description = newStore.Description;

        _dbContext.MemoryStores.Update(existingStore);
        await _dbContext.SaveChangesAsync();

        return true;
    }

    public async Task<bool> UpdateMemoryEntryAsync(Guid entryIdentifier, Memory newMemory)
    {
        var existingMemory = await _dbContext.Memories
            .FirstOrDefaultAsync(m => m.Identifier == entryIdentifier);

        if (existingMemory == null)
        {
            return false;
        }

        existingMemory.Title = newMemory.Title;
        existingMemory.Description = newMemory.Description;
        existingMemory.Content = newMemory.Content;
        existingMemory.Pinned = newMemory.Pinned;
        existingMemory.Tags = existingMemory.Tags.Concat(newMemory.Tags).Distinct().ToList();
        existingMemory.ReferenceType = newMemory.ReferenceType;
        existingMemory.Reference = newMemory.Reference;
        existingMemory.UpdatedAt = DateTimeOffset.UtcNow;

        _dbContext.Memories.Update(existingMemory);
        await _dbContext.SaveChangesAsync();

        await _qdrantMemoryClient.UpsertMemoryAsync(_currentUserService.GetCurrentUser(), existingMemory);

        return true;
    }

    public async Task<bool> UpdateMemoryEntryAsync(Guid collectionIdentifier, Guid entryIdentifier, Memory newMemory)
    {
        var existingMemory = await _dbContext.Memories
            .FirstOrDefaultAsync(m => m.Identifier == entryIdentifier && m.StoreId == collectionIdentifier);

        if (existingMemory == null)
        {
            return false;
        }

        existingMemory.Title = newMemory.Title;
        existingMemory.Description = newMemory.Description;
        existingMemory.Content = newMemory.Content;
        existingMemory.Pinned = newMemory.Pinned;
        existingMemory.Tags = newMemory.Tags;
        existingMemory.ReferenceType = newMemory.ReferenceType;
        existingMemory.Reference = newMemory.Reference;
        existingMemory.UpdatedAt = DateTimeOffset.UtcNow;

        _dbContext.Memories.Update(existingMemory);
        await _dbContext.SaveChangesAsync();

        await _qdrantMemoryClient.UpsertMemoryAsync(_currentUserService.GetCurrentUser(), existingMemory);

        return true;
    }

    public async Task<bool> UpdateMemoryEntryAsync(string collectionTitle, Guid entryIdentifier, Memory newMemory)
    {
        var targetStore = await _dbContext.MemoryStores
            .FirstOrDefaultAsync(s => s.Title == collectionTitle);

        if (targetStore == null)
        {
            return false;
        }

        var existingMemory = await _dbContext.Memories
            .FirstOrDefaultAsync(m => m.Identifier == entryIdentifier && m.StoreId == targetStore.StoreId);

        if (existingMemory == null)
        {
            return false;
        }

        existingMemory.Title = newMemory.Title;
        existingMemory.Description = newMemory.Description;
        existingMemory.Content = newMemory.Content;
        existingMemory.Pinned = newMemory.Pinned;
        existingMemory.Tags = newMemory.Tags;
        existingMemory.ReferenceType = newMemory.ReferenceType;
        existingMemory.Reference = newMemory.Reference;
        existingMemory.UpdatedAt = DateTimeOffset.UtcNow;

        _dbContext.Memories.Update(existingMemory);
        await _dbContext.SaveChangesAsync();

        await _qdrantMemoryClient.UpsertMemoryAsync(_currentUserService.GetCurrentUser(), existingMemory);

        return true;
    }

    public async Task<bool> DeleteCollectionAsync(Guid collectionIdentifier)
    {
        var targetStore = await _dbContext.MemoryStores
            .FirstOrDefaultAsync(s => s.StoreId == collectionIdentifier);

        if (targetStore == null)
        {
            return false;
        }

        await _qdrantMemoryClient.DeleteMemoryStoreAsync(_currentUserService.GetCurrentUser().Id, targetStore);

        _dbContext.MemoryStores.Remove(targetStore);
        await _dbContext.SaveChangesAsync();

        return true;
    }

    public async Task<bool> DeleteCollectionAsync(string collectionTitle)
    {
        var targetStore = await _dbContext.MemoryStores
            .Include(s => s.Memories)
            .FirstOrDefaultAsync(s => s.Title == collectionTitle);

        if (targetStore == null)
        {
            return false;
        }

        await _qdrantMemoryClient.DeleteMemoryStoreAsync(_currentUserService.GetCurrentUser().Id, targetStore);

        _dbContext.MemoryStores.Remove(targetStore);
        await _dbContext.SaveChangesAsync();

        return true;
    }

    public async Task<bool> DeleteMemoryEntryAsync(Guid collectionIdentifier, Guid entryIdentifier)
    {
        var targetMemory = await _dbContext.Memories
            .FirstOrDefaultAsync(m => m.Identifier == entryIdentifier && m.StoreId == collectionIdentifier);

        if (targetMemory == null)
        {
            return false;
        }

        await _qdrantMemoryClient.DeleteMemoryAsync(_currentUserService.GetCurrentUser().Id, entryIdentifier);

        _dbContext.Memories.Remove(targetMemory);
        await _dbContext.SaveChangesAsync();

        return true;
    }

    public async Task<bool> DeleteMemoryEntryAsync(Guid entryIdentifier)
    {
        var targetMemory = await _dbContext.Memories
            .FirstOrDefaultAsync(m => m.Identifier == entryIdentifier);

        if (targetMemory == null)
        {
            return false;
        }

        await _qdrantMemoryClient.DeleteMemoryAsync(_currentUserService.GetCurrentUser().Id, entryIdentifier);

        _dbContext.Memories.Remove(targetMemory);
        await _dbContext.SaveChangesAsync();

        return true;
    }

    public async Task<bool> DeleteMemoryEntryAsync(Guid collectionIdentifier, string entryTitle)
    {
        var targetMemory = await _dbContext.Memories
            .FirstOrDefaultAsync(m => m.Title == entryTitle && m.StoreId == collectionIdentifier);

        if (targetMemory == null)
        {
            return false;
        }

        await _qdrantMemoryClient.DeleteMemoryAsync(_currentUserService.GetCurrentUser().Id, targetMemory.Identifier);

        _dbContext.Memories.Remove(targetMemory);
        await _dbContext.SaveChangesAsync();

        return true;
    }

    public async Task<bool> PublishMemoryStoreToGroup(Guid collectionIdentifier, Guid groupId)
    {
        if (_currentUserService.GetCurrentUser().Memberships.All(m => m.Group.Id != groupId))
        {
            return false;
        }

        if (_dbContext.MemoryStores.FirstOrDefault(s => s.StoreId == collectionIdentifier) is not { } store)
        {
            return false;
        }

        if (_dbContext.Groups.FirstOrDefault(g => g.Id == groupId) is not { } group)
        {
            return false;
        }

        store.GroupId = groupId;
        store.OwnerGroup = group;
        _dbContext.MemoryStores.Update(store);
        foreach (var memory in store.Memories)
        {
            memory.GroupId = groupId;
            memory.OwnerGroup = group;
            _dbContext.Memories.Update(memory);
        }
        await _dbContext.SaveChangesAsync();
        await _qdrantMemoryClient.UpsertMemoryStoreAsync(group, store);
        await _qdrantMemoryClient.DeleteMemoryStoreAsync(_currentUserService.GetCurrentUser().Id, store);
        return true;

    }
}
