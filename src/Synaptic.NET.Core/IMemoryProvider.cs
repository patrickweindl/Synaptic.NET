using Synaptic.NET.Domain.Resources;

namespace Synaptic.NET.Core;

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
    /// Creates a new memory collection with the provided title.
    /// </summary>
    /// <param name="collectionTitle">The title of the collection to create.</param>
    /// <param name="storeDescription">An optional description of the collection to be created.</param>
    /// <returns>A task representing the operation, returning true if the collection was successfully created, otherwise false.</returns>
    Task<bool> CreateCollectionAsync(string collectionTitle, string storeDescription);

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
}
