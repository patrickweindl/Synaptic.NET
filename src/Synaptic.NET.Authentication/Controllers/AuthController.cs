using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Synaptic.NET.Authentication.Providers;
using Synaptic.NET.Authentication.Resources;
using Synaptic.NET.Domain;
using Synaptic.NET.Domain.Helpers;

namespace Synaptic.NET.Authentication.Controllers;

public class AuthController : ControllerBase
{
    private readonly SynapticServerSettings _settings;
    private readonly ISecurityTokenHandler _tokenHandler;
    private readonly IRefreshTokenHandler _refreshTokenHandler;
    private readonly RedirectUriProvider _redirectUriProvider;
    private readonly CodeBasedAuthProvider _codeBasedAuthProvider;
    public AuthController(
        SynapticServerSettings settings,
        ISecurityTokenHandler tokenHandler,
        IRefreshTokenHandler refreshTokenHandler,
        RedirectUriProvider redirectUriProvider,
        CodeBasedAuthProvider codeBasedAuthProvider)
    {
        _settings = settings;
        _tokenHandler = tokenHandler;
        _refreshTokenHandler = refreshTokenHandler;
        _redirectUriProvider = redirectUriProvider;
        _codeBasedAuthProvider = codeBasedAuthProvider;
    }

    [HttpPost("register")]
    [AllowAnonymous]
    public IActionResult Registration([FromBody] object? body)
    {
        var bodyJson = JsonSerializer.Serialize(body);
        Log.Information($"Registration request, body: {bodyJson}.");

        var response = new
        {
            redirect_uris = $"{_settings.ServerUrl}/authorize",
            client_id = _settings.GitHubOAuthSettings.ClientId
        };

        return StatusCode(201, response);
    }

    [HttpGet("login")]
    [AllowAnonymous]
    [ProducesResponseType(302, Description = "Redirects to a selection page for an OAuth provider.")]
    public IActionResult Login([FromBody] object? body, [FromQuery] Dictionary<string, string>? inputQuery)
    {
        return Authorize(body, inputQuery);
    }

    [HttpGet("authorize")]
    [AllowAnonymous]
    [ProducesResponseType(302, Description = "Redirects to a selection page for an OAuth provider.")]
    public IActionResult AuthLogin([FromBody] object? body, [FromQuery] Dictionary<string, string>? inputQuery)
    {
        return Authorize(body, inputQuery);
    }

    public IActionResult Authorize([FromBody] object? body, [FromQuery] Dictionary<string, string>? inputQuery)
    {
        string queryString = string.Empty;
        if (inputQuery != null)
        {
            // ReSharper disable once ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract
            queryString = string.Join("&", inputQuery.Where(k => k.Key != null && k.Value != null).Select(kvp => $"{Uri.EscapeDataString(kvp.Key)}={Uri.EscapeDataString(kvp.Value)}"));
            Log.Information($"[Authorization] Received authorize GET with query {queryString}.");
        }
        if (body != null)
        {
            Log.Information($"[Authorization] Received authorize GET with body {JsonSerializer.Serialize(body)}.");
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

        Log.Information($"[Authorization] OAuth callback received with state: {state}, redirecting to {redirectUri}.");
        return Redirect($"{redirectUri.Uri}?state={state}&code={code}");
    }

    [HttpPost("token")]
    [AllowAnonymous]
    [Produces("application/json")]
    [ProducesResponseType(200, Type = typeof(AccessTokenResult), Description = "Returned when the authentication was successful and a JWT token was generated.")]
    [ProducesResponseType(401, Description = "Returned when the user is not authorized to access the API.")]
    [ProducesResponseType(500, Description = "Returned when the authentication failed due to missing config entries.")]
    public async Task<IActionResult> Token([FromForm] string code,
        [FromForm(Name = "client_id")] string clientId,
        [FromForm(Name = "client_secret")] string clientSecret,
        [FromForm(Name = "redirect_uri")] string redirectUri,
        [FromForm(Name = "grant_type")] string grantType,
        [FromForm(Name = "code_verifier")] string codeVerifier,
        [FromForm(Name = "refresh_token")] string refreshToken = "")
    {
        if (string.IsNullOrEmpty(_settings.JwtIssuer))
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
            if (!_refreshTokenHandler.ValidateRefreshToken(refreshToken, out var claimsIdentity))
            {
                Log.Error("Invalid refresh token");
                return StatusCode(401, "Invalid refresh token");
            }

            if (!_refreshTokenHandler.ValidateRefreshTokenExpiry(refreshToken))
            {
                Log.Error("Expired refresh token");
                return StatusCode(401, "The refresh token expired");
            }

            _refreshTokenHandler.InvalidateRefreshToken(refreshToken);
            return Ok(_tokenHandler.GenerateJwtToken(_settings.JwtKey, _settings.JwtIssuer, _settings.JwtTokenLifetime, claimsIdentity));

        }
        if (!_codeBasedAuthProvider.GetIdentityProviderByCode(code, out string? provider))
        {
            if (!redirectUri.Contains("localhost"))
            {
                Log.Error($"[Authentication Token] No provider found for code: {code}");
                return StatusCode(401, "Invalid code");
            }
            provider = "github";
        }

        clientId = provider switch
        {
            "github" => _settings.GitHubOAuthSettings.ClientId,
            "google" => _settings.GoogleOAuthSettings.ClientId,
            "microsoft" => _settings.MicrosoftOAuthSettings.ClientId,
            _ => clientId
        };
        clientSecret = provider switch
        {
            "github" => _settings.GitHubOAuthSettings.ClientSecret,
            "google" => _settings.GoogleOAuthSettings.ClientSecret,
            "microsoft" => _settings.MicrosoftOAuthSettings.ClientSecret,
            _ => clientSecret
        };

        VerificationResult validationResult = provider switch
        {
            "google" => await _tokenHandler.VerifyGoogleAuthentication(_settings, clientId, clientSecret, code, $"{_settings.ServerUrl}/oauth-callback", grantType,
                codeVerifier),
            "microsoft" => await _tokenHandler.VerifyMicrosoftAuthentication(_settings, clientId, clientSecret, code, $"{_settings.ServerUrl}/oauth-callback", grantType,
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
        AccessTokenResult token = _tokenHandler.GenerateJwtToken(_settings.JwtKey, _settings.JwtIssuer, _settings.JwtTokenLifetime, identity);
        return Ok(token);
    }
}
