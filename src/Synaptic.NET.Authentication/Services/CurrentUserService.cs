using System.Collections.Concurrent;
using System.Security.Claims;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.EntityFrameworkCore;
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
    private readonly IDbContextFactory<SynapticDbContext> _dbContextFactory;
    private readonly SynapticServerSettings _settings;
    public CurrentUserService(SynapticServerSettings serverSettings, IDbContextFactory<SynapticDbContext> dbContextFactory, ISymLinkUserService symlinkUserService, AuthenticationStateProvider? authenticationStateProvider = null, IHttpContextAccessor? accessor = null)
    {
        _accessor = accessor;
        _symlinkUserService = symlinkUserService;
        _authenticationStateProvider = authenticationStateProvider;
        _dbContextFactory = dbContextFactory;
        _settings = serverSettings;
    }

    private async Task<ClaimsIdentity?> TryGetClaimsIdentityFromCookie()
    {
        if (_authenticationStateProvider == null)
        {
            return null;
        }
        var cookieState = await _authenticationStateProvider.GetAuthenticationStateAsync();

        if (cookieState.User is not { Identity.IsAuthenticated: true } ||
            cookieState.User.FindFirst(ClaimTypes.NameIdentifier) is not { } nameIdentifier ||
            cookieState.User.FindFirst(ClaimTypes.Name) is not { } name)
        {
            return null;
        }

        var cookieClaim = new ClaimsIdentity();
        cookieClaim.AddClaim(new Claim(ClaimTypes.NameIdentifier, nameIdentifier.Value));
        cookieClaim.AddClaim(new Claim(ClaimTypes.Name, name.Value));
        return cookieClaim;
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

    private User? _currentUser;

    public Task SetCurrentUserAsync(User user)
    {
        _currentUser = user;
        return Task.CompletedTask;
    }

    private ConcurrentDictionary<string, User> _userCache = new();

    public async Task<User> GetCurrentUserAsync()
    {
        if (_currentUser != null)
        {
            return _currentUser;
        }
        ClaimsIdentity? currentClaimsIdentity;

        ClaimsIdentity? cookieClaimsIdentity = await TryGetClaimsIdentityFromCookie();
        if (cookieClaimsIdentity != null)
        {
            string cookieIdentifier = cookieClaimsIdentity.ToUserIdentifier();
            if (_userCache.TryGetValue(cookieIdentifier, out var cookieUser))
            {
                return cookieUser;
            }
            currentClaimsIdentity = await _symlinkUserService.GetMainIdentityAsync(cookieClaimsIdentity);
        }
        else if (TryGetClaimsIdentityFromHttpContext() is { } httpClaimsIdentity)
        {
            string httpIdentifier = httpClaimsIdentity.ToUserIdentifier();
            if (_userCache.TryGetValue(httpIdentifier, out var httpUser))
            {
                return httpUser;
            }
            currentClaimsIdentity = await _symlinkUserService.GetMainIdentityAsync(httpClaimsIdentity);
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

        if (_userCache.TryGetValue(identifier, out var user))
        {
            return user;
        }

        await using var dbContext = await _dbContextFactory.CreateDbContextAsync();

        user = await dbContext.Users.FirstOrDefaultAsync(u => u.Identifier == identifier);
        if (user == null)
        {
            user = new User
            {
                Identifier = identifier,
                DisplayName = identifier.Split("__").FirstOrDefault() ?? identifier,
                Role = IdentityRole.Guest
            };
            await dbContext.Users.AddAsync(user);
            await dbContext.SaveChangesAsync();
        }

        _userCache.TryAdd(user.Identifier, user);
        if (!_settings.ServerSettings.AdminIdentifiers.Contains(identifier) || user.Role == IdentityRole.Admin)
        {
            return user;
        }

        user.Role = IdentityRole.Admin;
        await dbContext.SaveChangesAsync();
        return user;
    }
}
