using System.Security.Claims;
using Synaptic.NET.Domain.Enums;
using Synaptic.NET.Domain.Helpers;
using Synaptic.NET.Domain.Resources;

namespace Synaptic.NET.Core;

public interface IUserManager
{
    User GetCurrentUser(ICurrentUserService currentUserService);

    User GetOrCreateUser(ClaimsIdentity user, UserRole role = UserRole.User)
    {
        return GetOrCreateUser(user.ToUserIdentifier(), role);
    }

    User GetOrCreateUser(string userIdentifier, UserRole role = UserRole.User);

    bool GetOrCreateUser(string userIdentifier, out User user, UserRole role = UserRole.User);

    bool GetOrCreateUser(ClaimsIdentity userIdentity, out User user, UserRole role = UserRole.User)
    {
        return GetOrCreateUser(userIdentity.ToUserIdentifier(), out user, role);
    }

    List<User> GetUsers();

    List<User> GetGroups();

    void SetUserRole(User currentUser, User targetUser, UserRole targetRole);

    void CreateGroup(User currentUser, string groupName);

    void AddUserToGroup(User currentUser, User targetUser, string group);

    void RemoveUserFromGroup(User currentUser, User targetUser, string group);

    string ReadableGroupNameToUserGroupIdentifier(string groupName);

    string GroupIdentifierToReadableName(string groupIdentifier);
}
