using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json;

namespace Synaptic.NET.Domain.Resources.Management;

public class DynamicRegistration
{
    [Key]
    public required Guid Id { get; set; }

    [MaxLength(16384)]
    public string OriginalBodyString { get; set; } = string.Empty;

    [NotMapped]
    public JsonElement? OriginalBody
    {
        get;
        init
        {
            field = value;
            OriginalBodyString = field.HasValue ? JsonSerializer.Serialize(field.Value) : string.Empty;
        }
    }

    public DateTimeOffset ExpiresAt { get; set; }

    [MaxLength(16384)]
    public required string Secret { get; set; }

    public required Guid RegistrationId { get; set; }
}
