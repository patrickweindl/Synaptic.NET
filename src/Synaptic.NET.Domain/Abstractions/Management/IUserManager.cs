using Synaptic.NET.Domain.Enums;
using Synaptic.NET.Domain.Resources.Management;

namespace Synaptic.NET.Domain.Abstractions.Management;

public interface IUserManager
{
    List<User> GetUsers();

    List<Group> GetGroups();

    void SetUserRole(User currentUser, User targetUser, UserRole targetRole);

    void CreateGroup(User currentUser, string groupName);

    void AddUserToGroup(User currentUser, User targetUser, string readableGroupName);

    void AddUserToGroup(User currentUser, User targetUser, Guid group);

    void AddUserToGroup(User currentUser, User targetUser, Group group);

    void RemoveUserFromGroup(User currentUser, User targetUser, string group);

    void RemoveUserFromGroup(User currentUser, User targetUser, Guid group);

    void RemoveUserFromGroup(User currentUser, User targetUser, Group group);

    string ReadableGroupNameToUserGroupIdentifier(string groupName);

    string GroupIdentifierToReadableName(string groupIdentifier);

    string GroupIdentifierToReadableName(Guid groupIdentifier);
}
