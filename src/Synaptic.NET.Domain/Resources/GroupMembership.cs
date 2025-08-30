namespace Synaptic.NET.Domain.Resources;

public class GroupMembership
{
    public Guid UserId { get; set; }
    public User User { get; set; } = null!;

    public Guid GroupId { get; set; }
    public Group Group { get; set; } = null!;
}
