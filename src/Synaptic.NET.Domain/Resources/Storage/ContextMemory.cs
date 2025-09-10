using System.ComponentModel;

namespace Synaptic.NET.Domain.Resources.Storage;

[Description("A context memory is a leaner memory entity that is used to pass to a LLM on retrieval from the RAG system to cut token count to essential properties.")]
public class ContextMemory
{
    public ContextMemory(Memory memory)
    {
        Title = memory.Title;
        Content = memory.Content;
        ReferenceType = (ReferenceType)memory.ReferenceType;
        Reference = memory.Reference ?? string.Empty;
        CreatedAt = memory.CreatedAt;
        UpdatedAt = memory.UpdatedAt;
        StoreId = memory.StoreId;
    }

    [Description("The title of the memory.")]
    public string Title { get; set; }

    [Description("The content of the memory.")]
    public string Content { get; set; }

    [Description("The reference type of the memory describing where it originates from.")]
    public ReferenceType ReferenceType { get; set; }

    [Description("The reference of the memory.")]
    public string Reference { get; set; }

    [Description("The date time offset from 1970-01-01T00:00:00Z when the memory was created.")]
    public DateTimeOffset CreatedAt { get; set; }

    [Description("The date time offset from 1970-01-01T00:00:00Z when the memory was last updated. If it was never updated then this is the same as the created at date time offset.")]
    public DateTimeOffset UpdatedAt { get; set; }

    [Description("The identifier of the memory store containing this memory.")]
    public Guid StoreId { get; set; }
}
