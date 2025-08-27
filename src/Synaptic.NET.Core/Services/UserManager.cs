using System.Collections.Concurrent;
using System.Security.Claims;
using System.Text.Json;
using Synaptic.NET.Domain;
using Synaptic.NET.Domain.Enums;
using Synaptic.NET.Domain.Helpers;
using Synaptic.NET.Domain.Resources;

namespace Synaptic.NET.Core.Services;

public class UserManager : IUserManager
{
    private readonly ClaimsIdentity _userManagerIdentity = new(new List<Claim> { new(ClaimTypes.Name, "userManager") });
    private readonly SynapticServerSettings _settings;
    private readonly IEncryptionService _encryptionService;
    private readonly ISymLinkUserService _symLinkUserService;
    private string UserInfoPath => Path.Join(_settings.BaseDataPath, "userInformation.json");
    private readonly Lock _userLock = new();
    private readonly ConcurrentBag<User> _users;

    public UserManager(ISymLinkUserService symLinkUserService, IEncryptionService encryptionService, SynapticServerSettings settings)
    {
        _symLinkUserService = symLinkUserService;
        _settings = settings;
        _encryptionService = encryptionService;
        _users = Init();
    }

    private ConcurrentBag<User> Init()
    {
        if (File.Exists(UserInfoPath))
        {
            string encryptedSettings = File.ReadAllText(UserInfoPath);
            string json = _encryptionService.Decrypt(encryptedSettings, _userManagerIdentity);
            var users = JsonSerializer.Deserialize<List<User>>(json, JsonSerializerOptions.Default) ?? new List<User>();
            List<User> returnUsers = new();
            foreach (var user in users)
            {
                Directory.CreateDirectory(user.GetStorageDirectory(_settings));
                if (_settings.AdminIdentifiers.Contains(user.UserIdentifier))
                {
                    user.Role = UserRole.Admin;
                }
                returnUsers.Add(user);
            }
            SaveUsers(returnUsers);
            return new(returnUsers);
        }

        if (Directory.Exists(Path.Join(_settings.BaseDataPath, "users")))
        {
            List<User> existingUsers = new();
            foreach (var userDir in Directory.GetDirectories(Path.Join(_settings.BaseDataPath, "users")))
            {
                string userIdentifier = new DirectoryInfo(userDir).Name;
                User newUser = new()
                {
                    UserIdentifier = userIdentifier,
                    DisplayName = userIdentifier.Split("__").FirstOrDefault() ?? userIdentifier,
                    Role = UserRole.User
                };
                GetOrCreateUser(newUser.UserIdentifier, out User user, newUser.Role);
                if (_settings.AdminIdentifiers.Contains(newUser.UserIdentifier))
                {
                    newUser.Role = UserRole.Admin;
                }
                existingUsers.Add(user);
            }
            SaveUsers(existingUsers);
            return new(existingUsers);
        }

        return [];
    }

    private void SaveUsers(List<User> users)
    {
        lock (_userLock)
        {
            if (!File.Exists(UserInfoPath))
            {
                File.Create(UserInfoPath).Close();
            }

            string json = JsonSerializer.Serialize(users, JsonSerializerOptions.Default);
            string encryptedSettings = _encryptionService.Encrypt(json, _userManagerIdentity);
            File.WriteAllText(UserInfoPath, encryptedSettings);
        }
    }

    public List<User> GetUsers()
    {
        return _users.ToList();
    }

    public List<User> GetGroups()
    {
        return _users.Where(u => u.UserIdentifier.StartsWith("group-") && Guid.TryParse(u.UserIdentifier.Split("__").Last(), out _)).ToList();
    }

    public User GetOrCreateUser(string userIdentifier, UserRole role = UserRole.User)
    {
        userIdentifier = _symLinkUserService.GetMainIdentity(userIdentifier.ToClaimsIdentity()).ToUserIdentifier();
        GetOrCreateUser(userIdentifier, out User user, role);
        return user;
    }

    public bool GetOrCreateUser(string userIdentifier, out User user, UserRole role = UserRole.Guest)
    {
        userIdentifier = _symLinkUserService.GetMainIdentity(userIdentifier.ToClaimsIdentity()).ToUserIdentifier();
        if (_users.FirstOrDefault(u => u.UserIdentifier == userIdentifier) is { } existingUser)
        {
            user = existingUser;
            return false;
        }

        User newUser = new()
        {
            UserIdentifier = userIdentifier,
            Role = role,
            DisplayName = userIdentifier.Split("__").FirstOrDefault() ?? userIdentifier
        };
        Directory.CreateDirectory(newUser.GetStorageDirectory(_settings));
        _users.Add(newUser);
        SaveUsers(_users.ToList());
        user = newUser;
        return true;
    }

    public void SetUserRole(User currentUser, User targetUser, UserRole targetRole)
    {
        if (currentUser.Role == UserRole.Admin)
        {
            targetUser.Role = targetRole;
            SaveUsers(_users.ToList());
        }
    }

    public void CreateGroup(User currentUser, string groupName)
    {
        if (currentUser.Role != UserRole.Admin)
        {
            return;
        }

        string groupUserName = $"group-{groupName}__{Guid.NewGuid():N}";
        GetOrCreateUser(groupUserName);
    }

    public string ReadableGroupNameToUserGroupIdentifier(string groupName)
    {
        if (GetUsers().FirstOrDefault(u => u.UserIdentifier.StartsWith($"group-{groupName}__")) is { } existingGroup)
        {
            return existingGroup.UserIdentifier;
        }
        return $"group-{groupName}__{Guid.NewGuid():N}";
    }

    public string GroupIdentifierToReadableName(string groupIdentifier)
    {
        return groupIdentifier.Split("__").First().Replace("group-", "");
    }

    public void AddUserToGroup(User currentUser, User targetUser, string group)
    {
        string groupUserName = ReadableGroupNameToUserGroupIdentifier(group);
        if (GetUsers().All(u => u.UserIdentifier != groupUserName))
        {
            return;
        }
        if (currentUser.Role == UserRole.Admin)
        {
            targetUser.AddToGroup(groupUserName);
            SaveUsers(_users.ToList());
        }
    }

    public void RemoveUserFromGroup(User currentUser, User targetUser, string group)
    {
        string groupUserName = ReadableGroupNameToUserGroupIdentifier(group);
        if (currentUser.Role == UserRole.Admin)
        {
            targetUser.RemoveFromGroup(groupUserName);
            SaveUsers(_users.ToList());
        }
    }
}
