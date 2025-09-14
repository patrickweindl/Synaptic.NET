using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Synaptic.NET.Domain.Resources;
using Synaptic.NET.Domain.Resources.Management;

namespace Synaptic.NET.Authentication.Handlers;

public class ApiKeyAuthenticationSchemeOptions : AuthenticationSchemeOptions
{
    public const string DefaultScheme = "ApiKey";
    public string Scheme => DefaultScheme;
    public readonly string AuthenticationType = DefaultScheme;
}

public class ApiKeyAuthenticationHandler : AuthenticationHandler<ApiKeyAuthenticationSchemeOptions>
{
    private readonly IServiceProvider _serviceProvider;

    public ApiKeyAuthenticationHandler(IOptionsMonitor<ApiKeyAuthenticationSchemeOptions> options,
        ILoggerFactory logger, UrlEncoder encoder, IServiceProvider serviceProvider)
        : base(options, logger, encoder)
    {
        _serviceProvider = serviceProvider;
    }

    protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (!Request.Headers.ContainsKey("Authorization"))
        {
            return AuthenticateResult.NoResult();
        }

        var authHeader = Request.Headers["Authorization"].ToString();
        if (string.IsNullOrEmpty(authHeader) || !authHeader.StartsWith("Bearer "))
        {
            return AuthenticateResult.NoResult();
        }

        var apiKey = authHeader.Substring("Bearer ".Length).Trim();
        if (string.IsNullOrEmpty(apiKey))
        {
            return AuthenticateResult.Fail("Invalid API key format.");
        }

        try
        {
            var user = await ValidateApiKeyAsync(apiKey);
            if (user == null)
            {
                return AuthenticateResult.Fail("No user found with the provided API key.");
            }

            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, user.UserAuthId),
                new Claim(ClaimTypes.Name, user.UserName),
                new Claim(ClaimTypes.Role, user.Role.ToString()),
                new Claim("DisplayName", user.DisplayName)
            };

            var identity = new ClaimsIdentity(claims, Options.AuthenticationType);
            var principal = new ClaimsPrincipal(identity);
            var ticket = new AuthenticationTicket(principal, Options.Scheme);

            Logger.LogInformation("[API Key Authorization] User {User} authenticated successfully with API key", user.Identifier);

            return AuthenticateResult.Success(ticket);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "[API Key Authorization] Authentication failed");
            return AuthenticateResult.Fail("Authentication failed");
        }
    }

    private async Task<User?> ValidateApiKeyAsync(string apiKey)
    {
        using var scope = _serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<SynapticDbContext>();
        var users = await dbContext.Users.Include(u => u.ApiKeys).ToListAsync();

        var foundUser = users.FirstOrDefault(x => x.ApiKeys.Any(k => k.Key == apiKey));
        return foundUser;
    }
}
