using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Synaptic.NET.Domain.Resources.Configuration;

namespace Synaptic.NET.Authentication.Controllers;

public class WellKnownController : ControllerBase
{
    private readonly SynapticServerSettings _settings;
    public WellKnownController(SynapticServerSettings settings)
    {
        _settings = settings;
    }

    [HttpGet("/")]
    [AllowAnonymous]
    public IActionResult GetHead()
    {
        return LocalRedirect("/home");
    }

    [HttpGet("/.well-known/microsoft-identity-association.json")]
    [AllowAnonymous]
    public IActionResult GetMicrosoftIdentityAssociation()
    {
        if (!_settings.MicrosoftOAuthProviderSettings.Enabled)
        {
            return NotFound();
        }

        var response = new
        {
            associatedApplications = new List<object>()
        };

        if (_settings.MicrosoftOAuthProviderSettings.Enabled)
        {
            response.associatedApplications.Add(new { applicationId = _settings.MicrosoftOAuthProviderSettings.ClientId });
        }

        if (_settings.GitHubOAuthProviderSettings.Enabled)
        {
            response.associatedApplications.Add(new { applicationId = _settings.GitHubOAuthProviderSettings.ClientId });
        }

        if (_settings.GoogleOAuthProviderSettings.Enabled)
        {
            response.associatedApplications.Add(new { applicationId = _settings.GoogleOAuthProviderSettings.ClientId });
        }

        return Ok(response);
    }

    [HttpGet("/.well-known/oauth-protected-resource/mcp")]
    [AllowAnonymous]
    public IActionResult GetProtectedMcpResource()
    {
        return Ok(BuildProtectedResource());
    }

    [HttpGet("/.well-known/oauth-protected-resource")]
    [AllowAnonymous]
    public IActionResult GetProtectedResource()
    {
        return Ok(BuildProtectedResource());
    }

    [HttpGet("/.well-known/openid-configuration")]
    [AllowAnonymous]
    public IActionResult GetOpenIdConfiguration()
    {
        return Ok(BuildOAuthConfiguration());
    }

    [HttpGet("/.well-known/oauth-authorization-server")]
    [AllowAnonymous]
    public IActionResult Get()
    {
        return Ok(BuildOAuthConfiguration());
    }

    [HttpGet("/.well-known/oauth-authorization-server/authorize")]
    [AllowAnonymous]
    [ProducesResponseType(302, Description = "Redirects to a selection page for an OAuth provider.")]
    public IActionResult LoginWellKnown([FromBody] object? body, [FromQuery] Dictionary<string, string>? inputQuery)
    {
        string queryString = string.Empty;
        if (inputQuery != null)
        {
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

    private object BuildOAuthConfiguration()
    {
        return new
        {
            issuer = _settings.ServerSettings.JwtIssuer,
            authorization_endpoint = $"{_settings.ServerSettings.ServerUrl}/authorize",
            token_endpoint = $"{_settings.ServerSettings.ServerUrl}/token",
            registration_endpoint = $"{_settings.ServerSettings.ServerUrl}/register",
            response_types_supported = new[]
            {
                "code",
                "code token"
            },
            code_challenge_methods_supported = new[]
            {
                "S256", "plain"
            },
            grant_types_supported = new[]
            {
                "authorization_code"
            },
            token_endpoint_auth_signing_alg_values_supported = new[]
            {
                "HS256",
                "RS256"
            }
        };
    }

    private object BuildProtectedResource()
    {
        return new
        {
            resource = _settings.ServerSettings.JwtIssuer,
            authorization_servers = new[]
            {
                $"{_settings.ServerSettings.JwtIssuer}"
            },
            bearer_methods_supported = new[] { "header" },
            resource_name = _settings.ServerSettings.ServerUrl
        };
    }
}
