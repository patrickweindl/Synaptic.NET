using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using Synaptic.NET.Domain.Abstractions.Management;
using Synaptic.NET.Domain.Enums;
using Synaptic.NET.Domain.Resources.Storage;

namespace Synaptic.NET.Domain.Resources.Management;

public class User : IComparable<User>, IEquatable<User>, IManagedIdentity
{
    [Key]
    [JsonPropertyName("user_id")]
    public Guid Id { get; set; }

    [JsonPropertyName("user_identifier")]
    [Required]
    public required string Identifier { get; set; }

    [JsonPropertyName("display_name")]
    public string DisplayName { get; set; } = string.Empty;

    [JsonIgnore]
    public string UserName => Identifier.Split("__").FirstOrDefault() ?? Identifier;

    [JsonIgnore]
    public string UserAuthId => Identifier.Split("__").LastOrDefault() ?? Identifier;

    [JsonPropertyName("user_role")]
    public IdentityRole Role { get; set; } = IdentityRole.Guest;

    public ICollection<MemoryStore> Stores { get; set; } = new List<MemoryStore>();

    public ICollection<GroupMembership> Memberships { get; set; } = new List<GroupMembership>();

    public int CompareTo(User? other)
    {
        return other == null
            ? 1
            : Id.CompareTo(other.Id);
    }

    public bool Equals(User? other)
    {
        return other != null && Id.Equals(other.Id);
    }

    public override string ToString() => Identifier;
}
