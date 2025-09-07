using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;
using Microsoft.Extensions.VectorData;
using Synaptic.NET.Domain.Resources.Management;

namespace Synaptic.NET.Domain.Resources.Storage;

[Description("Contains data on a specific memory the model persistantly stores.")]
public class Memory
{
    [VectorStoreKey(StorageName = "id")]
    [Key]
    [Description("A unique identifier for a memory entry.")]
    [JsonPropertyName("id")]
    public Guid Identifier { get; set; } = Guid.NewGuid();

    [VectorStoreData(StorageName = "title", IsFullTextIndexed = true)]
    [Required]
    [Description("A unique title for the memory. Should be a very brief (4-8 words) descriptor. Has a maximum length of 256 characters. Required.")]
    [JsonPropertyName("title")]
    [MaxLength(256)]
    public required string Title { get; set; }

    [VectorStoreData(StorageName = "description", IsFullTextIndexed = true)]
    [JsonPropertyName("description")]
    [Description("A description of the memory, shorter than the actual content, but provides context about the memory content. Has a maximum length of 512 characters. Required as this property is vector indexed.")]
    [MaxLength(512)]
    [Required]
    public required string Description { get; set; } = string.Empty;

    [VectorStoreData(StorageName = "content", IsFullTextIndexed = true)]
    [Required]
    [Description("The memory's content. Can not exceed 4096 characters. Required.")]
    [JsonPropertyName("content")]
    [MaxLength(4096)]
    public required string Content { get; set; }

    [VectorStoreData(StorageName = "pinned", IsIndexed = true)]
    [Description("Whether the information has been marked as 'pinned' and is therefore of an importance that justifies always keeping it in context - e.g. personality information, life changing events, changes in the assistant's behavior. Defaults to false.")]
    [JsonPropertyName("pinned")]
    public bool Pinned { get; set; }

    [VectorStoreData(StorageName = "tags", IsIndexed = true)]
    [Description("Tags for the memory for easier referencing. Defaults to no tags.")]
    [JsonPropertyName("tags")]
    public List<string> Tags { get; set; } = [];

    [VectorStoreData(StorageName = "reference_type")]
    [Description("The type of the reference, e.g. conversation, document, memory, codebase, homepage or none. Defaults to none.")]
    [JsonPropertyName("reference_type")]
    [Range(0, 5)]
    public int ReferenceType { get; set; }

    [NotMapped]
    [JsonIgnore]
    public ReferenceType RefType => (ReferenceType)ReferenceType;

    [VectorStoreData(StorageName = "reference")]
    [Description("An optional reference that can be accessed to retrieve further information, e.g. a URL or a PDF name with page number. Has a maximum length of 2048 characters.")]
    [JsonPropertyName("reference")]
    [MaxLength(2048)]
    public string? Reference { get; set; }

    [VectorStoreData(StorageName = "created_at", IsIndexed = true)]
    [Description("The datetime (with timezone) the memory has been created at. Usually set by the backend on creation.")]
    [JsonPropertyName("created_at")]
    [Required]
    public required DateTimeOffset CreatedAt { get; set; }

    [VectorStoreData(StorageName = "updated_at", IsIndexed = true)]
    [Description(
        "The datetime (with timezone) the memory has last been updated at. When initializing object, set to unix 0 (1970-01-01T00:00:00Z). Usually set by the backend on updates.")]
    [JsonPropertyName("updated_at")]
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UnixEpoch;

    [Description(
        "The owner of the memory. Usually set by the backend on creation based on user claims. Defaults to empty string.")]
    [JsonPropertyName("owner")]
    public required Guid Owner { get; set; }

    [ForeignKey(nameof(Owner))]
    public User OwnerUser { get; set; } = null!;

    public Guid? GroupId { get; set; }

    [ForeignKey(nameof(GroupId))]
    public Group? OwnerGroup { get; set; }

    [NotMapped]
    [JsonIgnore]
    public TimeSpan Age => DateTime.UtcNow - CreatedAt;

    public Guid StoreId { get; set; }

    [ForeignKey(nameof(StoreId))]
    public MemoryStore Store { get; set; } = null!;
}
