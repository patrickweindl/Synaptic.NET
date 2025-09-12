using Synaptic.NET.Domain.Enums;
using Synaptic.NET.Domain.Resources.Management;

namespace Synaptic.NET.Domain.Abstractions.Management;

public interface IUserManager
{
    Task<List<User>> GetUsersAsync();

    Task<List<Group>> GetGroupsAsync();

    Task SetUserRoleAsync(User currentUser, User targetUser, IdentityRole targetRole);

    Task CreateGroupAsync(User currentUser, string groupName);

    Task AddUserToGroupAsync(User currentUser, User targetUser, string readableGroupName);

    Task AddUserToGroupAsync(User currentUser, User targetUser, Guid group);

    Task AddUserToGroupAsync(User currentUser, User targetUser, Group group);

    Task RemoveUserFromGroupAsync(User currentUser, User targetUser, string group);

    Task RemoveUserFromGroupAsync(User currentUser, User targetUser, Guid group);

    Task RemoveUserFromGroupAsync(User currentUser, User targetUser, Group group);

    Task<string> ReadableGroupNameToUserGroupIdentifierAsync(string groupName);

    Task<string> GroupIdentifierToReadableNameAsync(string groupIdentifier);

    Task<string> GroupIdentifierToReadableNameAsync(Guid groupIdentifier);
}
