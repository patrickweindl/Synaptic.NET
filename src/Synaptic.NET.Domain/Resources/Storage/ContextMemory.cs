using System.ComponentModel;
using System.Text.Json.Serialization;

namespace Synaptic.NET.Domain.Resources.Storage;

[Description("A context memory is a leaner memory entity that is used to pass to a LLM on retrieval from the RAG system to cut token count to essential properties.")]
public class ContextMemory
{
    public ContextMemory(Memory memory)
    {
        Id = memory.Identifier.ToString();
        Title = memory.Title;
        Content = memory.Content;
        ReferenceType = (ReferenceType)memory.ReferenceType;
        Reference = memory.Reference ?? string.Empty;
        CreatedAt = memory.CreatedAt;
        UpdatedAt = memory.UpdatedAt;
        StoreId = memory.StoreId;
    }
    [Description("The unique identifier of the memory.")]
    [JsonPropertyName("id")]
    public string Id { get; set; }

    [Description("The title of the memory.")]
    [JsonPropertyName("title")]
    public string Title { get; set; }

    [Description("The content of the memory.")]
    [JsonPropertyName("content")]
    public string Content { get; set; }

    [Description("The reference type of the memory describing where it originates from.")]
    [JsonPropertyName("reference_type")]
    public ReferenceType ReferenceType { get; set; }

    [Description("The reference of the memory.")]
    [JsonPropertyName("url")]
    public string Reference { get; set; }

    [Description("The date time offset from 1970-01-01T00:00:00Z when the memory was created.")]
    [JsonPropertyName("created_at")]
    public DateTimeOffset CreatedAt { get; set; }

    [Description("The date time offset from 1970-01-01T00:00:00Z when the memory was last updated. If it was never updated then this is the same as the created at date time offset.")]
    [JsonPropertyName("updated_at")]
    public DateTimeOffset UpdatedAt { get; set; }

    [Description("The identifier of the memory store containing this memory.")]
    [JsonPropertyName("store_id")]
    public Guid StoreId { get; set; }
}
