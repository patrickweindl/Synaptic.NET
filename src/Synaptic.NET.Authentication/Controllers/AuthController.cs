using System.Security.Cryptography;
using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Synaptic.NET.Authentication.Providers;
using Synaptic.NET.Authentication.Resources;
using Synaptic.NET.Domain.Helpers;
using Synaptic.NET.Domain.Resources;
using Synaptic.NET.Domain.Resources.Configuration;
using Synaptic.NET.Domain.Resources.Management;

namespace Synaptic.NET.Authentication.Controllers;

public class AuthController : ControllerBase
{
    private readonly SynapticServerSettings _settings;
    private readonly ISecurityTokenHandler _tokenHandler;
    private readonly IRefreshTokenHandler _refreshTokenHandler;
    private readonly RedirectUriProvider _redirectUriProvider;
    private readonly CodeBasedAuthProvider _codeBasedAuthProvider;
    private readonly IDbContextFactory<SynapticDbContext> _dbContextFactory;
    public AuthController(
        SynapticServerSettings settings,
        ISecurityTokenHandler tokenHandler,
        IRefreshTokenHandler refreshTokenHandler,
        RedirectUriProvider redirectUriProvider,
        CodeBasedAuthProvider codeBasedAuthProvider,
        IDbContextFactory<SynapticDbContext> dbContextFactory)
    {
        _settings = settings;
        _tokenHandler = tokenHandler;
        _refreshTokenHandler = refreshTokenHandler;
        _redirectUriProvider = redirectUriProvider;
        _codeBasedAuthProvider = codeBasedAuthProvider;
        _dbContextFactory = dbContextFactory;
    }

    [HttpHead("/")]
    [AllowAnonymous]
    public IActionResult Head()
    {
        return Ok();
    }

    [HttpHead("/mcp")]
    [AllowAnonymous]
    public IActionResult McpHead()
    {
        return Ok();
    }

    [HttpOptions("register")]
    [AllowAnonymous]
    public IActionResult OptionsRegistration()
    {
        Response.Headers.Append("Allow", "POST");
        return Ok();
    }

    [HttpPost("register/mcp")]
    [AllowAnonymous]
    public IActionResult McpRegistration()
    {
        return LocalRedirect("/register");
    }

    [HttpPost("register")]
    [Produces("application/json")]
    [AllowAnonymous]
    public async Task<IActionResult> Registration([FromQuery] Dictionary<string, string>? inputQuery, [FromBody] object? body)
    {
        return await BuildRegistrationResponseAsync(body);
    }

    private async Task<ContentResult> BuildRegistrationResponseAsync(object? body)
    {
        await using var dbContext = await _dbContextFactory.CreateDbContextAsync();
        var expiredRegistrations = await dbContext.DynamicRegistrations.Where(r => r.ExpiresAt < DateTimeOffset.UtcNow).ToListAsync();
        foreach (var expiredRegistration in expiredRegistrations)
        {
            dbContext.DynamicRegistrations.Remove(expiredRegistration);
        }
        await dbContext.SaveChangesAsync();
        Guid registrationId = Guid.NewGuid();
        DateTimeOffset expiresAt = DateTimeOffset.UtcNow.AddHours(0.5);

        Log.Logger.Information($"[Authorization] New registration with {registrationId}.");
        JsonElement json = JsonSerializer.SerializeToElement(body);
        var req = JsonNode.Parse(json.GetRawText())?.AsObject();
        string tokenAuth = req?["token_endpoint_auth_method"]?.GetValue<string>() ?? "client_secret_basic";
        var now = DateTimeOffset.UtcNow;
        string clientSecret = Convert.ToHexString(RandomNumberGenerator.GetBytes(32));
        long issuedAt = now.ToUnixTimeSeconds();
        long expiry = expiresAt.ToUnixTimeSeconds();

        if (body == null)
        {
            var noBodyResponse = new JsonObject
            {
                ["client_id"] = registrationId,
                ["client_id_issued_at"] = issuedAt,
                ["token_endpoint_auth_method"] = tokenAuth,
                ["client_secret"] = clientSecret,
                ["client_secret_expires_at"] = expiry,
                ["grant_types"] = new JsonArray { "authorization_code", "refresh_token"}
            };
            return new ContentResult
            {
                StatusCode = StatusCodes.Status201Created,
                ContentType = "application/json",
                Content = noBodyResponse.ToJsonString(new JsonSerializerOptions { WriteIndented = false })
            };
        }



        DynamicRegistration newRegistration = new(){ Id = Guid.NewGuid(), RegistrationId = registrationId, OriginalBody = json, ExpiresAt = expiresAt, Secret = clientSecret };

        await dbContext.DynamicRegistrations.AddAsync(newRegistration);
        await dbContext.SaveChangesAsync();
        var resp = new JsonObject
        {
            ["client_id"] = registrationId,
            ["client_id_issued_at"] = issuedAt,
            ["token_endpoint_auth_method"] = tokenAuth,
            ["client_secret"] = clientSecret,
            ["client_secret_expires_at"] = expiry
        };
        foreach (var kv in req ?? new())
        {
            if (kv.Key is "client_id" or "client_secret" or "client_id_issued_at" or "client_secret_expires_at")
            {
                continue;
            }
            resp[kv.Key] = kv.Value?.DeepClone();
        }
        Log.Logger.Information($"[Dynamic Registration] New Dynamic Registration with ID {registrationId} created. It will expire at {expiresAt}.");
        return new ContentResult
        {
            StatusCode = StatusCodes.Status201Created,
            ContentType = "application/json",
            Content = resp.ToJsonString(new JsonSerializerOptions { WriteIndented = false })
        };
    }

    [HttpGet("login")]
    [AllowAnonymous]
    [ProducesResponseType(302, Description = "Redirects to a selection page for an OAuth provider.")]
    public async Task<IActionResult> LoginAsync([FromBody] object? body, [FromQuery] Dictionary<string, string>? inputQuery)
    {
        return await AuthorizeAsync(body, inputQuery);
    }

    [HttpGet("authorize")]
    [AllowAnonymous]
    [ProducesResponseType(302, Description = "Redirects to a selection page for an OAuth provider.")]
    public async Task<IActionResult> AuthLoginAsync([FromBody] object? body, [FromQuery] Dictionary<string, string>? inputQuery)
    {
        return await AuthorizeAsync(body, inputQuery);
    }

    public async Task<IActionResult> AuthorizeAsync([FromBody] object? body, [FromQuery] Dictionary<string, string>? inputQuery)
    {
        string queryString = string.Empty;
        if (inputQuery != null)
        {
            await using var dbContext = await _dbContextFactory.CreateDbContextAsync();
            var expiredRegistrations = await dbContext.DynamicRegistrations.Where(r => r.ExpiresAt < DateTimeOffset.UtcNow).ToListAsync();
            foreach (var expiredRegistration in expiredRegistrations)
            {
                dbContext.DynamicRegistrations.Remove(expiredRegistration);
            }

            await dbContext.SaveChangesAsync();

            var validRegistrations = await dbContext.DynamicRegistrations.ToListAsync();

            queryString = string.Join("&", inputQuery.Where(k => k.Key != null && k.Value != null).Select(kvp => $"{Uri.EscapeDataString(kvp.Key)}={Uri.EscapeDataString(kvp.Value)}"));
            Log.Information($"[Authorization] Received authorize GET with query {queryString}.");
            if (inputQuery.TryGetValue("client_id", out string? clientId)
                && !string.IsNullOrEmpty(clientId)
                && validRegistrations.FirstOrDefault(r => r.RegistrationId.ToString() == clientId) is not null)
            {
                Log.Debug($"[Authorization] A dynamic registration was found for {clientId}. Information extraction is currently not implemented.");
            }
        }
        string redirectUri = string.IsNullOrEmpty(queryString)
            ? "/oauth-select"
            : $"/oauth-select?{queryString}";
        return LocalRedirect(redirectUri);
    }

    [HttpGet("/oauth-callback")]
    [AllowAnonymous]
    public IActionResult OAuthCallback(
        [FromQuery(Name = "state")] string state,
        [FromQuery(Name = "code")] string code)
    {
        if (string.IsNullOrEmpty(state))
        {
            Log.Error("[Authorization] OAuth callback received without state.");
            return BadRequest("Missing state parameter.");
        }
        if (!_redirectUriProvider.GetRedirectUri(state, out RedirectSettings redirectUri))
        {
            Log.Error($"[Authorization] OAuth callback received with unknown state: {state}.");
            return BadRequest("Unknown state parameter.");
        }

        if (string.IsNullOrEmpty(code))
        {
            Log.Error("[Authorization] OAuth callback received without code.");
            return BadRequest("Missing code parameter.");
        }
        _codeBasedAuthProvider.AddCodeIdentityProvider(code, redirectUri.Provider);

        Log.Debug($"[Authorization] OAuth callback received with state: {state}, redirecting to {redirectUri.Uri}.");
        return Redirect($"{redirectUri.Uri}?state={state}&code={code}");
    }

    private async Task<DynamicRegistration?> TryGetDynamicRegistrationAsync()
    {
        if (!HttpContext.Request.Headers.Authorization.Contains("Basic"))
        {
            return null;
        }

        string secret = HttpContext.Request.Headers.Authorization.ToString().Replace("Basic ", "");
        await using var dbContext = await _dbContextFactory.CreateDbContextAsync();
        var expiredRegistrations = await dbContext.DynamicRegistrations.Where(r => r.ExpiresAt < DateTimeOffset.UtcNow).ToListAsync();
        foreach (var expiredRegistration in expiredRegistrations)
        {
            dbContext.DynamicRegistrations.Remove(expiredRegistration);
        }
        await dbContext.SaveChangesAsync();

        if (await dbContext.DynamicRegistrations.FirstOrDefaultAsync(r => r.Secret == secret) is { } dynamicRegistration)
        {
            return dynamicRegistration;
        }

        return null;
    }

    [HttpPost("token")]
    [AllowAnonymous]
    [Produces("application/json")]
    [ProducesResponseType(200, Type = typeof(AccessTokenResult), Description = "Returned when the authentication was successful and a JWT token was generated.")]
    [ProducesResponseType(401, Description = "Returned when the user is not authorized to access the API.")]
    [ProducesResponseType(500, Description = "Returned when the authentication failed due to missing config entries.")]
    public async Task<IActionResult> Token(
        [FromForm] string code,
        [FromForm(Name = "client_id")] string clientId,
        [FromForm(Name = "client_secret")] string clientSecret,
        [FromForm(Name = "redirect_uri")] string redirectUri,
        [FromForm(Name = "grant_type")] string grantType,
        [FromForm(Name = "code_verifier")] string codeVerifier,
        [FromForm(Name = "refresh_token")] string refreshToken = "")
    {
        if (string.IsNullOrEmpty(_settings.ServerSettings.JwtIssuer))
        {
            Log.Error("Missing JWT Issuer");
            return StatusCode(500);
        }

        if (string.IsNullOrEmpty(_settings.JwtKey))
        {
            Log.Error("Missing JWT Secret");
            return StatusCode(500);
        }

        if (grantType.ToLowerInvariant().Contains("refresh") && !string.IsNullOrEmpty(refreshToken))
        {
            if (await _refreshTokenHandler.ValidateRefreshTokenAsync(refreshToken) is not { } claimsIdentity)
            {
                Log.Error("Invalid refresh token");
                return StatusCode(401, "Invalid refresh token");
            }

            try
            {
                await _refreshTokenHandler.InvalidateRefreshTokenAsync(refreshToken);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Failed to invalidate refresh token");
            }
            return Ok(await _tokenHandler.GenerateJwtTokenAsync(_settings.JwtKey, _settings.ServerSettings.JwtIssuer, _settings.JwtTokenLifetime, claimsIdentity));

        }
        if (!_codeBasedAuthProvider.GetIdentityProviderByCode(code, out string? provider))
        {
            if (!redirectUri.Contains("localhost"))
            {
                Log.Error($"[Authentication Token] No provider found for code: {code}");
                return StatusCode(401, "Invalid code");
            }
            Log.Warning("[Authentication Token] Could not find original provider for code. Assuming GitHub.");
            provider = "github";
        }

        if (await TryGetDynamicRegistrationAsync() is { } clientRegistration)
        {
            Log.Logger.Information("[Authorization Token] Received dynamic registration flow. Removing registration.");

            await using var dbContext = await _dbContextFactory.CreateDbContextAsync();
            dbContext.DynamicRegistrations.Remove(clientRegistration);
            await dbContext.SaveChangesAsync();

            clientId = clientRegistration.RegistrationId.ToString();
            Log.Logger.Information("[Authorization Token] Retrieved client ID from dynamic registration.");
            clientSecret = clientRegistration.Secret;
        }

        clientId = provider switch
        {
            "github" => _settings.GitHubOAuthProviderSettings.ClientId,
            "google" => _settings.GoogleOAuthProviderSettings.ClientId,
            "microsoft" => _settings.MicrosoftOAuthProviderSettings.ClientId,
            _ => clientId
        };
        clientSecret = provider switch
        {
            "github" => _settings.GitHubOAuthProviderSettings.ClientSecret,
            "google" => _settings.GoogleOAuthProviderSettings.ClientSecret,
            "microsoft" => _settings.MicrosoftOAuthProviderSettings.ClientSecret,
            _ => clientSecret
        };

        VerificationResult validationResult = provider switch
        {
            "google" => await _tokenHandler.VerifyGoogleAuthentication(_settings, clientId, clientSecret, code, $"{_settings.ServerSettings.ServerUrl}/oauth-callback", grantType,
                codeVerifier),
            "microsoft" => await _tokenHandler.VerifyMicrosoftAuthentication(_settings, clientId, clientSecret, code, $"{_settings.ServerSettings.ServerUrl}/oauth-callback", grantType,
                codeVerifier),
            "github" => await _tokenHandler.VerifyGitHubAuthentication(_settings, clientId, clientSecret, code, grantType,
                codeVerifier),
            _ => new VerificationResult { Success = false, UserId = string.Empty, UserName = string.Empty }
        };

        if (!validationResult.Success)
        {
            return StatusCode(401);
        }
        if (string.IsNullOrEmpty(validationResult.UserId) || string.IsNullOrEmpty(validationResult.UserName))
        {
            Log.Error("Invalid user data received from OAuth provider.");
            return StatusCode(500, "Invalid user data received from OAuth provider.");
        }

        var identity = ClaimsHelper.ClaimsIdentityFromUserNameAndId(validationResult.UserName, validationResult.UserId);
        AccessTokenResult token = await _tokenHandler.GenerateJwtTokenAsync(_settings.JwtKey, _settings.ServerSettings.JwtIssuer, _settings.JwtTokenLifetime, identity);
        return Ok(token);
    }
}
