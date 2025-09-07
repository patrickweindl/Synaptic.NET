using Synaptic.NET.Domain.Resources.Storage;

namespace Synaptic.NET.Domain.Abstractions.Augmentation;

/// <summary>
/// Defines a contract for services that provide memory augmentation functionalities,
/// including the generation of descriptive summaries for individual memory content
/// and groups of memories associated with a store.
/// </summary>
public interface IMemoryAugmentationService
{
    /// <summary>
    /// Asynchronously generates a descriptive summary for the provided memory content.
    /// </summary>
    /// <param name="memoryContent">The content of the memory for which a description needs to be generated.</param>
    /// <returns>Returns a task that represents the asynchronous operation. The task result contains the generated memory description as a string.</returns>
    Task<string> GenerateMemoryDescriptionAsync(string memoryContent);

    /// <summary>
    /// Asynchronously generates a descriptive summary for a list of memories associated with the specified store identifier.
    /// </summary>
    /// <param name="storeIdentifier">The unique identifier of the store for which the description is being generated.</param>
    /// <param name="memories">A list of memory contents associated with the store, providing contextual information for description generation.</param>
    /// <returns>Returns a task that represents the asynchronous operation. The task result contains the generated store description as a string.</returns>
    Task<string> GenerateStoreDescriptionAsync(string storeIdentifier, List<string> memories);


    /// <summary>
    /// Asynchronously generates a descriptive summary for a list of memories associated with the specified store identifier.
    /// </summary>
    /// <param name="storeIdentifier">The unique identifier of the store for which the description is being generated.</param>
    /// <param name="memories">A list of memories associated with the store, providing contextual information for description generation.</param>
    /// <returns>Returns a task that represents the asynchronous operation. The task result contains the generated store description as a string.</returns>
    Task<string> GenerateStoreDescriptionAsync(string storeIdentifier, List<Memory> memories);

    /// <summary>
    /// Asynchronously generates a title for the store based on its description
    /// and a collection of associated memories.
    /// </summary>
    /// <param name="storeDescription">The descriptive summary of the store for which a title needs to be generated.</param>
    /// <param name="memories">A collection of memories associated with the store to aid in title generation.</param>
    /// <returns>Returns a task that represents the asynchronous operation. The task result contains the generated store title as a string.</returns>
    Task<string> GenerateStoreTitleAsync(string storeDescription, List<Memory> memories);

    Task<IEnumerable<(Guid, double)>> RankMemoriesAsync(string query, MemoryStore memoryStore, CancellationToken cancellationToken = default);
}
