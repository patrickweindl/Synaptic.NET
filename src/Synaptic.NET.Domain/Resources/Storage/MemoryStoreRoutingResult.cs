namespace Synaptic.NET.Domain.Resources.Storage;

/// <summary>
/// Represents the result of a routing operation for a memory store.
/// This class is used to capture the identifier of the routed memory store
/// and the relevance score associated with that routing operation.
/// </summary>
public class MemoryStoreRoutingResult
{
    public MemoryStoreRoutingResult(Guid identifier, double relevance)
    {
        Identifier = identifier;
        Relevance = relevance;
    }

    public Guid Identifier { get; }

    /// <summary>
    /// Gets the relevance score associated with the routing operation.
    /// The relevance indicates the degree of suitability or importance
    /// of the routed memory store in the context of the operation.
    /// </summary>
    public double Relevance { get; }
}
