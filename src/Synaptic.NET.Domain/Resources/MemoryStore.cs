using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Synaptic.NET.Domain.Resources;

[Description("A memory store is a collection of memories that are grouped together by a common topic. It allows for better organization and retrieval of memories based on their context or subject matter.")]
public class MemoryStore
{
    [Required]
    [Description("A unique identifier for the memory store, ideally compatible to a linux file name (e.g. 'personality', 'various-thoughts', 'memories-from-2025-06-29'")]
    [JsonPropertyName("identifier")]
    public required string Identifier { get; set; }

    [Description("The description of the memory store, which describes a common topic or context all memories within the store share.")]
    [JsonPropertyName("description")]
    public string Description { get; set; } = string.Empty;

    [Description("A list of memories contained within the store")]
    [JsonPropertyName("memories")]
    public List<Memory> Memories { get; set; } = [];
}
