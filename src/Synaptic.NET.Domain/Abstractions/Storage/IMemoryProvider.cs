using System.Diagnostics.CodeAnalysis;
using Synaptic.NET.Domain.Resources.Storage;

namespace Synaptic.NET.Domain.Abstractions.Storage;

/// <summary>
/// Provides an interface for managing memory storage, including operations for creating, updating, replacing, and deleting memory collections and entries.
/// Implementations should be scoped and thread-safe.
/// </summary>
public interface IMemoryProvider
{
    /// <summary>
    /// Retrieves a read-only dictionary containing identifiers and descriptions of all available stores.
    /// </summary>
    /// <returns>A task representing the operation, returning a dictionary where the keys are store identifiers and the values are their descriptions.</returns>
    Task<IReadOnlyDictionary<Guid, string>> GetStoreIdentifiersAndDescriptionsAsync();

    /// <summary>
    /// Retrieves a list of all available memory stores, each representing a collection of related memories grouped by a common topic.
    /// </summary>
    /// <returns>A task representing the operation, returning a list of memory stores.</returns>
    Task<List<MemoryStore>> GetStoresAsync();

    /// <summary>
    /// Retrieves a memory store by its unique identifier.
    /// </summary>
    /// <param name="collectionIdentifier">The unique identifier of the memory store to retrieve.</param>
    /// <returns>A task representing the operation, returning the memory store if found; otherwise, null.</returns>
    Task<MemoryStore?> GetCollectionAsync(Guid collectionIdentifier);

    /// <summary>
    /// Retrieves a memory collection based on the provided collection title.
    /// </summary>
    /// <param name="collectionTitle">The title of the memory collection to retrieve.</param>
    /// <returns>A task representing the asynchronous operation, returning the memory collection associated with the specified title, or null if not found.</returns>
    Task<MemoryStore?> GetCollectionAsync(string collectionTitle);

    /// <summary>
    /// Executes a search query across memory collections based on specified criteria.
    /// </summary>
    /// <param name="query">The search query string used to match memory entries.</param>
    /// <param name="limit">The maximum number of search results to return. Defaults to 10.</param>
    /// <param name="relevanceThreshold">The minimum relevance score required for a result to be included. Defaults to 0.5.</param>
    /// <returns>A task representing the operation, returning a collection of memory search results that meet the specified criteria.</returns>
    Task<IEnumerable<MemorySearchResult>> SearchAsync(string query, int limit = 10, double relevanceThreshold = 0.5);

    /// <summary>
    /// Creates a new memory collection with the provided title.
    /// </summary>
    /// <param name="store">The memory store to create.</param>
    /// <returns>The created memory store, returning a value if the collection was successfully created, otherwise null.</returns>
    Task<MemoryStore?> CreateCollectionAsync(MemoryStore store);

    /// <summary>
    /// Creates a new memory collection with the provided title.
    /// </summary>
    /// <param name="collectionTitle">The title of the collection to create.</param>
    /// <param name="storeDescription">An optional description of the collection to be created.</param>
    /// <param name="memoryStore">The created memory store if successful.</param>
    /// <returns>A task representing the operation, returning true if the collection was successfully created, otherwise false.</returns>
    Task<bool> CreateCollectionAsync(string collectionTitle, string storeDescription, [MaybeNullWhen(false)] out MemoryStore memoryStore);

    /// <summary>
    /// Creates a new memory entry and routes it to the most suitable collection.
    /// </summary>
    /// <param name="memory">The memory object containing the details of the memory entry to be created.</param>
    /// <returns>A task representing the operation, returning true if the memory entry was created successfully; otherwise, false.</returns>
    Task<bool> CreateMemoryEntryAsync(Memory memory);

    /// <summary>
    /// Creates a new memory entry in the specified collection.
    /// </summary>
    /// <param name="collectionIdentifier">The unique identifier of the collection in which the memory will be created.</param>
    /// <param name="memory">The memory object containing the details of the memory entry to be created.</param>
    /// <param name="storeDescription">An optional store description provided for richer context.</param>
    /// <returns>A task representing the operation, returning true if the memory entry was created successfully; otherwise, false.</returns>
    Task<bool> CreateMemoryEntryAsync(Guid collectionIdentifier, Memory memory, string storeDescription = "");

    /// <summary>
    /// Creates a new memory entry within the specified collection.
    /// </summary>
    /// <param name="collectionTitle">The unique identifier of the collection in which the memory entry will be created.</param>
    /// <param name="memory">The memory object containing details of the entry to be added.</param>
    /// <param name="storeDescription">An optional store description provided for richer context.</param>
    /// <returns>A task representing the asynchronous operation, returning true if the memory entry was created successfully; otherwise, false.</returns>
    Task<bool> CreateMemoryEntryAsync(string collectionTitle, Memory memory, string storeDescription = "");

    /// <summary>
    /// Replaces an existing memory collection identified by its unique identifier with a new memory store.
    /// </summary>
    /// <param name="collectionIdentifier">The unique identifier of the collection to be replaced.</param>
    /// <param name="newStore">The new memory store that will replace the existing collection.</param>
    /// <returns>A task representing the operation, returning true if the replacement was successful, otherwise false.</returns>
    Task<bool> ReplaceCollectionAsync(Guid collectionIdentifier, MemoryStore newStore);

    /// <summary>
    /// Replaces an existing memory collection with a new memory store based on the collection identifier.
    /// </summary>
    /// <param name="collectionTitle">The unique identifier of the collection to replace.</param>
    /// <param name="newStore">The new memory store to replace the current collection with.</param>
    /// <returns>A task representing the operation, returning true if the collection was successfully replaced, otherwise false.</returns>
    Task<bool> ReplaceCollectionAsync(string collectionTitle, MemoryStore newStore);

    /// <summary>
    /// Replaces an existing memory entry within a collection identified by the specified collection identifier.
    /// </summary>
    /// <param name="entryIdentifier">The unique identifier of the memory entry to be replaced.</param>
    /// <param name="newMemory">The new memory object that replaces the existing memory entry.</param>
    /// <returns>A task representing the operation, returning a boolean indicating whether the replacement was successful.</returns>
    Task<bool> ReplaceMemoryEntryAsync(Guid entryIdentifier, Memory newMemory);

    /// <summary>
    /// Replaces an existing memory entry within a collection identified by the specified collection identifier.
    /// </summary>
    /// <param name="collectionIdentifier">The unique identifier of the collection that contains the memory entry to be replaced.</param>
    /// <param name="entryIdentifier">The unique identifier of the memory entry to be replaced.</param>
    /// <param name="newMemory">The new memory object that replaces the existing memory entry.</param>
    /// <returns>A task representing the operation, returning a boolean indicating whether the replacement was successful.</returns>
    Task<bool> ReplaceMemoryEntryAsync(Guid collectionIdentifier, Guid entryIdentifier, Memory newMemory);

    /// <summary>
    /// Replaces an existing memory entry within a specified collection.
    /// </summary>
    /// <param name="collectionTitle">The title of the collection containing the memory entry to be replaced.</param>
    /// <param name="entryIdentifier">The unique identifier of the memory entry to replace.</param>
    /// <param name="newMemory">The new memory object to replace the existing memory entry.</param>
    /// <returns>A task that represents the operation, returning a boolean value indicating whether the replacement was successful.</returns>
    Task<bool> ReplaceMemoryEntryAsync(string collectionTitle, Guid entryIdentifier, Memory newMemory);

    /// <summary>
    /// Updates an existing memory collection with the specified identifier using the provided new memory store.
    /// </summary>
    /// <param name="collectionIdentifier">The unique identifier of the collection to update.</param>
    /// <param name="newStore">The new memory store to replace the existing collection.</param>
    /// <returns>A task representing the operation, returning true if the collection was successfully updated, otherwise false.</returns>
    Task<bool> UpdateCollectionAsync(Guid collectionIdentifier, MemoryStore newStore);

    /// <summary>
    /// Updates an existing memory collection with the provided new memory store using its unique identifier.
    /// </summary>
    /// <param name="collectionTitle">The unique identifier of the collection to update.</param>
    /// <param name="newStore">The new memory store containing updated information for the collection.</param>
    /// <returns>A task representing the operation, returning true if the collection was successfully updated, otherwise false.</returns>
    Task<bool> UpdateCollectionAsync(string collectionTitle, MemoryStore newStore);

    /// <summary>
    /// Updates an existing memory entry within a specified collection.
    /// </summary>
    /// <param name="entryIdentifier">The identifier of the memory entry to be updated.</param>
    /// <param name="newMemory">The new memory data to update the existing entry with.</param>
    /// <returns>A task representing the operation, returning a boolean indicating whether the update was successful.</returns>
    Task<bool> UpdateMemoryEntryAsync(Guid entryIdentifier, Memory newMemory);

    /// <summary>
    /// Updates an existing memory entry within a specified collection.
    /// </summary>
    /// <param name="collectionIdentifier">The identifier of the collection containing the memory entry to be updated.</param>
    /// <param name="entryIdentifier">The identifier of the memory entry to be updated.</param>
    /// <param name="newMemory">The new memory data to update the existing entry with.</param>
    /// <returns>A task representing the operation, returning a boolean indicating whether the update was successful.</returns>
    Task<bool> UpdateMemoryEntryAsync(Guid collectionIdentifier, Guid entryIdentifier, Memory newMemory);

    /// <summary>
    /// Updates an existing memory entry in the specified collection by its title and entry identifier.
    /// </summary>
    /// <param name="collectionTitle">The title of the collection containing the memory entry to update.</param>
    /// <param name="entryIdentifier">The unique identifier of the memory entry to be updated.</param>
    /// <param name="newMemory">The new memory object containing the updated data.</param>
    /// <returns>A task representing the operation, returning a boolean value indicating whether the update was successful.</returns>
    Task<bool> UpdateMemoryEntryAsync(string collectionTitle, Guid entryIdentifier, Memory newMemory);

    /// <summary>
    /// Deletes a collection by its identifier.
    /// </summary>
    /// <param name="collectionIdentifier">The unique identifier of the collection to delete.</param>
    /// <returns>True if the collection was successfully deleted, otherwise false.</returns>
    Task<bool> DeleteCollectionAsync(Guid collectionIdentifier);

    /// <summary>
    /// Deletes a collection by title. Should not delete anything if results are ambiguous.
    /// </summary>
    /// <param name="collectionTitle">The title of the collection to delete.</param>
    /// <returns>True if the collection was deleted, otherwise false.</returns>
    Task<bool> DeleteCollectionAsync(string collectionTitle);

    /// <summary>
    /// Deletes a memory entry.
    /// </summary>
    /// <param name="entryIdentifier">The identifier of the memory entry to delete.</param>
    /// <returns>True if the memory entry was deleted, otherwise false.</returns>
    Task<bool> DeleteMemoryEntryAsync(Guid entryIdentifier);

    /// <summary>
    /// Deletes a memory entry from a specified collection.
    /// </summary>
    /// <param name="collectionIdentifier">The identifier of the collection containing the memory entry.</param>
    /// <param name="entryIdentifier">The identifier of the memory entry to delete.</param>
    /// <returns>True if the memory entry was deleted, otherwise false.</returns>
    Task<bool> DeleteMemoryEntryAsync(Guid collectionIdentifier, Guid entryIdentifier);

    /// <summary>
    /// Deletes a memory entry from a specified collection by its title.
    /// </summary>
    /// <param name="collectionIdentifier">The identifier of the collection containing the memory entry.</param>
    /// <param name="entryTitle">The title of the memory entry to delete.</param>
    /// <returns>True if the memory entry was successfully deleted, otherwise false.</returns>
    Task<bool> DeleteMemoryEntryAsync(Guid collectionIdentifier, string entryTitle);

    /// <summary>
    /// Publishes the specified memory store to a group, making its data accessible within the group's context.
    /// </summary>
    /// <param name="collectionIdentifier">The unique identifier of the memory store to be published.</param>
    /// <param name="groupId">The unique identifier of the group to which the memory store is being published.</param>
    /// <returns>A task that represents the operation. Returns true if the memory store is successfully published; otherwise, false.</returns>
    Task<bool> PublishMemoryStoreToGroup(Guid collectionIdentifier, Guid groupId);
}
