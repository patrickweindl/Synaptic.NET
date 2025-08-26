using System.Security.Claims;
using Synaptic.NET.Authentication.Providers;
using Synaptic.NET.Core;

namespace Synaptic.NET.Authentication.Services;

public class CurrentUserService : ICurrentUserService
{
    private readonly IHttpContextAccessor? _accessor;
    private readonly ISymLinkUserService _symlinkUserService;
    private readonly CookieAuthenticationStateProvider? _cookieAuthenticationStateProvider;
    public CurrentUserService(ISymLinkUserService symlinkUserService, CookieAuthenticationStateProvider? cookieAuthenticationStateProvider = null, IHttpContextAccessor? accessor = null)
    {
        _accessor = accessor;
        _symlinkUserService = symlinkUserService;
        _cookieAuthenticationStateProvider = cookieAuthenticationStateProvider;
    }

    public ClaimsIdentity GetUserClaimIdentity()
    {
        var cookieState = _cookieAuthenticationStateProvider?.GetAuthenticationStateAsync().Result;

        if (cookieState?.User is { Identity: { IsAuthenticated: true } } &&
            cookieState.User.FindFirst(ClaimTypes.NameIdentifier) is { } nameIdentifier &&
            cookieState.User.FindFirst(ClaimTypes.Name) is { } name)
        {
            var cookieClaim = new ClaimsIdentity();
            cookieClaim.AddClaim(new Claim(ClaimTypes.NameIdentifier, nameIdentifier.Value));
            cookieClaim.AddClaim(new Claim(ClaimTypes.Name, name.Value));
            return cookieClaim;
        }

        if (_accessor?.HttpContext == null || _accessor.HttpContext.User is { Identity: { IsAuthenticated: false } })
        {
            return new ClaimsIdentity();
        }

        if (_accessor.HttpContext.User.Identity is ClaimsIdentity httpClaimsIdentity)
        {
            return _symlinkUserService.GetMainIdentity(httpClaimsIdentity);
        }

        if (_accessor.HttpContext?.User is not { Identity: { IsAuthenticated: true } } principal
            || string.IsNullOrEmpty(principal.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Name)?.Value)
            || string.IsNullOrEmpty(principal.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value))
        {
            return new ClaimsIdentity();
        }

        var claimsIdentity = new ClaimsIdentity();
        claimsIdentity.AddClaim(new Claim(ClaimTypes.NameIdentifier, principal.Claims.First(c => c.Type == ClaimTypes.NameIdentifier).Value));
        claimsIdentity.AddClaim(new Claim(ClaimTypes.Name, principal.Claims.First(c => c.Type == ClaimTypes.Name).Value));
        return _symlinkUserService.GetMainIdentity(claimsIdentity);

    }
}
