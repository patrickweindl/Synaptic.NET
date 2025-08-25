using System.Security.Claims;
using Synaptic.NET.Domain.Helpers;

namespace Synaptic.NET.Core;

public interface ICurrentUserService
{
    public string GetUserIdentifier()
    {
        return GetUserClaimIdentity().ToUserIdentifier();
    }

    ClaimsIdentity GetUserClaimIdentity();

    public IReadOnlyDictionary<string, string> GetAllClaims()
    {
        return GetUserClaimIdentity().Claims.ToDictionary(c => c.Type, c => c.Value);
    }
}
