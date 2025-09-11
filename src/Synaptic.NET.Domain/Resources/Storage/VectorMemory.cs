using System.ComponentModel;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.Extensions.VectorData;
using OpenAI.Embeddings;

namespace Synaptic.NET.Domain.Resources.Storage;

[Description("An extension of the memory class that changes property types that can otherwise not be stored in common vector databases.")]
public class VectorMemory : Memory
{
    public static async Task<VectorMemory> FromMemory(Memory memory, EmbeddingClient embeddingGenerator, CancellationToken cancellationToken = default)
    {
        var contentEmbeddingResult = await embeddingGenerator.GenerateEmbeddingAsync(memory.Content, cancellationToken: cancellationToken);
        var descriptionEmbeddingResult =
            await embeddingGenerator.GenerateEmbeddingAsync(memory.Description, cancellationToken: cancellationToken);
        var titleEmbedding = await embeddingGenerator.GenerateEmbeddingAsync(memory.Title, cancellationToken: cancellationToken);

        return new VectorMemory
        {
            Identifier = memory.Identifier,
            StoreId = memory.StoreId,
            Title = memory.Title,
            Content = memory.Content,
            Description = memory.Description,
            CreatedAt = memory.CreatedAt,
            UpdatedAt = memory.UpdatedAt,
            Pinned = memory.Pinned,
            Tags = memory.Tags,
            ReferenceType = memory.ReferenceType,
            Reference = memory.Reference,
            Owner = memory.Owner,
            OwnerUser = memory.OwnerUser,
            VectorStoreIdentifier = memory.StoreId.ToString(),
            VectorOwnerIdentifier = memory.GroupId != null ? memory.GroupId.Value.ToString() : memory.Owner.ToString(),
            TitleEmbedding = titleEmbedding.Value.ToFloats(),
            DescriptionEmbedding = descriptionEmbeddingResult.Value.ToFloats(),
            ContentEmbedding = contentEmbeddingResult.Value.ToFloats()
        };
    }

    private string _storeIdString = string.Empty;

    [VectorStoreData(StorageName = "store_id", IsIndexed = true)]
    [Description("The identifier of the store the memory belongs to as a string value. Must be parseable to a GUID.")]
    [NotMapped]
    public required string VectorStoreIdentifier
    {
        get
        {
            if (string.IsNullOrEmpty(_storeIdString))
            {
                _storeIdString = StoreId.ToString();
            }
            return _storeIdString;
        }
        set
        {
            if (Guid.TryParse(value, out var guid))
            {
                StoreId = guid;
                _storeIdString = value;
            }
            else
            {
                throw new FormatException("The provided StoreId string is not a valid GUID.");
            }
        }
    }

    private string _ownerGuidString = string.Empty;

    [NotMapped]
    [VectorStoreData(StorageName = "owner_id", IsIndexed = true)]
    [Description("The owner of the memory, usually set by the backend on creation. This string value must be parseable to a GUID as it is a backing property for storage to vector databases.")]
    public string VectorOwnerIdentifier
    {
        get
        {
            if (string.IsNullOrEmpty(_ownerGuidString))
            {
                _ownerGuidString = Owner.ToString();
            }
            return _ownerGuidString;
        }
        set
        {
            if (Guid.TryParse(value, out var guid))
            {
                Owner = guid;
                _ownerGuidString = value;
            }
            else
            {
                throw new FormatException("The provided StoreId string is not a valid GUID.");
            }
        }
    }

    [NotMapped]
    [VectorStoreVector(3072, DistanceFunction = DistanceFunction.CosineSimilarity, IndexKind = IndexKind.Hnsw, StorageName = "title_embedding")]
    public required ReadOnlyMemory<float> TitleEmbedding { get; init; }

    [NotMapped]
    [VectorStoreVector(3072, DistanceFunction = DistanceFunction.CosineSimilarity, IndexKind = IndexKind.Hnsw, StorageName = "description_embedding")]
    public required ReadOnlyMemory<float> DescriptionEmbedding { get; init; }

    [NotMapped]
    [VectorStoreVector(3072, DistanceFunction = DistanceFunction.CosineSimilarity, IndexKind = IndexKind.Hnsw, StorageName = "content_embedding")]
    public required ReadOnlyMemory<float> ContentEmbedding { get; init; }
}
