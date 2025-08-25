using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Synaptic.NET.Domain;

namespace Synaptic.NET.Authentication.Controllers;

public class AccountController : Controller
{
    private readonly SynapticServerSettings _configuration;

    public AccountController(SynapticServerSettings synapticServerSettings)
    {
        _configuration = synapticServerSettings;
    }

    [HttpGet("/account/login")]
    [AllowAnonymous]
    public IActionResult Login(string? returnUrl = null)
    {
        if (!string.IsNullOrEmpty(returnUrl))
        {
            TempData["ReturnUrl"] = returnUrl;
        }

        var queryParams = new Dictionary<string, string>
        {
            ["redirect_uri"] = $"{_configuration.ServerUrl}/account/callback"
        };

        var queryString = string.Join("&", queryParams.Select(kvp =>
            $"{Uri.EscapeDataString(kvp.Key)}={Uri.EscapeDataString(kvp.Value)}"));

        return Redirect($"/oauth-select?{queryString}");
    }

    [HttpGet("/account/callback")]
    [AllowAnonymous]
    public async Task<IActionResult> Callback([FromQuery] string code, [FromQuery] string state)
    {
        if (string.IsNullOrEmpty(code))
        {
            return BadRequest("Missing authorization code");
        }

        try
        {
            var tokenRequest = new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string, string>("grant_type", "authorization_code"),
                new KeyValuePair<string, string>("code", code),
                new KeyValuePair<string, string>("client_id", GetClientIdFromState(state)),
                new KeyValuePair<string, string>("client_secret", GetClientSecretFromState(state)),
                new KeyValuePair<string, string>("redirect_uri", $"{_configuration.ServerUrl}/oauth-callback")
            });

            using var httpClient = new HttpClient();
            var tokenResponse = await httpClient.PostAsync($"{_configuration.ServerUrl}/token", tokenRequest);

            if (!tokenResponse.IsSuccessStatusCode)
            {
                Log.Error($"[Web Authentication] Token exchange failed with status {tokenResponse.StatusCode}");
                return Redirect("/account/login?error=auth_failed");
            }

            var tokenJson = await tokenResponse.Content.ReadAsStringAsync();
            var tokenData = JsonSerializer.Deserialize<JsonElement>(tokenJson);

            if (!tokenData.TryGetProperty("access_token", out var accessTokenElement))
            {
                Log.Error("[Web Authentication] Access token not found in response");
                return Redirect("/account/login?error=token_missing");
            }

            var accessToken = accessTokenElement.GetString();
            if (string.IsNullOrEmpty(accessToken))
            {
                Log.Error("[Web Authentication] Empty access token received");
                return Redirect("/account/login?error=empty_token");
            }

            var handler = new System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler();
            var jsonToken = handler.ReadJwtToken(accessToken);

            var claims = new List<Claim>();
            claims.Add(new Claim(ClaimTypes.NameIdentifier,
                jsonToken.Claims.FirstOrDefault(x => x.Type == "nameid")?.Value ?? ""));
            claims.Add(new Claim(ClaimTypes.Name,
                jsonToken.Claims.FirstOrDefault(x => x.Type == "unique_name")?.Value ?? ""));
            claims.Add(new Claim("Name",
                jsonToken.Claims.FirstOrDefault(x => x.Type == "unique_name")?.Value ?? ""));

            var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            var principal = new ClaimsPrincipal(identity);
            Log.Logger.Information("[Web Authentication] Authentication processed. Logging in {identity}.", identity.Name);
            await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal);

            var returnUrl = TempData["ReturnUrl"] as string ?? "/management";
            return LocalRedirect(returnUrl);
        }
        catch (Exception ex)
        {
            Log.Error($"[Web Authentication] Authentication callback error: {ex.Message}");
            return Redirect("/account/login?error=callback_failed");
        }
    }

    [HttpGet("/account/logout")]
    [AllowAnonymous]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return LocalRedirect("/home");
    }

    private string GetClientIdFromState(string? state)
    {
        if (string.IsNullOrEmpty(state) ||
            !AuthController.StateRedirectUris.Remove(state, out var redirectSettings))
        {
            return _configuration.GitHubOAuthSettings.ClientId;
        }

        return redirectSettings.Provider switch
        {
            "github" => _configuration.GitHubOAuthSettings.ClientId,
            "google" => _configuration.GoogleOAuthSettings.ClientId,
            "microsoft" => _configuration.MicrosoftOAuthSettings.ClientId,
            _ => _configuration.GitHubOAuthSettings.ClientId
        };
    }

    private string GetClientSecretFromState(string? state)
    {
        if (string.IsNullOrEmpty(state) ||
            !AuthController.StateRedirectUris.Remove(state, out var redirectSettings))
        {
            return _configuration.GitHubOAuthSettings.ClientSecret;
        }

        return redirectSettings.Provider switch
        {
            "github" => _configuration.GitHubOAuthSettings.ClientSecret,
            "google" => _configuration.GoogleOAuthSettings.ClientSecret,
            "microsoft" => _configuration.MicrosoftOAuthSettings.ClientSecret,
            _ => _configuration.GitHubOAuthSettings.ClientSecret
        };
    }
}
