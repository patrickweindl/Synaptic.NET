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
    private readonly IDbContextFactory<SynapticDbContext> _dbContextFactory;
    private readonly QdrantMemoryClient _qdrantMemoryClient;
    private readonly ICurrentUserService _currentUserService;
    private readonly IMemoryStoreRouter _storeRouter;
    private readonly IMemoryAugmentationService _augmentationService;
    private readonly IMemoryQueryResultReranker _reranker;
    private readonly User _currentUser; // This service should strictly be used scoped.

    public HybridMemoryProvider(
        ICurrentUserService currentUserService,
        IDbContextFactory<SynapticDbContext> synapticDbContextFactory,
        QdrantMemoryClient qdrantMemoryClient,
        IMemoryStoreRouter storeRouter,
        IMemoryAugmentationService augmentationService,
        IMemoryQueryResultReranker reranker)
    {
        _currentUserService = currentUserService;
        _currentUser = Task.Run(async () => await _currentUserService.GetCurrentUserAsync()).Result;
        _dbContextFactory = synapticDbContextFactory;
        _qdrantMemoryClient = qdrantMemoryClient;
        _storeRouter = storeRouter;
        _augmentationService = augmentationService;
        _reranker = reranker;
    }

    public async Task<IReadOnlyDictionary<Guid, string>> GetStoreIdentifiersAndDescriptionsAsync()
    {
        await using var dbContext = await _dbContextFactory.CreateDbContextAsync();
        await dbContext.SetCurrentUserAsync(_currentUser);
        var result = await dbContext.MemoryStores.ToDictionaryAsync(s => s.StoreId, s => s.Description);
        return result;
    }

    public async Task<List<MemoryStore>> GetStoresAsync()
    {
        await using var dbContext = await _dbContextFactory.CreateDbContextAsync();
        await dbContext.SetCurrentUserAsync(_currentUser);
        return await dbContext.MemoryStores.ToListAsync();
    }

    public async Task<MemoryStore?> GetCollectionAsync(Guid collectionIdentifier)
    {
        await using var dbContext = await _dbContextFactory.CreateDbContextAsync();
        await dbContext.SetCurrentUserAsync(_currentUser);
        return await dbContext.MemoryStores.FirstOrDefaultAsync(s => s.StoreId == collectionIdentifier);
    }

    public async Task<MemoryStore?> GetCollectionAsync(string collectionTitle)
    {
        await using var dbContext = await _dbContextFactory.CreateDbContextAsync();
        await dbContext.SetCurrentUserAsync(_currentUser);
        return await dbContext.MemoryStores.FirstOrDefaultAsync(s => s.Title == collectionTitle);
    }

    private async IAsyncEnumerable<MemorySearchResult> HybridSearchWithReranking(ObservableMemorySearchResult r, string query, int limit, double relevanceThreshold, MemoryQueryOptions options)
    {
        await using var dbContext = await _dbContextFactory.CreateDbContextAsync();
        await dbContext.SetCurrentUserAsync(_currentUser);
        r.Message = "Memory query received. Starting memory search...";
        var userVectorResults = Enumerable.Empty<MemorySearchResult>();

        if (options.SearchInPersonal)
        {
            if (options.StoreQueryOptions.StoreSearchMode < StoreSearchMode.All)
            {
                var storeFilteredResults = new List<MemorySearchResult>();
                foreach (var store in dbContext.MemoryStores.Where(s => s.GroupId == null))
                {
                    if (options.StoreQueryOptions.StoreShouldBeIncluded(store))
                    {
                        storeFilteredResults.AddRange(await _qdrantMemoryClient.SearchInStoreAsync(query, limit, relevanceThreshold,
                            options.StoreQueryOptions.StoreIds.FirstOrDefault(), _currentUser.Id));
                    }
                }
                userVectorResults = storeFilteredResults;
            }
            else
            {
                userVectorResults = await _qdrantMemoryClient.SearchAsync(query, limit, relevanceThreshold, _currentUser);
            }

        }

        if (options.GroupQueryOptions.GroupSearchMode > GroupSearchMode.None)
        {
            foreach (var group in _currentUser.Memberships.Select(m => m.Group).Where(g => options.GroupQueryOptions.GroupShouldBeIncluded(g)))
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
            var taskList = reranked.OrderBy(res => res.Relevance).Take(limit).Select(async m => await AddStoreAndUserIfMissingAsync(m, _currentUser));
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
            var taskList = reranked.OrderBy(res => res.Relevance).Take(limit).Select(async m => await AddStoreAndUserIfMissingAsync(m, _currentUser));
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

    private async Task<MemorySearchResult> AddStoreAndUserIfMissingAsync(MemorySearchResult result, User requestingUser)
    {
        await using var dbContext = await _dbContextFactory.CreateDbContextAsync();
        await dbContext.SetCurrentUserAsync(requestingUser);
        var memoryStores = await dbContext.MemoryStores
            .Include(memoryStore => memoryStore.Memories)
            .ThenInclude(mem => mem.OwnerUser)
            .Include(memoryStore => memoryStore.Memories)
            .ThenInclude(m => m.OwnerGroup).ToListAsync();
        if (memoryStores.FirstOrDefault(s => s.StoreId == result.Memory.StoreId) is
            { } existingStore
            && memoryStores.SelectMany(s => s.Memories).FirstOrDefault(m => m.Identifier == result.Memory.Identifier) is { } memory)
        {
            result.Memory.Store = existingStore;
            result.Memory.OwnerUser = memory.OwnerUser;
            result.Memory.OwnerGroup = memory.OwnerGroup;
        }
        return result;
    }

    private async Task<IEnumerable<MemorySearchResult>> SearchAugmented(string query, MemoryQueryOptions options, int limit = 10,
        double relevanceThreshold = 0.5)
    {
        await using var dbContext = await _dbContextFactory.CreateDbContextAsync();
        await dbContext.SetCurrentUserAsync(_currentUser);

        var userStores = await dbContext.MemoryStores.ToListAsync();
        var userOwnedStores = userStores.Where(s => s.GroupId == null);
        var groupStores = options.GroupQueryOptions.GroupSearchMode == GroupSearchMode.None
            ? new List<MemoryStore>()
            : userStores.Where(s => s.GroupId != null).ToList();
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
        await using var dbContext = await _dbContextFactory.CreateDbContextAsync();
        await dbContext.SetCurrentUserAsync(_currentUser);
        if (await dbContext.MemoryStores.FirstOrDefaultAsync(s => s.StoreId == store.StoreId) is { } existingStore)
        {
            foreach (var memory in store.Memories)
            {
                memory.OwnerUser = null;
                existingStore.Memories.Add(memory);
            }
            await _qdrantMemoryClient.UpsertMemoryStoreAsync(await _currentUserService.GetCurrentUserAsync(), store);
            await dbContext.SaveChangesAsync();
            return existingStore;
        }

        foreach (var mem in store.Memories)
        {
            mem.Store = null;
            mem.OwnerUser = null;
            mem.OwnerGroup = null;
        }
        store.OwnerUser = null!;
        store.OwnerGroup = null!;
        await dbContext.MemoryStores.AddAsync(store);
        await dbContext.SaveChangesAsync();
        await _qdrantMemoryClient.UpsertMemoryStoreAsync(await _currentUserService.GetCurrentUserAsync(), store);
        return store;
    }

    public async Task<MemoryStore?> CreateCollectionAsync(string collectionTitle, string storeDescription)
    {
        await using var dbContext = await _dbContextFactory.CreateDbContextAsync();
        await dbContext.SetCurrentUserAsync(_currentUser);
        var newStore = new MemoryStore
        {
            Title = collectionTitle,
            Description = storeDescription,
            StoreId = Guid.NewGuid(),
            OwnerUser = _currentUser,
            UserId = _currentUser.Id
        };

        foreach (var mem in newStore.Memories)
        {
            mem.Store = null;
            mem.OwnerUser = null;
            mem.OwnerGroup = null;
        }
        newStore.OwnerUser = null!;
        newStore.OwnerGroup = null!;
        await dbContext.MemoryStores.AddAsync(newStore);
        await dbContext.SaveChangesAsync();
        return newStore;
    }

    public async Task<bool> CreateMemoryEntryAsync(Memory memory)
    {
        await using var dbContext = await _dbContextFactory.CreateDbContextAsync();
        await dbContext.SetCurrentUserAsync(_currentUser);
        var targetStoreRoutingResult = await _storeRouter.RouteMemoryToStoreAsync(memory, await dbContext.MemoryStores.ToListAsync());
        var targetStore = await dbContext.MemoryStores.FirstOrDefaultAsync(s => s.StoreId == targetStoreRoutingResult.Identifier);

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
        memory.Store = null;
        await dbContext.Memories.AddAsync(memory);
        await dbContext.SaveChangesAsync();
        await _qdrantMemoryClient.UpsertMemoryAsync(await _currentUserService.GetCurrentUserAsync(), memory);
        return true;
    }

    public async Task<bool> CreateMemoryEntryAsync(Guid collectionIdentifier, Memory memory, string storeDescription = "")
    {
        await using var dbContext = await _dbContextFactory.CreateDbContextAsync();
        await dbContext.SetCurrentUserAsync(_currentUser);
        var targetStore = await dbContext.MemoryStores.FirstOrDefaultAsync(s => s.StoreId == collectionIdentifier);
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
        memory.Store = null;
        await dbContext.Memories.AddAsync(memory);
        await dbContext.SaveChangesAsync();
        await _qdrantMemoryClient.UpsertMemoryAsync(_currentUser, memory);
        return true;
    }

    public async Task<bool> CreateMemoryEntryAsync(string collectionTitle, Memory memory, string storeDescription = "")
    {
        await using var dbContext = await _dbContextFactory.CreateDbContextAsync();
        await dbContext.SetCurrentUserAsync(_currentUser);
        var targetStore = await dbContext.MemoryStores.FirstOrDefaultAsync(s => s.Title == collectionTitle) ?? await CreateCollectionAsync(collectionTitle, storeDescription);

        if (targetStore == null)
        {
            return false;
        }
        memory.StoreId = targetStore.StoreId;
        memory.Store = null;
        await dbContext.Memories.AddAsync(memory);
        await dbContext.SaveChangesAsync();
        await _qdrantMemoryClient.UpsertMemoryAsync(_currentUser, memory);
        return true;
    }

    public async Task<bool> ReplaceCollectionAsync(Guid collectionIdentifier, MemoryStore newStore)
    {
        await using var dbContext = await _dbContextFactory.CreateDbContextAsync();
        await dbContext.SetCurrentUserAsync(_currentUser);
        var existingStore = await dbContext.MemoryStores
            .Include(s => s.Memories)
            .FirstOrDefaultAsync(s => s.StoreId == collectionIdentifier);

        if (existingStore == null)
        {
            return false;
        }

        await _qdrantMemoryClient.DeleteMemoryStoreAsync(_currentUser.Id, existingStore);

        dbContext.MemoryStores.Remove(existingStore);

        newStore.StoreId = collectionIdentifier;
        newStore.UserId = _currentUser.Id;
        newStore.OwnerUser = null!;

        await dbContext.MemoryStores.AddAsync(newStore);

        await dbContext.SaveChangesAsync();

        await _qdrantMemoryClient.UpsertMemoryStoreAsync(_currentUser, newStore);
        return true;
    }

    public async Task<bool> ReplaceCollectionAsync(string collectionTitle, MemoryStore newStore)
    {
        await using var dbContext = await _dbContextFactory.CreateDbContextAsync();
        await dbContext.SetCurrentUserAsync(_currentUser);
        var existingStore = await dbContext.MemoryStores
            .Include(s => s.Memories)
            .FirstOrDefaultAsync(s => s.Title == collectionTitle);

        if (existingStore == null)
        {
            return false;
        }

        await _qdrantMemoryClient.DeleteMemoryStoreAsync(_currentUser.Id, existingStore);

        dbContext.MemoryStores.Remove(existingStore);

        newStore.StoreId = existingStore.StoreId;
        newStore.UserId = _currentUser.Id;
        newStore.OwnerUser = null!;
        await dbContext.MemoryStores.AddAsync(newStore);

        await dbContext.SaveChangesAsync();

        await _qdrantMemoryClient.UpsertMemoryStoreAsync(_currentUser, newStore);
        return true;
    }

    public async Task<bool> ReplaceMemoryEntryAsync(Guid entryIdentifier, Memory newMemory)
    {
        await using var dbContext = await _dbContextFactory.CreateDbContextAsync();
        await dbContext.SetCurrentUserAsync(_currentUser);
        var existingMemory = await dbContext.Memories
            .FirstOrDefaultAsync(m => m.Identifier == entryIdentifier);

        if (existingMemory == null)
        {
            return false;
        }

        await _qdrantMemoryClient.DeleteMemoryAsync(_currentUser.Id, entryIdentifier);

        dbContext.Memories.Remove(existingMemory);

        newMemory.Identifier = entryIdentifier;
        newMemory.StoreId = existingMemory.StoreId;
        newMemory.Store = null;
        newMemory.Owner = _currentUser.Id;
        newMemory.OwnerUser = null!;
        newMemory.UpdatedAt = DateTimeOffset.UtcNow;

        await dbContext.Memories.AddAsync(newMemory);

        await dbContext.SaveChangesAsync();

        await _qdrantMemoryClient.UpsertMemoryAsync(_currentUser, newMemory);
        return true;
    }

    public async Task<bool> ReplaceMemoryEntryAsync(Guid collectionIdentifier, Guid entryIdentifier, Memory newMemory)
    {
        await using var dbContext = await _dbContextFactory.CreateDbContextAsync();
        await dbContext.SetCurrentUserAsync(_currentUser);
        var existingMemory = await dbContext.Memories
            .FirstOrDefaultAsync(m => m.Identifier == entryIdentifier && m.StoreId == collectionIdentifier);

        if (existingMemory == null)
        {
            return false;
        }

        await _qdrantMemoryClient.DeleteMemoryAsync(_currentUser.Id, entryIdentifier);

        dbContext.Memories.Remove(existingMemory);

        newMemory.Identifier = entryIdentifier;
        newMemory.StoreId = collectionIdentifier;
        newMemory.Store = null;
        newMemory.Owner = _currentUser.Id;
        newMemory.OwnerUser = null!;
        newMemory.UpdatedAt = DateTimeOffset.UtcNow;

        await dbContext.Memories.AddAsync(newMemory);

        await dbContext.SaveChangesAsync();

        await _qdrantMemoryClient.UpsertMemoryAsync(_currentUser, newMemory);
        return true;
    }

    public async Task<bool> ReplaceMemoryEntryAsync(string collectionTitle, Guid entryIdentifier, Memory newMemory)
    {
        await using var dbContext = await _dbContextFactory.CreateDbContextAsync();
        await dbContext.SetCurrentUserAsync(_currentUser);
        var targetStore = await dbContext.MemoryStores
            .FirstOrDefaultAsync(s => s.Title == collectionTitle);

        if (targetStore == null)
        {
            return false;
        }

        var existingMemory = await dbContext.Memories
            .FirstOrDefaultAsync(m => m.Identifier == entryIdentifier && m.StoreId == targetStore.StoreId);

        if (existingMemory == null)
        {
            return false;
        }

        await _qdrantMemoryClient.DeleteMemoryAsync(_currentUser.Id, entryIdentifier);

        dbContext.Memories.Remove(existingMemory);

        newMemory.Identifier = entryIdentifier;
        newMemory.StoreId = targetStore.StoreId;
        newMemory.Store = null;
        newMemory.Owner = _currentUser.Id;
        newMemory.OwnerUser = null!;
        newMemory.UpdatedAt = DateTimeOffset.UtcNow;

        await dbContext.Memories.AddAsync(newMemory);

        await dbContext.SaveChangesAsync();

        await _qdrantMemoryClient.UpsertMemoryAsync(_currentUser, newMemory);
        return true;
    }

    public async Task<bool> UpdateCollectionAsync(Guid collectionIdentifier, MemoryStore newStore)
    {
        await using var dbContext = await _dbContextFactory.CreateDbContextAsync();
        await dbContext.SetCurrentUserAsync(_currentUser);
        var existingStore = await dbContext.MemoryStores
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
        await dbContext.SaveChangesAsync();
        return true;
    }

    public async Task<bool> UpdateCollectionAsync(string collectionTitle, MemoryStore newStore)
    {
        await using var dbContext = await _dbContextFactory.CreateDbContextAsync();
        await dbContext.SetCurrentUserAsync(_currentUser);
        var existingStore = await dbContext.MemoryStores
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
        await dbContext.SaveChangesAsync();
        return true;
    }

    public async Task<bool> UpdateMemoryEntryAsync(Guid entryIdentifier, Memory newMemory)
    {
        await using var dbContext = await _dbContextFactory.CreateDbContextAsync();
        await dbContext.SetCurrentUserAsync(_currentUser);
        var existingMemory = await dbContext.Memories
            .FirstOrDefaultAsync(m => m.Identifier == entryIdentifier);

        if (existingMemory == null)
        {
            return false;
        }

        await dbContext.Memories.Where(m => m.Identifier == existingMemory.Identifier).ExecuteUpdateAsync(m =>
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
        await dbContext.SaveChangesAsync();

        await _qdrantMemoryClient.UpsertMemoryAsync(_currentUser, existingMemory);
        return true;
    }

    public async Task<bool> UpdateMemoryEntryAsync(Guid collectionIdentifier, Guid entryIdentifier, Memory newMemory)
    {
        await using var dbContext = await _dbContextFactory.CreateDbContextAsync();
        await dbContext.SetCurrentUserAsync(_currentUser);
        var existingMemory = await dbContext.Memories
            .FirstOrDefaultAsync(m => m.Identifier == entryIdentifier && m.StoreId == collectionIdentifier);

        if (existingMemory == null)
        {
            return false;
        }

        await dbContext.Memories.Where(m => m.Identifier == existingMemory.Identifier).ExecuteUpdateAsync(m =>
            m.SetProperty(p => p.Title, newMemory.Title)
                .SetProperty(p => p.Description, newMemory.Description)
                .SetProperty(p => p.Content, newMemory.Content)
                .SetProperty(p => p.Pinned, newMemory.Pinned)
                .SetProperty(p => p.Tags, existingMemory.Tags)
                .SetProperty(p => p.ReferenceType, newMemory.ReferenceType)
                .SetProperty(p => p.Reference, newMemory.Reference)
                .SetProperty(p => p.UpdatedAt, DateTimeOffset.UtcNow));
        await dbContext.SaveChangesAsync();

        await _qdrantMemoryClient.UpsertMemoryAsync(_currentUser, existingMemory);
        return true;
    }

    public async Task<bool> UpdateMemoryEntryAsync(string collectionTitle, Guid entryIdentifier, Memory newMemory)
    {
        await using var dbContext = await _dbContextFactory.CreateDbContextAsync();
        await dbContext.SetCurrentUserAsync(_currentUser);
        var targetStore = await dbContext.MemoryStores
            .FirstOrDefaultAsync(s => s.Title == collectionTitle);

        if (targetStore == null)
        {
            return false;
        }

        var existingMemory = await dbContext.Memories
            .FirstOrDefaultAsync(m => m.Identifier == entryIdentifier && m.StoreId == targetStore.StoreId);

        if (existingMemory == null)
        {
            return false;
        }


        await dbContext.Memories.Where(m => m.Identifier == existingMemory.Identifier).ExecuteUpdateAsync(m =>
            m.SetProperty(p => p.Title, newMemory.Title)
                .SetProperty(p => p.Description, newMemory.Description)
                .SetProperty(p => p.Content, newMemory.Content)
                .SetProperty(p => p.Pinned, newMemory.Pinned)
                .SetProperty(p => p.Tags, existingMemory.Tags)
                .SetProperty(p => p.ReferenceType, newMemory.ReferenceType)
                .SetProperty(p => p.Reference, newMemory.Reference)
                .SetProperty(p => p.UpdatedAt, DateTimeOffset.UtcNow)
        );
        await dbContext.SaveChangesAsync();

        await _qdrantMemoryClient.UpsertMemoryAsync(_currentUser, existingMemory);
        return true;
    }

    public async Task<bool> DeleteCollectionAsync(Guid collectionIdentifier)
    {
        await using var dbContext = await _dbContextFactory.CreateDbContextAsync();
        await dbContext.SetCurrentUserAsync(_currentUser);
        var targetStore = await dbContext.MemoryStores
            .FirstOrDefaultAsync(s => s.StoreId == collectionIdentifier);

        if (targetStore == null)
        {
            return false;
        }

        await _qdrantMemoryClient.DeleteMemoryStoreAsync(_currentUser.Id, targetStore);

        dbContext.MemoryStores.Remove(targetStore);
        await dbContext.SaveChangesAsync();
        return true;
    }

    public async Task<bool> DeleteCollectionAsync(string collectionTitle)
    {
        await using var dbContext = await _dbContextFactory.CreateDbContextAsync();
        await dbContext.SetCurrentUserAsync(_currentUser);
        var targetStore = await dbContext.MemoryStores
            .Include(s => s.Memories)
            .FirstOrDefaultAsync(s => s.Title == collectionTitle);

        if (targetStore == null)
        {
            return false;
        }

        await _qdrantMemoryClient.DeleteMemoryStoreAsync(_currentUser.Id, targetStore);

        dbContext.MemoryStores.Remove(targetStore);
        await dbContext.SaveChangesAsync();
        return true;
    }

    public async Task<bool> DeleteMemoryEntryAsync(Guid collectionIdentifier, Guid entryIdentifier)
    {
        await using var dbContext = await _dbContextFactory.CreateDbContextAsync();
        await dbContext.SetCurrentUserAsync(_currentUser);
        var targetMemory = await dbContext.Memories
            .FirstOrDefaultAsync(m => m.Identifier == entryIdentifier && m.StoreId == collectionIdentifier);

        if (targetMemory == null)
        {
            return false;
        }

        await _qdrantMemoryClient.DeleteMemoryAsync(_currentUser.Id, entryIdentifier);

        dbContext.Memories.Remove(targetMemory);
        await dbContext.SaveChangesAsync();
        return true;
    }

    public async Task<bool> DeleteMemoryEntryAsync(Guid entryIdentifier)
    {
        await using var dbContext = await _dbContextFactory.CreateDbContextAsync();
        await dbContext.SetCurrentUserAsync(_currentUser);
        var targetMemory = await dbContext.Memories
            .FirstOrDefaultAsync(m => m.Identifier == entryIdentifier);

        if (targetMemory == null)
        {
            return false;
        }

        await _qdrantMemoryClient.DeleteMemoryAsync(_currentUser.Id, entryIdentifier);

        dbContext.Memories.Remove(targetMemory);
        await dbContext.SaveChangesAsync();
        return true;
    }

    public async Task<bool> DeleteMemoryEntryAsync(Guid collectionIdentifier, string entryTitle)
    {
        await using var dbContext = await _dbContextFactory.CreateDbContextAsync();
        await dbContext.SetCurrentUserAsync(_currentUser);
        var targetMemory = await dbContext.Memories
            .FirstOrDefaultAsync(m => m.Title == entryTitle && m.StoreId == collectionIdentifier);

        if (targetMemory == null)
        {
            return false;
        }

        await _qdrantMemoryClient.DeleteMemoryAsync(_currentUser.Id, targetMemory.Identifier);

        dbContext.Memories.Remove(targetMemory);
        await dbContext.SaveChangesAsync();
        return true;
    }

    public async Task<bool> PublishMemoryStoreToGroup(Guid collectionIdentifier, Guid groupId)
    {
        await using var dbContext = await _dbContextFactory.CreateDbContextAsync();
        await dbContext.SetCurrentUserAsync(_currentUser);
        if (_currentUser.Memberships.All(m => m.Group.Id != groupId))
        {
            return false;
        }

        if (await dbContext.MemoryStores.FirstOrDefaultAsync(s => s.StoreId == collectionIdentifier) is not { } store)
        {
            return false;
        }

        if (await dbContext.Groups.FirstOrDefaultAsync(g => g.Id == groupId) is not { } group)
        {
            return false;
        }

        await dbContext.MemoryStores.Where(s => s.StoreId == store.StoreId).ExecuteUpdateAsync(s =>
            s.SetProperty(p => p.GroupId, groupId)
                .SetProperty(p => p.OwnerGroup, group));
        foreach (var memory in store.Memories)
        {
            memory.GroupId = groupId;
        }
        await dbContext.SaveChangesAsync();
        await _qdrantMemoryClient.UpsertMemoryStoreAsync(group, store);
        await _qdrantMemoryClient.DeleteMemoryStoreAsync(_currentUser.Id, store);
        return true;

    }
}
