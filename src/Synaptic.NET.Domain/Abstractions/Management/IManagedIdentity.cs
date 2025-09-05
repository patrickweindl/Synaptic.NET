using Synaptic.NET.Domain.Resources.Configuration;
using Synaptic.NET.Domain.Resources.Management;
using Synaptic.NET.Domain.Resources.Storage;

namespace Synaptic.NET.Domain.Abstractions.Management;

public interface IManagedIdentity
{
    Guid Id { get; }

    string Identifier { get; }

    string DisplayName { get; }

    ICollection<MemoryStore> Stores { get; }

    ICollection<GroupMembership> Memberships { get; }

    public string GetStorageDirectory(SynapticServerSettings settings)
    {
        Directory.CreateDirectory(Path.Join(settings.BaseDataPath, "storage"));
        return Path.Join(settings.BaseDataPath, "storage", Identifier);
    }
}
