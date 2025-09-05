using Microsoft.SemanticKernel.Memory;

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
    IEnumerable<MemoryQueryResult> Rerank(IReadOnlyList<MemoryQueryResult> results);
}
