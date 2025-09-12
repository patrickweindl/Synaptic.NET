using Microsoft.EntityFrameworkCore;
using Synaptic.NET.Domain.Abstractions.Management;
using Synaptic.NET.Domain.Enums;
using Synaptic.NET.Domain.Resources;
using Synaptic.NET.Domain.Resources.Management;

namespace Synaptic.NET.Core.Services;

public class UserManager : IUserManager
{
    private readonly SynapticDbContext _dbContext;
    private readonly ICurrentUserService _currentUserService;
    public UserManager(ICurrentUserService currentUserService, SynapticDbContext dbContext)
    {
        _dbContext = dbContext;
        _currentUserService = currentUserService;
    }

    public async Task<List<User>> GetUsersAsync()
    {
        return (await _currentUserService.GetCurrentUserAsync()).Role >= IdentityRole.Admin ? _dbContext.Users.ToList() : [];
    }

    public async Task<List<Group>> GetGroupsAsync()
    {
        return (await _currentUserService.GetCurrentUserAsync()).Role >= IdentityRole.Admin ? _dbContext.Groups.ToList() : [];
    }

    public async Task SetUserRoleAsync(User currentUser, User targetUser, IdentityRole targetRole)
    {
        if (currentUser.Role == IdentityRole.Admin)
        {
            var dbTargetUser = await _dbContext.Users.FirstOrDefaultAsync(u => u.Id == targetUser.Id);
            if (dbTargetUser == null)
            {
                return;
            }
            dbTargetUser.Role = targetRole;
            _dbContext.Users.Update(dbTargetUser);
            await _dbContext.SaveChangesAsync();
        }
    }

    public async Task CreateGroupAsync(User currentUser, string groupName)
    {
        if (currentUser.Role < IdentityRole.Admin)
        {
            return;
        }

        string groupUserName = $"group-{groupName}__{Guid.NewGuid():N}";
        Group newGroup = new() { Identifier = groupUserName, DisplayName = groupName };
        if (_dbContext.Groups.Any(u => u.DisplayName == groupUserName))
        {
            return;
        }
        _dbContext.Groups.Add(newGroup);
        await _dbContext.SaveChangesAsync();
    }

    public async Task RemoveUserFromGroupAsync(User currentUser, User targetUser, Group group)
    {
        if (currentUser.Role < IdentityRole.Admin)
        {
            return;
        }
        if (group.Memberships.FirstOrDefault(m => m.UserId == targetUser.Id) is { } existingMembership)
        {
            group.Memberships.Remove(existingMembership);
            _dbContext.Groups.Update(group);
            await _dbContext.SaveChangesAsync();
        }
    }

    public async Task<string> ReadableGroupNameToUserGroupIdentifierAsync(string groupName)
    {
        if ((await GetGroupsAsync()).FirstOrDefault(u => u.Identifier.StartsWith($"group-{groupName}__")) is { } existingGroup)
        {
            return existingGroup.Identifier;
        }
        return $"group-{groupName}__{Guid.NewGuid():N}";
    }

    public Task<string> GroupIdentifierToReadableNameAsync(string groupIdentifier)
    {
        return Task.FromResult(groupIdentifier.Split("__").First().Replace("group-", ""));
    }

    public async Task<string> GroupIdentifierToReadableNameAsync(Guid groupIdentifier)
    {
        if (await _dbContext.Groups.FirstOrDefaultAsync(g => g.Id == groupIdentifier) is { } group)
        {
            return group.DisplayName;
        }

        return string.Empty;
    }

    public async Task AddUserToGroupAsync(User currentUser, User targetUser, string readableGroupName)
    {
        if ((await GetGroupsAsync()).FirstOrDefault(u => u.DisplayName == readableGroupName) is not { })
        {
            if (currentUser.Role == IdentityRole.Admin)
            {
                await CreateGroupAsync(currentUser, readableGroupName);
            }
            else
            {
                return;
            }
        }

        if ((await GetGroupsAsync()).FirstOrDefault(u => u.DisplayName == readableGroupName) is not { } createdGroup)
        {
            return;
        }

        if (currentUser.Role == IdentityRole.Admin)
        {
            createdGroup.Memberships.Add(new GroupMembership { UserId = targetUser.Id });
            _dbContext.Groups.Update(createdGroup);
            await _dbContext.SaveChangesAsync();
        }
    }

    public async Task AddUserToGroupAsync(User currentUser, User targetUser, Guid group)
    {
        if ((await GetGroupsAsync()).FirstOrDefault(u => u.Id == group) is not { } existingGroup)
        {
            return;
        }

        if (currentUser.Role == IdentityRole.Admin)
        {
            existingGroup.Memberships.Add(new GroupMembership { UserId = targetUser.Id });
            _dbContext.Groups.Update(existingGroup);
            await _dbContext.SaveChangesAsync();
        }
    }

    public async Task AddUserToGroupAsync(User currentUser, User targetUser, Group group)
    {
        if ((await GetGroupsAsync()).FirstOrDefault(u => u.Id == group.Id) is not { })
        {
            if (currentUser.Role == IdentityRole.Admin)
            {
                await CreateGroupAsync(currentUser, group.DisplayName);
            }
            else
            {
                return;
            }
        }

        if ((await GetGroupsAsync()).FirstOrDefault(u => u.DisplayName == group.DisplayName) is not { } createdGroup)
        {
            return;
        }

        if (currentUser.Role == IdentityRole.Admin)
        {
            createdGroup.Memberships.Add(new GroupMembership { UserId = targetUser.Id });
            _dbContext.Groups.Update(createdGroup);
            await _dbContext.SaveChangesAsync();
        }
    }

    public async Task RemoveUserFromGroupAsync(User currentUser, User targetUser, string readableGroupName)
    {
        string groupUserName = await ReadableGroupNameToUserGroupIdentifierAsync(readableGroupName);
        if ((await GetGroupsAsync()).FirstOrDefault(u => u.DisplayName == groupUserName) is not { })
        {
            if (currentUser.Role == IdentityRole.Admin)
            {
                await CreateGroupAsync(currentUser, readableGroupName);
            }
            else
            {
                return;
            }
        }

        if ((await GetGroupsAsync()).FirstOrDefault(u => u.DisplayName == groupUserName) is not { } createdGroup)
        {
            return;
        }

        if (currentUser.Role == IdentityRole.Admin && createdGroup.Memberships.FirstOrDefault(m => m.UserId == targetUser.Id) is { } existingMembership)
        {
            createdGroup.Memberships.Remove(existingMembership);
            _dbContext.Groups.Update(createdGroup);
            await _dbContext.SaveChangesAsync();
        }
    }

    public async Task RemoveUserFromGroupAsync(User currentUser, User targetUser, Guid group)
    {
        if ((await GetGroupsAsync()).FirstOrDefault(u => u.Id == group) is not { } existingGroup)
        {
            return;
        }
        if (currentUser.Role == IdentityRole.Admin && existingGroup.Memberships.FirstOrDefault(m => m.UserId == targetUser.Id) is { } existingMembership)
        {
            existingGroup.Memberships.Remove(existingMembership);
            _dbContext.Groups.Update(existingGroup);
            await _dbContext.SaveChangesAsync();
        }
    }
}
