using System.Text.Json.Serialization;
using Synaptic.NET.Domain.Enums;

namespace Synaptic.NET.Domain.Resources;

public class User : IComparable<User>, IEquatable<User>
{
    [JsonPropertyName("user_identifier")]
    public required string UserIdentifier { get; set; }

    [JsonPropertyName("display_name")]
    public string DisplayName { get; set; } = string.Empty;

    public string UserName => UserIdentifier.Split("__").FirstOrDefault() ?? UserIdentifier;

    public string UserAuthId => UserIdentifier.Split("__").LastOrDefault() ?? UserIdentifier;

    [JsonPropertyName("user_groups")]
    public List<string> Groups { get; set; } = new();

    [JsonPropertyName("user_role")]
    public UserRole Role { get; set; } = UserRole.Guest;

    public int CompareTo(User? other)
    {
        return other == null
            ? 1
            : string.Compare(UserIdentifier, other.UserIdentifier, StringComparison.InvariantCulture);
    }

    public bool Equals(User? other)
    {
        return other != null && string.Equals(UserIdentifier, other.UserIdentifier, StringComparison.InvariantCulture);
    }

    public override string ToString() => UserIdentifier;

    public string GetStorageDirectory(SynapticServerSettings settings)
    {
        Directory.CreateDirectory(Path.Join(settings.BaseDataPath, "users"));
        return Path.Join(settings.BaseDataPath, "users", UserIdentifier);
    }

    public void AddToGroup(string groupIdentifier)
    {
        if (!Groups.Contains(groupIdentifier))
        {
            Groups.Add(groupIdentifier);
        }
    }

    public void RemoveFromGroup(string groupIdentifier)
    {
        Groups.Remove(groupIdentifier);
    }

    public bool CanAccessGroup(string groupIdentifier)
    {
        return Groups.Contains(groupIdentifier);
    }

    public static bool CanAccessGroup(User user, string groupIdentifier)
    {
        return user.CanAccessGroup(groupIdentifier);
    }
}
