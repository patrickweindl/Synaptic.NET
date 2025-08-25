using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Http;

namespace Synaptic.NET.Authentication.Controllers;

public class CookieAuthenticationStateProvider : AuthenticationStateProvider
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IDataProtectionProvider _dataProtectionProvider;
    private AuthenticationState? _cachedState;
    private bool _isInitialized;

    public CookieAuthenticationStateProvider(IHttpContextAccessor httpContextAccessor, IDataProtectionProvider dataProtectionProvider)
    {
        _dataProtectionProvider = dataProtectionProvider;
        _httpContextAccessor = httpContextAccessor;
    }

    public override async Task<AuthenticationState> GetAuthenticationStateAsync()
    {
        if (_isInitialized && _cachedState != null)
        {
            return _cachedState;
        }

        var httpContext = _httpContextAccessor.HttpContext;

        if (httpContext?.User.Identity?.IsAuthenticated == true)
        {
            _cachedState = new AuthenticationState(httpContext.User);
            _isInitialized = true;
            return _cachedState;
        }

        const string cookieName = $".AspNetCore.{CookieAuthenticationDefaults.AuthenticationScheme}";
        if (httpContext?.Request.Cookies.TryGetValue(cookieName, out string? cookieValue) == true
            && !string.IsNullOrEmpty(cookieValue))
        {
            try
            {
                var principal = await ParseCookieTicketAsync(cookieValue);
                if (principal?.Identity?.IsAuthenticated == true)
                {
                    _cachedState = new AuthenticationState(principal);
                    return _cachedState;
                }
            }
            catch (Exception ex)
            {
                Log.Logger.Error(ex, "Failed to parse cookie.");
            }
        }

        _cachedState = new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity()));
        return _cachedState;
    }

    private Task<ClaimsPrincipal?> ParseCookieTicketAsync(string cookieValue)
    {
        ClaimsPrincipal? result = null;
        try
        {
            var dataProtector = _dataProtectionProvider.CreateProtector(
                "Microsoft.AspNetCore.Authentication.Cookies.CookieAuthenticationMiddleware",
                CookieAuthenticationDefaults.AuthenticationScheme,
                "v2");

            string ticketData = dataProtector.Unprotect(cookieValue);
            var ticket = TicketSerializer.Default.Deserialize(Encoding.UTF8.GetBytes(ticketData));

            if (ticket?.Principal != null &&
                ticket.Properties.ExpiresUtc > DateTimeOffset.UtcNow)
            {
                result = ticket.Principal;
            }
        }
        catch
        {
            // Cookie ist ungültig oder abgelaufen
        }

        return Task.FromResult(result);
    }
}
