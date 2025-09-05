using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using Synaptic.NET.Domain.Abstractions.Management;
using Synaptic.NET.Domain.Resources.Storage;

namespace Synaptic.NET.Domain.Resources.Management;

public class Group : IManagedIdentity
{
    [Key]
    [JsonPropertyName("group_id")]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    [JsonPropertyName("group_identifier")]
    public required string Identifier { get; set; }

    [JsonPropertyName("group_name")]
    public string DisplayName { get; set; } = string.Empty;

    public ICollection<MemoryStore> Stores { get; set; } = new List<MemoryStore>();

    public ICollection<GroupMembership> Memberships { get; set; } = new List<GroupMembership>();
}
