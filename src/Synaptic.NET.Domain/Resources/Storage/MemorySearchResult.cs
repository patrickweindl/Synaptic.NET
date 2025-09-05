namespace Synaptic.NET.Domain.Resources.Storage;

public class MemorySearchResult
{
    public required double Relevance { get; set; }
    public required Memory Memory { get; set; }
}
