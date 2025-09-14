using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Synaptic.NET.Domain.Resources.Storage;

public class IngestionReference
{
    /// <summary>
    /// A unique identifier for the reference.
    /// </summary>
    [Key]
    [JsonPropertyName("id")]
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>
    /// The original text of the chunk that was ingested. In case of PDF files usually the base64 encoded string of the ingested portion of the document of which memories were created.
    /// </summary>
    [JsonPropertyName("original_reference_text")]
    public required string OriginalText { get; set; }

    /// <summary>
    /// The name of the document, file or other source the ingestion was performed on.
    /// </summary>
    [MaxLength(4096)]
    [JsonPropertyName("reference_name")]
    public required string ReferenceName { get; set; }

    /// <summary>
    /// If the ingestion was performed on a paginated document (e.g. PDF), this indicates the starting page of the chunk.
    /// </summary>
    [JsonPropertyName("start_page")]
    public int StartPage { get; set; }

    /// <summary>
    /// If the ingestion was performed on a paginated document (e.g. PDF), this indicates the ending page of the chunk.
    /// </summary>
    [JsonPropertyName("end_page")]
    public int EndPage { get; set; }
}
