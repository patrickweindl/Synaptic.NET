using Synaptic.NET.Domain;
using Synaptic.NET.Domain.Enums;
using Synaptic.NET.Domain.Resources;

namespace Synaptic.NET.Core.Services;

public class UserManager : IUserManager
{
    private readonly SynapticServerSettings _settings;
    private readonly IEncryptionService _encryptionService;
    private readonly SynapticDbContext _dbContext;
    private readonly ICurrentUserService _currentUserService;
    public UserManager(ICurrentUserService currentUserService, SynapticDbContext dbContext, IEncryptionService encryptionService, SynapticServerSettings settings)
    {
        _dbContext = dbContext;
        _currentUserService = currentUserService;
        _settings = settings;
        _encryptionService = encryptionService;
    }

    public List<User> GetUsers()
    {
        if (_currentUserService.GetCurrentUser().Role >= UserRole.Admin)
        {
            return _dbContext.Users.ToList();
        }
        return [];
    }

    public List<Group> GetGroups()
    {
        if (_currentUserService.GetCurrentUser().Role >= UserRole.Admin)
        {
            return _dbContext.Groups.ToList();
        }
        return [];
    }

    public void SetUserRole(User currentUser, User targetUser, UserRole targetRole)
    {
        if (currentUser.Role == UserRole.Admin)
        {
            var dbTargetUser = _dbContext.Users.FirstOrDefault(u => u.Id == targetUser.Id);
            if (dbTargetUser == null)
            {
                return;
            }
            dbTargetUser.Role = targetRole;
            _dbContext.Users.Update(dbTargetUser);
            _dbContext.SaveChanges();
        }
    }

    public void CreateGroup(User currentUser, string groupName)
    {
        if (currentUser.Role < UserRole.Admin)
        {
            return;
        }

        string groupUserName = $"group-{groupName}__{Guid.NewGuid():N}";
        Group newGroup = new Group() { Identifier = groupUserName, DisplayName = groupName };
        if (_dbContext.Groups.Any(u => u.DisplayName == groupUserName))
        {
            return;
        }
        _dbContext.Groups.Add(newGroup);
        _dbContext.SaveChanges();
    }

    public void RemoveUserFromGroup(User currentUser, User targetUser, Group group) => throw new NotImplementedException();

    public string ReadableGroupNameToUserGroupIdentifier(string groupName)
    {
        if (GetGroups().FirstOrDefault(u => u.Identifier.StartsWith($"group-{groupName}__")) is { } existingGroup)
        {
            return existingGroup.Identifier;
        }
        return $"group-{groupName}__{Guid.NewGuid():N}";
    }

    public string GroupIdentifierToReadableName(string groupIdentifier)
    {
        return groupIdentifier.Split("__").First().Replace("group-", "");
    }

    public string GroupIdentifierToReadableName(Guid groupIdentifier)
    {
        if (_dbContext.Groups.FirstOrDefault(g => g.Id == groupIdentifier) is { } group)
        {
            return group.DisplayName;
        }

        return string.Empty;
    }

    public void AddUserToGroup(User currentUser, User targetUser, string readableGroupName)
    {
        string groupUserName = ReadableGroupNameToUserGroupIdentifier(readableGroupName);
        if (GetGroups().FirstOrDefault(u => u.DisplayName == groupUserName) is not { } existingGroup)
        {
            if (currentUser.Role == UserRole.Admin)
            {
                CreateGroup(currentUser, readableGroupName);
            }
            else
            {
                return;
            }
        }

        if (GetGroups().FirstOrDefault(u => u.DisplayName == groupUserName) is not { } createdGroup)
        {
            return;
        }

        if (currentUser.Role == UserRole.Admin)
        {
            createdGroup.Memberships.Add(new GroupMembership() { UserId = targetUser.Id });
            _dbContext.Groups.Update(createdGroup);
            _dbContext.SaveChanges();
        }
    }

    public void AddUserToGroup(User currentUser, User targetUser, Guid group)
    {
        if (GetGroups().FirstOrDefault(u => u.Id == group) is not { } existingGroup)
        {
            return;
        }

        if (currentUser.Role == UserRole.Admin)
        {
            existingGroup.Memberships.Add(new GroupMembership() { UserId = targetUser.Id });
            _dbContext.Groups.Update(existingGroup);
            _dbContext.SaveChanges();
        }
    }

    public void AddUserToGroup(User currentUser, User targetUser, Group group)
    {
        if (GetGroups().FirstOrDefault(u => u.Id == group.Id) is not { } existingGroup)
        {
            if (currentUser.Role == UserRole.Admin)
            {
                CreateGroup(currentUser, group.DisplayName);
            }
            else
            {
                return;
            }
        }

        if (GetGroups().FirstOrDefault(u => u.DisplayName == group.DisplayName) is not { } createdGroup)
        {
            return;
        }

        if (currentUser.Role == UserRole.Admin)
        {
            createdGroup.Memberships.Add(new GroupMembership() { UserId = targetUser.Id });
            _dbContext.Groups.Update(createdGroup);
            _dbContext.SaveChanges();
        }
    }

    public void RemoveUserFromGroup(User currentUser, User targetUser, string readableGroupName)
    {
        string groupUserName = ReadableGroupNameToUserGroupIdentifier(readableGroupName);
        if (GetGroups().FirstOrDefault(u => u.DisplayName == groupUserName) is not { } existingGroup)
        {
            if (currentUser.Role == UserRole.Admin)
            {
                CreateGroup(currentUser, readableGroupName);
            }
            else
            {
                return;
            }
        }

        if (GetGroups().FirstOrDefault(u => u.DisplayName == groupUserName) is not { } createdGroup)
        {
            return;
        }

        if (currentUser.Role == UserRole.Admin && createdGroup.Memberships.FirstOrDefault(m => m.UserId == targetUser.Id) is { } existingMembership)
        {
            createdGroup.Memberships.Remove(existingMembership);
            _dbContext.Groups.Update(createdGroup);
            _dbContext.SaveChanges();
        }
    }

    public void RemoveUserFromGroup(User currentUser, User targetUser, Guid group)
    {
        if (GetGroups().FirstOrDefault(u => u.Id == group) is not { } existingGroup)
        {
            return;
        }
        if (currentUser.Role == UserRole.Admin && existingGroup.Memberships.FirstOrDefault(m => m.UserId == targetUser.Id) is { } existingMembership)
        {
            existingGroup.Memberships.Remove(existingMembership);
            _dbContext.Groups.Update(existingGroup);
            _dbContext.SaveChanges();
        }
    }
}
