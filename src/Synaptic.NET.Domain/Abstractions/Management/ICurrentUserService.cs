using Synaptic.NET.Domain.Resources;

namespace Synaptic.NET.Core;

public interface ICurrentUserService
{
    public string GetUserIdentifier()
    {
        return GetCurrentUser().Identifier;
    }

    User GetCurrentUser();
}
