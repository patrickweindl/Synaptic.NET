using System.Security.Claims;

namespace Synaptic.NET.Domain.Helpers;

public static class ClaimsHelper
{
    public static string ToUserIdentifier(this ClaimsIdentity? claimsIdentity)
    {
        if (claimsIdentity is null)
        {
            return "default__default";
        }
        string userName = claimsIdentity.FindFirst(ClaimTypes.Name)?.Value ?? "default";
        string id = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "default";
        return $"{userName}__{id}";
    }
}
