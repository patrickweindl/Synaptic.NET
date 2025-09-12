using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Synaptic.NET.Domain.Resources.Management;

public class SymLinkUser
{
    [Key]
    public required Guid Id { get; set; }

    public required Guid UserId { get; set; }

    [ForeignKey(nameof(UserId))]
    public User User { get; set; } = null!;

    public required Guid SymLinkUserId { get; set; }
}
