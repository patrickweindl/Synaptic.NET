using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Synaptic.NET.Domain.Resources;

[Description("A memory store is a collection of memories that are grouped together by a common topic. It allows for better organization and retrieval of memories based on their context or subject matter.")]
public class MemoryStore
{
    [Key]
    [JsonPropertyName("store_id")]
    public Guid StoreId { get; set; } = Guid.NewGuid();

    [Required]
    [JsonPropertyName("title")]
    [Description("A unique title for the memory store. Should be a very brief (4-8 words) descriptor.")]
    public required string Title { get; set; } = string.Empty;

    [JsonPropertyName("description")]
    [Description("The description of the memory store, which describes a common topic or context all memories within the store share.")]
    public string Description { get; set; } = string.Empty;

    [JsonPropertyName("tags")]
    [Description("A set of tags that can be used to categorize the store.")]
    public List<string> Tags { get; set; } = new();

    public Guid? UserId { get; set; }
    public User? OwnerUser { get; set; }

    public Guid? GroupId { get; set; }
    public Group? OwnerGroup { get; set; }

    [Description("A list of memories contained within the store")]
    [JsonPropertyName("memories")]
    public ICollection<Memory> Memories { get; set; } = new List<Memory>();
}
