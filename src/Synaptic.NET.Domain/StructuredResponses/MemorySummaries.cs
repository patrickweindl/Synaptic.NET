using System.Text.Json.Serialization;
using Synaptic.NET.Domain.Attributes;
using Synaptic.NET.Domain.StructuredResponses;

namespace Synaptic.NET.Domain.Resources;

/// <summary>
/// Structured response for memory summaries containing an array of identifier-summary pairs
/// </summary>
[JsonSchema("memory_summaries", "A list of identifier-summary pairs")]
public sealed class MemorySummaries : IStructuredResponseSchema
{
    [JsonPropertyName("summaries")]
    [JsonPropertyDescription("Array of memory summary items")]
    public required IReadOnlyList<MemorySummary> Summaries { get; init; }
}
