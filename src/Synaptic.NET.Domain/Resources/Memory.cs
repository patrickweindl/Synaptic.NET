using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;
using Microsoft.Extensions.VectorData;

namespace Synaptic.NET.Domain.Resources;

[Description("Contains data on a specific memory the model persistantly stores.")]
public class Memory
{
    [VectorStoreKey(StorageName = "id")]
    [Key]
    [Description("A unique identifier for a memory entry.")]
    [JsonPropertyName("id")]
    public Guid Identifier { get; set; }

    [VectorStoreData(StorageName = "title", IsFullTextIndexed = true)]
    [Required]
    [Description("A unique title for the memory. Should be a very brief (4-8 words) descriptor.")]
    [JsonPropertyName("title")]
    public required string Title { get; set; }

    [VectorStoreData(StorageName = "description", IsFullTextIndexed = true)]
    [JsonPropertyName("description")]
    [Description("A description of the memory, shorter than the actual content, but provides context about the memory content.")]
    public string Description { get; set; } = string.Empty;

    [VectorStoreData(StorageName = "content", IsFullTextIndexed = true)]
    [Required]
    [Description("The memory's content. Required.")]
    [JsonPropertyName("content")]
    public required string Content { get; set; }

    [VectorStoreData(StorageName = "pinned")]
    [Description("Whether the information has been marked as 'pinned' and is therefore of an importance that justifies always keeping it in context - e.g. personality information, life changing events, changes in the assistant's behavior. Defaults to false.")]
    [JsonPropertyName("pinned")]
    public bool Pinned { get; set; }

    [VectorStoreData(StorageName = "tags")]
    [Description("Tags for the memory for easier referencing. Default to no tags.")]
    [JsonPropertyName("tags")]
    public List<string> Tags { get; set; } = [];

    [VectorStoreData(StorageName = "reference")]
    [Description("An optional reference that can be accessed to retrieve further information, e.g. a URL or a PDF name with page number.")]
    [JsonPropertyName("reference")]
    public string? Reference { get; set; }

    [VectorStoreData(StorageName = "created_at", IsIndexed = false)]
    [Description("The datetime (with timezone) the memory has been created at. Usually set by the backend on creation.")]
    [JsonPropertyName("created_at")]
    public DateTimeOffset CreatedAt { get; set; }

    [VectorStoreData(StorageName = "updated_at", IsIndexed = false)]
    [Description("The datetime (with timezone) the memory has last been updated at. When initializing object, set to unix 0 (1970-01-01T00:00:00Z). Usually set by the backend on updates.")]
    [JsonPropertyName("updated_at")]
    public DateTimeOffset UpdatedAt { get; set; }

    [VectorStoreData(StorageName = "owner")]
    [Description("The owner of the memory. Usually set by the backend on creation based on user claims. Defaults to empty string.")]
    [JsonPropertyName("owner")]
    public required Guid Owner { get; set; }

    [JsonIgnore]
    public TimeSpan Age => DateTime.UtcNow - CreatedAt;

    [NotMapped]
    [VectorStoreVector(1536, DistanceFunction = DistanceFunction.CosineSimilarity, IndexKind = IndexKind.Hnsw, StorageName = "store_description_embedding")]
    public ReadOnlyMemory<float>? StoreDescriptionEmbedding { get; set; }

    [NotMapped]
    [VectorStoreVector(1536, DistanceFunction = DistanceFunction.CosineSimilarity, IndexKind = IndexKind.Hnsw, StorageName = "title_embedding")]
    public ReadOnlyMemory<float>? TitleEmbedding { get; set; }

    [NotMapped]
    [VectorStoreVector(1536, DistanceFunction = DistanceFunction.CosineSimilarity, IndexKind = IndexKind.Hnsw, StorageName = "description_embedding")]
    public ReadOnlyMemory<float>? DescriptionEmbedding { get; set; }

    [NotMapped]
    [VectorStoreVector(1536, DistanceFunction = DistanceFunction.CosineSimilarity, IndexKind = IndexKind.Hnsw, StorageName = "content_embedding")]
    public ReadOnlyMemory<float>? ContentEmbedding { get; set; }

    [ForeignKey(nameof(MemoryStore.StoreId))]
    public Guid StoreId { get; set; }

    [ForeignKey(nameof(MemoryStore))]
    public MemoryStore Store { get; set; } = null!;
}
