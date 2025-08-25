using System.Text.Json.Serialization;
using Synaptic.NET.OpenAI.Attributes;

namespace Synaptic.NET.OpenAI.Resources;

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
