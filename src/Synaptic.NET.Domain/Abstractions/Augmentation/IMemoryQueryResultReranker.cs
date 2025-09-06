using Microsoft.SemanticKernel.Memory;
using Synaptic.NET.Domain.Resources.Storage;

namespace Synaptic.NET.Domain.Abstractions.Augmentation;

/// <summary>
/// Defines a mechanism for reranking memory query results based on custom logic.
/// </summary>
public interface IMemoryQueryResultReranker
{
    /// <summary>
    /// Re-ranks the given memory query results based on custom logic.
    /// </summary>
    /// <param name="results">A read-only list of memory query results to be re-ranked.</param>
    /// <returns>An enumerable collection of re-ranked memory query results.</returns>
    Task<IEnumerable<MemorySearchResult>> Rerank(IReadOnlyList<MemorySearchResult> results);

    /// <summary>
    /// Re-ranks the given memory query results based on custom logic.
    /// </summary>
    /// <param name="results">An enumerable collection of memory query results to be re-ranked.</param>
    /// <returns>An enumerable collection of re-ranked memory query results.</returns>
    Task<IEnumerable<MemorySearchResult>> Rerank(IEnumerable<MemorySearchResult> results);
}
