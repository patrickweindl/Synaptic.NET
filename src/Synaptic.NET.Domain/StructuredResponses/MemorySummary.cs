using System.Text.Json.Serialization;
using Synaptic.NET.Domain.Attributes;

namespace Synaptic.NET.Domain.StructuredResponses;

/// <summary>
/// Represents a memory summary item with an identifier and summary
/// </summary>
[JsonSchema("memory_summary", "A memory summary item containing an identifier and a summary of the memory content.")]
public sealed class MemorySummary : IStructuredResponseSchema
{
    [JsonPropertyName("identifier")]
    [JsonPropertyDescription("A unique identifier string")]
    public required string Identifier { get; init; }

    [JsonPropertyName("summary")]
    [JsonPropertyDescription("A summary associated with the identifier, created from a chunk")]
    public required string Summary { get; init; }
}
