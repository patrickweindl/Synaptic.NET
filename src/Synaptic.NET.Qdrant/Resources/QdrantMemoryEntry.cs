using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using Microsoft.Extensions.VectorData;

namespace Synaptic.NET.Qdrant.Resources;

public class QdrantMemoryEntry
{
    [VectorStoreKey(StorageName = "id")]
    [Key]
    [JsonPropertyName("id")]
    public Guid Identifier { get; set; }

    [JsonPropertyName("store_id")]
    public Guid StoreIdentifier { get; set; }

    [VectorStoreData(StorageName = "store_title", IsFullTextIndexed = true)]
    [JsonPropertyName("store_title")]
    public string StoreTitle { get; set; } = string.Empty;

    [VectorStoreData(StorageName = "store_description", IsFullTextIndexed = true)]
    [JsonPropertyName("store_description")]
    public string StoreDescription { get; set; } = string.Empty;

    [VectorStoreData(StorageName = "owner")]
    [JsonPropertyName("owner")]
    public string Owner { get; set; } = string.Empty;

    [VectorStoreData(StorageName = "pinned")]
    [JsonPropertyName("pinned")]
    public bool Pinned { get; set; }

    [VectorStoreData(StorageName = "title", IsFullTextIndexed = true)]
    public string Title { get; set; } = string.Empty;

    [VectorStoreData(StorageName = "content", IsFullTextIndexed = true)]
    [JsonPropertyName("content")]
    public string Content { get; set; } = string.Empty;

    [VectorStoreData(StorageName = "description", IsFullTextIndexed = true)]
    [JsonPropertyName("description")]
    public string Description { get; set; } = string.Empty;

    [VectorStoreData(StorageName = "tags")]
    [JsonPropertyName("tags")]
    public List<string> Tags { get; set; } = [];

    [VectorStoreData(StorageName = "created_at", IsIndexed = false)]
    [JsonPropertyName("created_at")]
    public DateTimeOffset CreatedAt { get; set; }

    [VectorStoreData(StorageName = "updated_at", IsIndexed = false)]
    [JsonPropertyName("updated_at")]
    public DateTimeOffset UpdatedAt { get; set; }

    [JsonIgnore]
    public TimeSpan Age => DateTime.UtcNow - (CreatedAt > UpdatedAt ? CreatedAt : UpdatedAt);

    [VectorStoreVector(1536, DistanceFunction = DistanceFunction.CosineSimilarity, IndexKind = IndexKind.Hnsw, StorageName = "title_embedding")]
    public ReadOnlyMemory<float>? TitleEmbedding { get; set; }

    [VectorStoreVector(1536, DistanceFunction = DistanceFunction.CosineSimilarity, IndexKind = IndexKind.Hnsw, StorageName = "description_embedding")]
    public ReadOnlyMemory<float>? DescriptionEmbedding { get; set; }

    [VectorStoreVector(1536, DistanceFunction = DistanceFunction.CosineSimilarity, IndexKind = IndexKind.Hnsw, StorageName = "content_embedding")]
    public ReadOnlyMemory<float>? ContentEmbedding { get; set; }
}
