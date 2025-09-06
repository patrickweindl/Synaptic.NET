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

    public HybridMemoryProvider(ICurrentUserService currentUserService, SynapticDbContext synapticDbContext, QdrantMemoryClient qdrantMemoryClient, IMemoryStoreRouter storeRouter, IMemoryAugmentationService augmentationService)
    {
        _currentUserService = currentUserService;
        _dbContext = synapticDbContext;
        _qdrantMemoryClient = qdrantMemoryClient;
        _storeRouter = storeRouter;
        _augmentationService = augmentationService;
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

    public Task<bool> CreateCollectionAsync(string collectionTitle, string storeDescription, [MaybeNullWhen(false)] out MemoryStore store)
    {
        var newStore = new MemoryStore
        {
            Title = collectionTitle,
            Description = storeDescription,
            StoreId = Guid.NewGuid(),
            OwnerUser = _currentUserService.GetCurrentUser()
        };
        _dbContext.MemoryStores.Add(newStore);
        store = newStore;
        return Task.FromResult(true);
    }

    public async Task<bool> CreateMemoryEntryAsync(Memory memory)
    {
        var targetStoreRoutingResult = await _storeRouter.RouteMemoryToStoreAsync(memory, _dbContext.MemoryStores);
        var targetStore = _dbContext.MemoryStores.FirstOrDefault(s => s.StoreId == targetStoreRoutingResult.Identifier);

        if (targetStore == null)
        {
            return false;
        }

        memory.StoreId = targetStore.StoreId;
        memory.Store = targetStore;
        targetStore.Memories.Add(memory);
        await _dbContext.SaveChangesAsync();
        await _qdrantMemoryClient.UpsertMemoryAsync(_currentUserService.GetCurrentUser().Id, memory);
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
        targetStore.Memories.Add(memory);
        await _dbContext.SaveChangesAsync();
        await _qdrantMemoryClient.UpsertMemoryAsync(_currentUserService.GetCurrentUser().Id, memory);
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
        targetStore.Memories.Add(memory);
        _dbContext.MemoryStores.Update(targetStore);
        await _dbContext.SaveChangesAsync();
        await _qdrantMemoryClient.UpsertMemoryAsync(_currentUserService.GetCurrentUser().Id, memory);
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

        await _qdrantMemoryClient.UpsertMemoryStoreAsync(_currentUserService.GetCurrentUser().Id, newStore);

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

        await _qdrantMemoryClient.UpsertMemoryStoreAsync(_currentUserService.GetCurrentUser().Id, newStore);

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

        await _qdrantMemoryClient.UpsertMemoryAsync(_currentUserService.GetCurrentUser().Id, newMemory);

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

        await _qdrantMemoryClient.UpsertMemoryAsync(_currentUserService.GetCurrentUser().Id, newMemory);

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

        await _qdrantMemoryClient.UpsertMemoryAsync(_currentUserService.GetCurrentUser().Id, newMemory);

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
        existingMemory.Tags = newMemory.Tags;
        existingMemory.ReferenceType = newMemory.ReferenceType;
        existingMemory.Reference = newMemory.Reference;
        existingMemory.UpdatedAt = DateTimeOffset.UtcNow;

        _dbContext.Memories.Update(existingMemory);
        await _dbContext.SaveChangesAsync();

        await _qdrantMemoryClient.UpsertMemoryAsync(_currentUserService.GetCurrentUser().Id, existingMemory);

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

        await _qdrantMemoryClient.UpsertMemoryAsync(_currentUserService.GetCurrentUser().Id, existingMemory);

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

        await _qdrantMemoryClient.UpsertMemoryAsync(_currentUserService.GetCurrentUser().Id, existingMemory);

        return true;
    }

    public async Task<bool> DeleteCollectionAsync(Guid collectionIdentifier)
    {
        var targetStore = await _dbContext.MemoryStores
            .Include(s => s.Memories)
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
}
