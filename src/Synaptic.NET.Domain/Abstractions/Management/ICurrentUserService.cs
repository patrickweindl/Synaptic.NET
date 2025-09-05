using Synaptic.NET.Domain.Resources.Management;

namespace Synaptic.NET.Domain.Abstractions.Management;

public interface ICurrentUserService
{
    public string GetUserIdentifier()
    {
        return GetCurrentUser().Identifier;
    }

    User GetCurrentUser();
}
