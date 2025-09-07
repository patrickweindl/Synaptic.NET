using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace Synaptic.NET.Domain.Resources.Management;

public class ApiKey
{
    [Key]
    public required Guid Id { get; set; }

    [ForeignKey(nameof(User))]
    public required User Owner { get; set; }

    [JsonPropertyName("api_key_name")]
    [MaxLength(512)]
    public required string Name { get; set; }

    [JsonPropertyName("key")]
    [MaxLength(1024)]
    public required string Key { get; set; }

    [JsonPropertyName("expires_at")]
    public DateTimeOffset ExpiresAt { get; set; }
}
