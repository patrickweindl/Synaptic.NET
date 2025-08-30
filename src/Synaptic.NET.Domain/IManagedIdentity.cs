using Synaptic.NET.Domain.Resources;

namespace Synaptic.NET.Domain;

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
