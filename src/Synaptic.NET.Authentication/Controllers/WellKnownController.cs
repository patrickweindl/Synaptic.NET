using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Synaptic.NET.Domain;

namespace Synaptic.NET.Authentication.Controllers;

public class WellKnownController : ControllerBase
{
    private readonly SynapticServerSettings _settings;
    public WellKnownController(SynapticServerSettings settings)
    {
        _settings = settings;
    }

    [HttpGet("/.well-known/microsoft-identity-association.json")]
    [AllowAnonymous]
    public IActionResult GetMicrosoftIdentityAssociation()
    {
        if (!_settings.MicrosoftOAuthSettings.Enabled)
        {
            return NotFound();
        }

        var response = new
        {
            associatedApplications = new List<object>()
        };

        if (_settings.MicrosoftOAuthSettings.Enabled)
        {
            response.associatedApplications.Add(new { applicationId = _settings.MicrosoftOAuthSettings.ClientId });
        }

        if (_settings.GitHubOAuthSettings.Enabled)
        {
            response.associatedApplications.Add(new { applicationId = _settings.GitHubOAuthSettings.ClientId });
        }

        if (_settings.GoogleOAuthSettings.Enabled)
        {
            response.associatedApplications.Add(new { applicationId = _settings.GoogleOAuthSettings.ClientId });
        }

        return Ok(response);
    }

    [HttpGet("/.well-known/oauth-protected-resource")]
    [AllowAnonymous]
    public IActionResult GetProtectedResource()
    {
        var resource = new
        {
            resource = _settings.JwtIssuer,
            authorization_servers = new[]
            {
                $"{_settings.JwtIssuer}/authorize", $"{_settings.JwtIssuer}", $"{_settings.JwtIssuer}/token",
            },
            bearer_methods_supported = new[] { "header" },
            resource_name = "mneme API"
        };
        return Ok(resource);
    }

    [HttpGet("/.well-known/openid-configuration")]
    [AllowAnonymous]
    public IActionResult GetOpenIdConfiguration()
    {
        var response = new
        {
            issuer = _settings.JwtIssuer,
            authorization_endpoint = $"{_settings.ServerUrl}/authorize",
            token_endpoint = $"{_settings.ServerUrl}/token",
            registration_endpoint = $"{_settings.ServerUrl}/register"
        };
        return Ok(response);
    }


    [HttpGet("/.well-known/oauth-authorization-server")]
    [AllowAnonymous]
    public IActionResult Get()
    {
        var response = new
        {
            issuer = _settings.JwtIssuer,
            authorization_endpoint = $"{_settings.ServerUrl}/authorize",
            token_endpoint = $"{_settings.ServerUrl}/token",
            registration_endpoint = $"{_settings.ServerUrl}/register"
        };
        return Ok(response);
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
}
