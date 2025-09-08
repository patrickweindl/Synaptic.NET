using System.Security.Claims;
using Microsoft.AspNetCore.Components.Authorization;
using Synaptic.NET.Domain.Abstractions.Management;
using Synaptic.NET.Domain.Enums;
using Synaptic.NET.Domain.Helpers;
using Synaptic.NET.Domain.Resources;
using Synaptic.NET.Domain.Resources.Configuration;
using Synaptic.NET.Domain.Resources.Management;

namespace Synaptic.NET.Authentication.Services;

public class CurrentUserService : ICurrentUserService
{
    private readonly IHttpContextAccessor? _accessor;
    private readonly ISymLinkUserService _symlinkUserService;
    private readonly AuthenticationStateProvider? _authenticationStateProvider;
    private readonly SynapticDbContext _dbContext;
    private readonly SynapticServerSettings _settings;
    public CurrentUserService(SynapticServerSettings serverSettings, SynapticDbContext synapticDbContext, ISymLinkUserService symlinkUserService, AuthenticationStateProvider? authenticationStateProvider = null, IHttpContextAccessor? accessor = null)
    {
        _accessor = accessor;
        _symlinkUserService = symlinkUserService;
        _authenticationStateProvider = authenticationStateProvider;
        _dbContext = synapticDbContext;
        _settings = serverSettings;
    }

    private ClaimsIdentity? TryGetClaimsIdentityFromCookie()
    {
        var cookieState = _authenticationStateProvider?.GetAuthenticationStateAsync().Result;

        if (cookieState?.User is { Identity: { IsAuthenticated: true } } &&
            cookieState.User.FindFirst(ClaimTypes.NameIdentifier) is { } nameIdentifier &&
            cookieState.User.FindFirst(ClaimTypes.Name) is { } name)
        {
            var cookieClaim = new ClaimsIdentity();
            cookieClaim.AddClaim(new Claim(ClaimTypes.NameIdentifier, nameIdentifier.Value));
            cookieClaim.AddClaim(new Claim(ClaimTypes.Name, name.Value));
            return cookieClaim;
        }

        return null;
    }

    private ClaimsIdentity? TryGetClaimsIdentityFromHttpContext()
    {
        if (_accessor?.HttpContext is { } httpContext)
        {
            if (httpContext.User.Identity is ClaimsIdentity httpClaimsIdentity)
            {
                return httpClaimsIdentity;
            }

            if (httpContext.User is { Identity: { IsAuthenticated: true } } principal
                && !string.IsNullOrEmpty(principal.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Name)?.Value)
                && !string.IsNullOrEmpty(principal.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value))
            {
                var claimsIdentity = new ClaimsIdentity();
                claimsIdentity.AddClaim(new Claim(ClaimTypes.NameIdentifier, principal.Claims.First(c => c.Type == ClaimTypes.NameIdentifier).Value));
                claimsIdentity.AddClaim(new Claim(ClaimTypes.Name, principal.Claims.First(c => c.Type == ClaimTypes.Name).Value));
                return claimsIdentity;
            }
        }

        return null;
    }

    public User GetCurrentUser()
    {
        ClaimsIdentity? currentClaimsIdentity = null;

        if (TryGetClaimsIdentityFromCookie() is { } cookieClaimsIdentity)
        {
            currentClaimsIdentity = _symlinkUserService.GetMainIdentity(cookieClaimsIdentity);
        }
        else if (TryGetClaimsIdentityFromHttpContext() is { } httpClaimsIdentity)
        {
            currentClaimsIdentity = _symlinkUserService.GetMainIdentity(httpClaimsIdentity);
        }
        else
        {
            currentClaimsIdentity = new ClaimsIdentity();
        }

        string identifier = currentClaimsIdentity.ToUserIdentifier();

        if (string.IsNullOrEmpty(identifier))
        {
            throw new UnauthorizedAccessException();
        }

        var user = _dbContext.Users.FirstOrDefault(u => u.Identifier == identifier);
        if (user == null)
        {
            user = new User
            {
                Identifier = identifier,
                DisplayName = identifier.Split("__").FirstOrDefault() ?? identifier,
                Role = IdentityRole.Guest
            };
            _dbContext.Users.Add(user);
            _dbContext.SaveChanges();
        }

        if (_settings.AdminIdentifiers.Contains(identifier))
        {
            user.Role = IdentityRole.Admin;
            _dbContext.Users.Update(user);
            _dbContext.SaveChanges();
        }

        _dbContext.SetCurrentUser(user);
        return user;
    }
}
