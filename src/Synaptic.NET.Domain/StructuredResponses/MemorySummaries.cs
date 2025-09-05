using System.Text.Json.Serialization;
using Synaptic.NET.Domain.Abstractions.Augmentation;
using Synaptic.NET.Domain.Attributes;

namespace Synaptic.NET.Domain.StructuredResponses;

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
