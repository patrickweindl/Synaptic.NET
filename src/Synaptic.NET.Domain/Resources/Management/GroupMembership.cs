using System.ComponentModel.DataAnnotations.Schema;

namespace Synaptic.NET.Domain.Resources.Management;

public class GroupMembership
{
    public Guid UserId { get; set; }

    [ForeignKey(nameof(UserId))]
    public User User { get; set; } = null!;

    public Guid GroupId { get; set; }

    [ForeignKey(nameof(GroupId))]
    public Group Group { get; set; } = null!;
}
