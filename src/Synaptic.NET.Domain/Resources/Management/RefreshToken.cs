using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace Synaptic.NET.Domain.Resources.Management;

public class RefreshToken
{
    [Key]
    public required Guid Id { get; set; }

    [JsonPropertyName("token")]
    [MaxLength(16384)]
    public required string Token { get; set; } = string.Empty;

    [JsonPropertyName("user_id")]
    public required string UserId { get; set; } = string.Empty;

    [JsonPropertyName("user_name")]
    public required string UserName { get; set; } = string.Empty;

    [JsonPropertyName("expires_at")]
    public DateTimeOffset ExpiresAt { get; set; }

    [NotMapped]
    public bool IsExpired => ExpiresAt < DateTimeOffset.UtcNow;

    [JsonPropertyName("is_consumed")]
    public required bool IsConsumed { get; set; }
}
