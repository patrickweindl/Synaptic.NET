using Microsoft.EntityFrameworkCore;
using Synaptic.NET.Domain.Abstractions.Augmentation;
using Synaptic.NET.Domain.Abstractions.Management;
using Synaptic.NET.Domain.Abstractions.Storage;
using Synaptic.NET.Domain.Resources;
using Synaptic.NET.Domain.Resources.Management;
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
        _ = Task.Run(async () => await _dbContext.SetCurrentUserAsync(currentUserService));
    }

    public async Task<IReadOnlyDictionary<Guid, string>> GetStoreIdentifiersAndDescriptionsAsync()
    {
        var result = await _dbContext.MemoryStores.ToDictionaryAsync(s => s.StoreId, s => s.Description);
        return result;
    }

    public async Task<List<MemoryStore>> GetStoresAsync()
    {
        return await _dbContext.MemoryStores.ToListAsync();
    }

    public async Task<MemoryStore?> GetCollectionAsync(Guid collectionIdentifier)
    {
        return await _dbContext.MemoryStores.FirstOrDefaultAsync(s => s.StoreId == collectionIdentifier);
    }

    public async Task<MemoryStore?> GetCollectionAsync(string collectionTitle)
    {
        return await _dbContext.MemoryStores.FirstOrDefaultAsync(s => s.Title == collectionTitle);
    }

    private async IAsyncEnumerable<MemorySearchResult> HybridSearchWithReranking(ObservableMemorySearchResult r, string query, int limit, double relevanceThreshold, MemoryQueryOptions options)
    {
        r.Message = "Memory query received. Starting memory search...";
        var currentUser = await _currentUserService.GetCurrentUserAsync();
        var userVectorResults = Enumerable.Empty<MemorySearchResult>();
        if (options.SearchInPersonal)
        {
            if (options.StoreQueryOptions.StoreSearchMode < StoreSearchMode.All)
            {
                var storeFilteredResults = new List<MemorySearchResult>();
                foreach (var store in currentUser.Stores.Where(s => s.GroupId == null))
                {
                    if (options.StoreQueryOptions.StoreShouldBeIncluded(store))
                    {
                        storeFilteredResults.AddRange(await _qdrantMemoryClient.SearchInStoreAsync(query, limit, relevanceThreshold,
                            options.StoreQueryOptions.StoreIds.FirstOrDefault(), currentUser.Id));
                    }
                }
                userVectorResults = storeFilteredResults;
            }
            else
            {
                userVectorResults = await _qdrantMemoryClient.SearchAsync(query, limit, relevanceThreshold, currentUser);
            }

        }

        if (options.GroupQueryOptions.GroupSearchMode > GroupSearchMode.None)
        {
            foreach (var group in currentUser.Memberships.Select(m => m.Group).Where(g => options.GroupQueryOptions.GroupShouldBeIncluded(g)))
            {
                var groupResults = await _qdrantMemoryClient.SearchAsync(query, limit, relevanceThreshold, group);
                userVectorResults = userVectorResults.Concat(groupResults);
            }
        }


        var resultList = userVectorResults.ToList();
        r.Progress = 0.2;
        r.Message = $"Vector search resulted in {resultList.Count} results.";
        if (resultList.Count != 0)
        {
            r.Progress = 0.7;
            r.Message = "Reranking results...";
            var rerankedResults = _reranker.Rerank(resultList);
            Log.Information(
                "[Memory Search] Vector search was sufficiently returning results.");
            var reranked = await rerankedResults.ToListAsync();
            r.Progress = 0.85;
            r.Message = "Reranking complete, adding missing information if required...";

            double progressPerItem = 0.15 / Math.Max(reranked.Count, 1);
            var taskList = reranked.OrderBy(res => res.Relevance).Take(limit).Select(async m => await AddStoreAndUserIfMissing(m, currentUser));
            foreach (var task in taskList)
            {
                var result = await task;
                r.Progress += progressPerItem;
                yield return result;
            }
        }
        else
        {
            r.Progress = 0.4;
            r.Message = "Vector search did not return any results. Falling back to augmented search...";
            Log.Information(
                "[Memory Search] Vector search did not result in any results. Falling back to augmented search...");
            var augmentedResults = await SearchAugmented(query, options, limit, relevanceThreshold);
            var rerankedResults = _reranker.Rerank(augmentedResults);
            var reranked = await rerankedResults.ToListAsync();
            var taskList = reranked.OrderBy(res => res.Relevance).Take(limit).Select(async m => await AddStoreAndUserIfMissing(m, currentUser));
            double progressPerItem = 0.15 / Math.Max(reranked.Count, 1);
            foreach (var task in taskList)
            {
                r.Progress += progressPerItem;
                yield return await task;
            }
        }
        r.Message = "Memory search complete.";
        r.Progress = 1;
        r.IsComplete = true;
    }

    public Task<ObservableMemorySearchResult> SearchAsync(string query, int limit = 10, double relevanceThreshold = 0.5, MemoryQueryOptions? options = null)
    {
        var searchTask = new ObservableMemorySearchResult(r => HybridSearchWithReranking(r, query, limit, relevanceThreshold, options ?? MemoryQueryOptions.Default));
        return Task.FromResult(searchTask);
    }

    private Task<MemorySearchResult> AddStoreAndUserIfMissing(MemorySearchResult result, User requestingUser)
    {
        var memoryStores = requestingUser.Stores;
        var userGroups = requestingUser.Memberships.Select(m => m.Group).ToList();
        if (memoryStores.FirstOrDefault(s => s.StoreId == result.Memory.StoreId) is
            { } existingStore
            && memoryStores.SelectMany(s => s.Memories).FirstOrDefault(m => m.Identifier == result.Memory.Identifier) is { } memory)
        {
            result.Memory.Store = existingStore;
            result.Memory.OwnerUser = memory.OwnerUser;
            result.Memory.OwnerGroup = memory.OwnerGroup;
            return Task.FromResult(result);
        }

        if (userGroups.FirstOrDefault(g => g.Stores.Any(s => s.Memories.Any(groupMem => groupMem.Identifier == result.Memory.Identifier))) is { } owningGroup)
        {
            result.Memory.OwnerGroup = owningGroup;
            result.Memory.GroupId = owningGroup.Id;
            result.Memory.Store = owningGroup.Stores.FirstOrDefault(s => s.StoreId == result.Memory.StoreId);
        }

        return Task.FromResult(result);
    }

    private async Task<IEnumerable<MemorySearchResult>> SearchAugmented(string query, MemoryQueryOptions options, int limit = 10,
        double relevanceThreshold = 0.5)
    {
        var groupStores = options.GroupQueryOptions.GroupSearchMode == GroupSearchMode.None
            ? new List<MemoryStore>()
            : (await _currentUserService.GetCurrentUserAsync()).Memberships
            .Select(m => m.Group).Where(g => options.GroupQueryOptions.GroupShouldBeIncluded(g))
            .SelectMany(g => g.Stores).ToList();

        var userStores = await _dbContext.MemoryStores.ToListAsync();
        var userOwnedStores = userStores.Where(s => s.GroupId == null);
        if (!options.SearchInPersonal)
        {
            userOwnedStores = Enumerable.Empty<MemoryStore>();
        }

        var relevantStores = userOwnedStores.Concat(groupStores).DistinctBy(s => s.StoreId).ToList();

        if (options.StoreQueryOptions.StoreSearchMode < StoreSearchMode.All)
        {
            relevantStores = relevantStores.Where(s => options.StoreQueryOptions.StoreShouldBeIncluded(s)).ToList();
        }

        var storeRankings = (await _storeRouter.RankStoresAsync(query, relevantStores)).ToList();

        double topScore = storeRankings.FirstOrDefault()?.Relevance ?? double.MinValue;

        if (Math.Abs(topScore - double.MinValue) < 0.1)
        {
            return Enumerable.Empty<MemorySearchResult>();
        }

        storeRankings = storeRankings.Where(r => r.Relevance >= topScore * 0.5).ToList();

        var rankedStores = relevantStores.Where(s => storeRankings.Any(r => r.Identifier == s.StoreId)).ToList();

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
        var vectorResults = await _qdrantMemoryClient.SearchInStoreAsync(query, limit, relevanceThreshold, chunk.StoreId, (await _currentUserService.GetCurrentUserAsync()).Id, token);

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
        if (await _dbContext.MemoryStores.FirstOrDefaultAsync(s => s.StoreId == store.StoreId) is { } existingStore)
        {
            foreach (var memory in store.Memories)
            {
                existingStore.Memories.Add(memory);
            }
            await _qdrantMemoryClient.UpsertMemoryStoreAsync(await _currentUserService.GetCurrentUserAsync(), store);
            await _dbContext.SaveChangesAsync();
            return existingStore;
        }
        await _dbContext.MemoryStores.AddAsync(store);
        await _dbContext.SaveChangesAsync();
        await _qdrantMemoryClient.UpsertMemoryStoreAsync(await _currentUserService.GetCurrentUserAsync(), store);
        return store;
    }

    public async Task<MemoryStore?> CreateCollectionAsync(string collectionTitle, string storeDescription)
    {
        var currentUser = await _currentUserService.GetCurrentUserAsync();
        var newStore = new MemoryStore
        {
            Title = collectionTitle,
            Description = storeDescription,
            StoreId = Guid.NewGuid(),
            OwnerUser = currentUser,
            UserId = currentUser.Id
        };
        await _dbContext.MemoryStores.AddAsync(newStore);
        await _dbContext.SaveChangesAsync();
        return newStore;
    }

    public async Task<bool> CreateMemoryEntryAsync(Memory memory)
    {
        var targetStoreRoutingResult = await _storeRouter.RouteMemoryToStoreAsync(memory, await _dbContext.MemoryStores.ToListAsync());
        var targetStore = await _dbContext.MemoryStores.FirstOrDefaultAsync(s => s.StoreId == targetStoreRoutingResult.Identifier);

        if (targetStore == null)
        {
            string title = await _augmentationService.GenerateStoreTitleAsync(string.Empty, [memory]);
            string description = await _augmentationService.GenerateStoreDescriptionAsync(title, [memory]);
            targetStore = await CreateCollectionAsync(title, description);
        }

        if (targetStore == null)
        {
            return false;
        }

        memory.StoreId = targetStore.StoreId;
        memory.Store = targetStore;
        await _dbContext.Memories.AddAsync(memory);
        await _dbContext.SaveChangesAsync();
        await _qdrantMemoryClient.UpsertMemoryAsync(await _currentUserService.GetCurrentUserAsync(), memory);
        return true;
    }

    public async Task<bool> CreateMemoryEntryAsync(Guid collectionIdentifier, Memory memory, string storeDescription = "")
    {
        var currentUser = await _currentUserService.GetCurrentUserAsync();
        var targetStore = await _dbContext.MemoryStores.FirstOrDefaultAsync(s => s.StoreId == collectionIdentifier);
        if (targetStore == null)
        {
            string title = await _augmentationService.GenerateStoreTitleAsync(storeDescription, [memory]);
            targetStore = await CreateCollectionAsync(title, storeDescription);
        }

        if (targetStore == null)
        {
            return false;
        }

        memory.StoreId = targetStore.StoreId;
        memory.Store = targetStore;
        await _dbContext.Memories.AddAsync(memory);
        await _dbContext.SaveChangesAsync();
        await _qdrantMemoryClient.UpsertMemoryAsync(currentUser, memory);
        return true;
    }

    public async Task<bool> CreateMemoryEntryAsync(string collectionTitle, Memory memory, string storeDescription = "")
    {
        var currentUser = await _currentUserService.GetCurrentUserAsync();
        var targetStore = await _dbContext.MemoryStores.FirstOrDefaultAsync(s => s.Title == collectionTitle) ?? await CreateCollectionAsync(collectionTitle, storeDescription);

        if (targetStore == null)
        {
            return false;
        }
        memory.StoreId = targetStore.StoreId;
        memory.Store = targetStore;
        await _dbContext.Memories.AddAsync(memory);
        _dbContext.MemoryStores.Update(targetStore);
        await _dbContext.SaveChangesAsync();
        await _qdrantMemoryClient.UpsertMemoryAsync(currentUser, memory);
        return true;
    }

    public async Task<bool> ReplaceCollectionAsync(Guid collectionIdentifier, MemoryStore newStore)
    {
        var currentUser = await _currentUserService.GetCurrentUserAsync();
        var existingStore = await _dbContext.MemoryStores
            .Include(s => s.Memories)
            .FirstOrDefaultAsync(s => s.StoreId == collectionIdentifier);

        if (existingStore == null)
        {
            return false;
        }

        await _qdrantMemoryClient.DeleteMemoryStoreAsync(currentUser.Id, existingStore);

        _dbContext.MemoryStores.Remove(existingStore);

        newStore.StoreId = collectionIdentifier;
        newStore.OwnerUser = currentUser;
        await _dbContext.MemoryStores.AddAsync(newStore);

        await _dbContext.SaveChangesAsync();

        await _qdrantMemoryClient.UpsertMemoryStoreAsync(currentUser, newStore);

        return true;
    }

    public async Task<bool> ReplaceCollectionAsync(string collectionTitle, MemoryStore newStore)
    {
        var currentUser = await _currentUserService.GetCurrentUserAsync();
        var existingStore = await _dbContext.MemoryStores
            .Include(s => s.Memories)
            .FirstOrDefaultAsync(s => s.Title == collectionTitle);

        if (existingStore == null)
        {
            return false;
        }

        await _qdrantMemoryClient.DeleteMemoryStoreAsync(currentUser.Id, existingStore);

        _dbContext.MemoryStores.Remove(existingStore);

        newStore.StoreId = existingStore.StoreId;
        newStore.OwnerUser = currentUser;
        await _dbContext.MemoryStores.AddAsync(newStore);

        await _dbContext.SaveChangesAsync();

        await _qdrantMemoryClient.UpsertMemoryStoreAsync(currentUser, newStore);

        return true;
    }

    public async Task<bool> ReplaceMemoryEntryAsync(Guid entryIdentifier, Memory newMemory)
    {
        var currentUser = await _currentUserService.GetCurrentUserAsync();
        var existingMemory = await _dbContext.Memories
            .FirstOrDefaultAsync(m => m.Identifier == entryIdentifier);

        if (existingMemory == null)
        {
            return false;
        }

        await _qdrantMemoryClient.DeleteMemoryAsync(currentUser.Id, entryIdentifier);

        _dbContext.Memories.Remove(existingMemory);

        newMemory.Identifier = entryIdentifier;
        newMemory.StoreId = existingMemory.StoreId;
        newMemory.Owner = currentUser.Id;
        newMemory.UpdatedAt = DateTimeOffset.UtcNow;

        await _dbContext.Memories.AddAsync(newMemory);

        await _dbContext.SaveChangesAsync();

        await _qdrantMemoryClient.UpsertMemoryAsync(currentUser, newMemory);

        return true;
    }

    public async Task<bool> ReplaceMemoryEntryAsync(Guid collectionIdentifier, Guid entryIdentifier, Memory newMemory)
    {
        var currentUser = await _currentUserService.GetCurrentUserAsync();
        var existingMemory = await _dbContext.Memories
            .FirstOrDefaultAsync(m => m.Identifier == entryIdentifier && m.StoreId == collectionIdentifier);

        if (existingMemory == null)
        {
            return false;
        }

        await _qdrantMemoryClient.DeleteMemoryAsync(currentUser.Id, entryIdentifier);

        _dbContext.Memories.Remove(existingMemory);

        newMemory.Identifier = entryIdentifier;
        newMemory.StoreId = collectionIdentifier;
        newMemory.Owner = currentUser.Id;
        newMemory.UpdatedAt = DateTimeOffset.UtcNow;

        await _dbContext.Memories.AddAsync(newMemory);

        await _dbContext.SaveChangesAsync();

        await _qdrantMemoryClient.UpsertMemoryAsync(currentUser, newMemory);

        return true;
    }

    public async Task<bool> ReplaceMemoryEntryAsync(string collectionTitle, Guid entryIdentifier, Memory newMemory)
    {
        var currentUser = await _currentUserService.GetCurrentUserAsync();
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

        await _qdrantMemoryClient.DeleteMemoryAsync(currentUser.Id, entryIdentifier);

        _dbContext.Memories.Remove(existingMemory);

        newMemory.Identifier = entryIdentifier;
        newMemory.StoreId = targetStore.StoreId;
        newMemory.Owner = currentUser.Id;
        newMemory.UpdatedAt = DateTimeOffset.UtcNow;

        await _dbContext.Memories.AddAsync(newMemory);

        await _dbContext.SaveChangesAsync();

        await _qdrantMemoryClient.UpsertMemoryAsync(currentUser, newMemory);

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

        foreach (var memory in newStore.Memories.Except(existingStore.Memories))
        {
            existingStore.Memories.Add(memory);
        }
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

        foreach (var memory in newStore.Memories.Except(existingStore.Memories))
        {
            existingStore.Memories.Add(memory);
        }
        await _dbContext.SaveChangesAsync();

        return true;
    }

    public async Task<bool> UpdateMemoryEntryAsync(Guid entryIdentifier, Memory newMemory)
    {
        var currentUser = await _currentUserService.GetCurrentUserAsync();
        var existingMemory = await _dbContext.Memories
            .FirstOrDefaultAsync(m => m.Identifier == entryIdentifier);

        if (existingMemory == null)
        {
            return false;
        }

        await _dbContext.Memories.Where(m => m.Identifier == existingMemory.Identifier).ExecuteUpdateAsync(m =>
            m
            .SetProperty(p => p.Title, newMemory.Title)
            .SetProperty(p => p.Description, newMemory.Description)
            .SetProperty(p => p.Content, newMemory.Content)
            .SetProperty(p => p.Pinned, newMemory.Pinned)
            .SetProperty(p => p.Tags, existingMemory.Tags)
            .SetProperty(p => p.ReferenceType, newMemory.ReferenceType)
            .SetProperty(p => p.Reference, newMemory.Reference)
            .SetProperty(p => p.UpdatedAt, DateTimeOffset.UtcNow)
        );
        await _dbContext.SaveChangesAsync();

        await _qdrantMemoryClient.UpsertMemoryAsync(currentUser, existingMemory);

        return true;
    }

    public async Task<bool> UpdateMemoryEntryAsync(Guid collectionIdentifier, Guid entryIdentifier, Memory newMemory)
    {
        var currentUser = await _currentUserService.GetCurrentUserAsync();
        var existingMemory = await _dbContext.Memories
            .FirstOrDefaultAsync(m => m.Identifier == entryIdentifier && m.StoreId == collectionIdentifier);

        if (existingMemory == null)
        {
            return false;
        }

        await _dbContext.Memories.Where(m => m.Identifier == existingMemory.Identifier).ExecuteUpdateAsync(m =>
            m.SetProperty(p => p.Title, newMemory.Title)
                .SetProperty(p => p.Description, newMemory.Description)
                .SetProperty(p => p.Content, newMemory.Content)
                .SetProperty(p => p.Pinned, newMemory.Pinned)
                .SetProperty(p => p.Tags, existingMemory.Tags)
                .SetProperty(p => p.ReferenceType, newMemory.ReferenceType)
                .SetProperty(p => p.Reference, newMemory.Reference)
                .SetProperty(p => p.UpdatedAt, DateTimeOffset.UtcNow));
        await _dbContext.SaveChangesAsync();

        await _qdrantMemoryClient.UpsertMemoryAsync(currentUser, existingMemory);

        return true;
    }

    public async Task<bool> UpdateMemoryEntryAsync(string collectionTitle, Guid entryIdentifier, Memory newMemory)
    {
        var currentUser = await _currentUserService.GetCurrentUserAsync();
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


        await _dbContext.Memories.Where(m => m.Identifier == existingMemory.Identifier).ExecuteUpdateAsync(m =>
            m.SetProperty(p => p.Title, newMemory.Title)
                .SetProperty(p => p.Description, newMemory.Description)
                .SetProperty(p => p.Content, newMemory.Content)
                .SetProperty(p => p.Pinned, newMemory.Pinned)
                .SetProperty(p => p.Tags, existingMemory.Tags)
                .SetProperty(p => p.ReferenceType, newMemory.ReferenceType)
                .SetProperty(p => p.Reference, newMemory.Reference)
                .SetProperty(p => p.UpdatedAt, DateTimeOffset.UtcNow)
        );
        await _dbContext.SaveChangesAsync();

        await _qdrantMemoryClient.UpsertMemoryAsync(currentUser, existingMemory);

        return true;
    }

    public async Task<bool> DeleteCollectionAsync(Guid collectionIdentifier)
    {
        var currentUser = await _currentUserService.GetCurrentUserAsync();
        var targetStore = await _dbContext.MemoryStores
            .FirstOrDefaultAsync(s => s.StoreId == collectionIdentifier);

        if (targetStore == null)
        {
            return false;
        }

        await _qdrantMemoryClient.DeleteMemoryStoreAsync(currentUser.Id, targetStore);

        _dbContext.MemoryStores.Remove(targetStore);
        await _dbContext.SaveChangesAsync();

        return true;
    }

    public async Task<bool> DeleteCollectionAsync(string collectionTitle)
    {
        var currentUser = await _currentUserService.GetCurrentUserAsync();
        var targetStore = await _dbContext.MemoryStores
            .Include(s => s.Memories)
            .FirstOrDefaultAsync(s => s.Title == collectionTitle);

        if (targetStore == null)
        {
            return false;
        }

        await _qdrantMemoryClient.DeleteMemoryStoreAsync(currentUser.Id, targetStore);

        _dbContext.MemoryStores.Remove(targetStore);
        await _dbContext.SaveChangesAsync();

        return true;
    }

    public async Task<bool> DeleteMemoryEntryAsync(Guid collectionIdentifier, Guid entryIdentifier)
    {
        var currentUser = await _currentUserService.GetCurrentUserAsync();
        var targetMemory = await _dbContext.Memories
            .FirstOrDefaultAsync(m => m.Identifier == entryIdentifier && m.StoreId == collectionIdentifier);

        if (targetMemory == null)
        {
            return false;
        }

        await _qdrantMemoryClient.DeleteMemoryAsync(currentUser.Id, entryIdentifier);

        _dbContext.Memories.Remove(targetMemory);
        await _dbContext.SaveChangesAsync();

        return true;
    }

    public async Task<bool> DeleteMemoryEntryAsync(Guid entryIdentifier)
    {
        var currentUser = await _currentUserService.GetCurrentUserAsync();
        var targetMemory = await _dbContext.Memories
            .FirstOrDefaultAsync(m => m.Identifier == entryIdentifier);

        if (targetMemory == null)
        {
            return false;
        }

        await _qdrantMemoryClient.DeleteMemoryAsync(currentUser.Id, entryIdentifier);

        _dbContext.Memories.Remove(targetMemory);
        await _dbContext.SaveChangesAsync();

        return true;
    }

    public async Task<bool> DeleteMemoryEntryAsync(Guid collectionIdentifier, string entryTitle)
    {
        var currentUser = await _currentUserService.GetCurrentUserAsync();
        var targetMemory = await _dbContext.Memories
            .FirstOrDefaultAsync(m => m.Title == entryTitle && m.StoreId == collectionIdentifier);

        if (targetMemory == null)
        {
            return false;
        }

        await _qdrantMemoryClient.DeleteMemoryAsync(currentUser.Id, targetMemory.Identifier);

        _dbContext.Memories.Remove(targetMemory);
        await _dbContext.SaveChangesAsync();

        return true;
    }

    public async Task<bool> PublishMemoryStoreToGroup(Guid collectionIdentifier, Guid groupId)
    {
        var currentUser = await _currentUserService.GetCurrentUserAsync();
        if (currentUser.Memberships.All(m => m.Group.Id != groupId))
        {
            return false;
        }

        if (await _dbContext.MemoryStores.FirstOrDefaultAsync(s => s.StoreId == collectionIdentifier) is not { } store)
        {
            return false;
        }

        if (await _dbContext.Groups.FirstOrDefaultAsync(g => g.Id == groupId) is not { } group)
        {
            return false;
        }

        await _dbContext.MemoryStores.Where(s => s.StoreId == store.StoreId).ExecuteUpdateAsync(s =>
            s.SetProperty(p => p.GroupId, groupId)
                .SetProperty(p => p.OwnerGroup, group));
        foreach (var memory in store.Memories)
        {
            memory.GroupId = groupId;
            memory.OwnerGroup = group;
        }
        await _dbContext.SaveChangesAsync();
        await _qdrantMemoryClient.UpsertMemoryStoreAsync(group, store);
        await _qdrantMemoryClient.DeleteMemoryStoreAsync(currentUser.Id, store);
        return true;

    }
}
