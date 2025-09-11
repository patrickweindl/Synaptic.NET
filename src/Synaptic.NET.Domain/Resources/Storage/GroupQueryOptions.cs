using Synaptic.NET.Domain.Resources.Management;

namespace Synaptic.NET.Domain.Resources.Storage;

public class GroupQueryOptions
{
    /// <summary>
    /// Initializes a new instance of the <see cref="GroupQueryOptions"/> class.
    /// This is equal to the default settings, which searches in all groups.
    /// </summary>
    public GroupQueryOptions()
    {

    }

    public bool GroupShouldBeIncluded(Group group)
    {
        if (GroupSearchMode == GroupSearchMode.All)
        {
            return true;
        }
        return GroupIds.Any(g => g == group.Id) || GroupNames.Any(n => n == group.DisplayName);
    }

    public static GroupQueryOptions ById(Guid id) => new GroupQueryOptions()
    {
        GroupSearchMode = GroupSearchMode.SingleById, GroupIds = new List<Guid> { id }
    };

    public static GroupQueryOptions ByName(string name) => new GroupQueryOptions()
    {
        GroupSearchMode = GroupSearchMode.SingleByName, GroupNames = new List<string> { name }
    };

    public static GroupQueryOptions ByIds(List<Guid> ids) => new GroupQueryOptions()
    {
        GroupSearchMode = GroupSearchMode.ManyById, GroupIds = ids
    };

    public static GroupQueryOptions ByNames(List<string> names) => new GroupQueryOptions()
    {
        GroupSearchMode = GroupSearchMode.ManyByName, GroupNames = names
    };

    public static GroupQueryOptions None => new GroupQueryOptions() { GroupSearchMode = GroupSearchMode.None };

    public static readonly GroupQueryOptions Default = new GroupQueryOptions() { GroupSearchMode = GroupSearchMode.All };
    public GroupSearchMode GroupSearchMode { get; init; } = GroupSearchMode.All;
    public List<Guid> GroupIds { get; init; } = new();
    public List<string> GroupNames { get; init; } = new();
}
