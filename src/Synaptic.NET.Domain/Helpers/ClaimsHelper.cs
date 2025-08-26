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

    public static string ToUserName(this ClaimsIdentity? claimsIdentity) => claimsIdentity?.FindFirst(ClaimTypes.Name)?.Value ?? "default";

    public static string ToUserId(this ClaimsIdentity? claimsIdentity) => claimsIdentity?.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "default";

    public static ClaimsIdentity FromUserNameAndId(string userName, string userId)
    {
        var identity = new ClaimsIdentity();
        identity.AddClaim(new Claim(ClaimTypes.Name, userName));
        identity.AddClaim(new Claim(ClaimTypes.NameIdentifier, userId));
        return identity;
    }
}
