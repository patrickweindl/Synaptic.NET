using Synaptic.NET.Domain.Resources.Storage;

namespace Synaptic.NET.Domain.Abstractions.Storage;

/// <summary>
/// Defines a contract for routing or ranking memory stores based on certain criteria
/// such as a query or a specific memory. This interface is designed to support selecting
/// the most appropriate memory store(s) from a list of available memory stores.
/// </summary>
public interface IMemoryStoreRouter
{
    /// <summary>
    /// Evaluates a given query against a collection of available memory stores and ranks them
    /// based on relevance or other predefined criteria.
    /// </summary>
    /// <param name="query">The query string used to evaluate and rank the memory stores.</param>
    /// <param name="availableStores">A collection of available memory stores to be ranked.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains a list of
    /// <see cref="MemoryStoreRoutingResult"/> objects, each containing an identifier and a relevance score
    /// for the ranked memory stores.</returns>
    Task<IEnumerable<MemoryStoreRoutingResult>> RankStoresAsync(string query, IEnumerable<MemoryStore> availableStores);

    /// <summary>
    /// Routes a given memory to the most appropriate memory store from a provided collection of available memory stores,
    /// based on relevance or other predefined routing criteria.
    /// </summary>
    /// <param name="memory">The memory instance to be routed to an appropriate memory store.</param>
    /// <param name="availableStores">A collection of available memory stores to evaluate for routing the memory.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains a
    /// <see cref="MemoryStoreRoutingResult"/> object, indicating the selected memory store and its relevance score.</returns>
    Task<MemoryStoreRoutingResult> RouteMemoryToStoreAsync(Memory memory, IEnumerable<MemoryStore> availableStores);
}
