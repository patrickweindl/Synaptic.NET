using Synaptic.NET.Domain.Enums;
using Synaptic.NET.Domain.Resources.Configuration;
using Synaptic.NET.Domain.Resources.Management;
using Synaptic.NET.Domain.Resources.Storage;

namespace Synaptic.NET.Domain.Abstractions.Management;

/// <summary>
/// Defines the contract for an entity that is managed within the system by its unique identity and attributes.
/// Provides a way to encapsulate identity, metadata, and associated stores or memberships.
/// Common implementations may include users, groups, or other identifiable entities.
/// </summary>
public interface IManagedIdentity
{
    /// <summary>
    /// Represents the unique identifier for an entity that implements the IManagedIdentity interface.
    /// This identifier is immutable and globally unique, serving as the primary key to distinguish
    /// entities such as users or groups within the system.
    /// </summary>
    Guid Id { get; }

    /// <summary>
    /// Represents a unique, user-defined identifier for an entity implementing the IManagedIdentity interface.
    /// This property is used to distinctly recognize and manage entities such as users or groups within the system.
    /// </summary>
    string Identifier { get; }

    string DisplayName { get; }

    IdentityRole Role { get; }

    ICollection<MemoryStore> Stores { get; }

    ICollection<GroupMembership> Memberships { get; }

    public string GetStorageDirectory(SynapticServerSettings settings)
    {
        Directory.CreateDirectory(Path.Join(settings.BaseDataPath, "storage"));
        Directory.CreateDirectory(Path.Join(settings.BaseDataPath, "storage", Identifier));
        return Path.Join(settings.BaseDataPath, "storage", Identifier);
    }
}
