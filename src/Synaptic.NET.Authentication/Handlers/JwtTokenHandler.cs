using System.Security.Claims;
using System.Text;
using System.Text.Json;
using Microsoft.IdentityModel.Tokens;
using Synaptic.NET.Authentication.Resources;
using Synaptic.NET.Domain.Resources.Configuration;
using AccessTokenResult = Synaptic.NET.Authentication.Resources.AccessTokenResult;
using TokenHandler = System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler;

namespace Synaptic.NET.Authentication.Handlers;

public class JwtTokenHandler : ISecurityTokenHandler
{
    private readonly IRefreshTokenHandler _refreshTokenHandler;
    private readonly HttpClient _authenticationClient;

    public JwtTokenHandler(IRefreshTokenHandler refreshTokenHandler)
    {
        _authenticationClient = new HttpClient();
        _refreshTokenHandler = refreshTokenHandler;
    }

    public async Task<VerificationResult> VerifyMicrosoftAuthentication(SynapticServerSettings settings, string clientId, string clientSecret,
        string code, string redirectUri, string grantType = "", string codeVerifier = "")
    {
        HttpRequestMessage request = new(HttpMethod.Post, "https://login.microsoftonline.com/consumers/oauth2/v2.0/token");
        Dictionary<string, string> parameters = new()
        {
            { "client_id", clientId },
            { "client_secret", clientSecret },
            { "code", code },
            { "redirect_uri", redirectUri }
        };

        if (!string.IsNullOrEmpty(grantType))
        {
            parameters.Add("grant_type", grantType);
        }

        if (!string.IsNullOrEmpty(codeVerifier))
        {
            parameters.Add("code_verifier", codeVerifier);
        }

        request.Content = new FormUrlEncodedContent(parameters);
        request.Headers.Add("Accept", "application/json");

        HttpResponseMessage response = await _authenticationClient.SendAsync(request);
        JsonDocument payload = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        string? accessToken = payload.RootElement.GetProperty("access_token").GetString();

        HttpRequestMessage userRequest = new(HttpMethod.Get, "https://graph.microsoft.com/v1.0/me");
        userRequest.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);
        userRequest.Headers.UserAgent.ParseAdd("synaptic-auth");

        HttpResponseMessage userResponse = await _authenticationClient.SendAsync(userRequest);
        JsonDocument userJson = JsonDocument.Parse(await userResponse.Content.ReadAsStringAsync());
        string? msDisplayName = userJson.RootElement.GetProperty("displayName").GetString();
        if (msDisplayName == null)
        {
            Log.Error("[Authentication] Missing MS login in json response.");
            return new VerificationResult { Success = false, UserName = string.Empty, UserId = string.Empty };
        }

        string? msUserId = userJson.RootElement.GetProperty("id").GetString();
        if (msUserId == null)
        {
            Log.Error("[Authentication] Missing Microsoft user id in json response.");
            return new VerificationResult { Success = false, UserName = string.Empty, UserId = string.Empty };
        }
        Log.Information($"[Authentication] Successfully authenticated user {msDisplayName} with id {msUserId}.");
        return new VerificationResult { Success = true, UserName = msDisplayName, UserId = msUserId };
    }

    public async Task<VerificationResult> VerifyGoogleAuthentication(SynapticServerSettings settings, string clientId, string clientSecret,
        string code, string redirectUri, string grantType = "", string codeVerifier = "")
    {
        HttpRequestMessage request = new(HttpMethod.Post, "https://oauth2.googleapis.com/token");
        Dictionary<string, string> parameters = new()
        {
            { "client_id", clientId },
            { "client_secret", clientSecret },
            { "code", code },
            { "redirect_uri", redirectUri }
        };

        if (!string.IsNullOrEmpty(grantType))
        {
            parameters.Add("grant_type", grantType);
        }

        if (!string.IsNullOrEmpty(codeVerifier))
        {
            parameters.Add("code_verifier", codeVerifier);
        }

        request.Content = new FormUrlEncodedContent(parameters);
        request.Headers.Add("Accept", "application/json");

        HttpResponseMessage response = await _authenticationClient.SendAsync(request);
        JsonDocument payload = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        string? accessToken = payload.RootElement.GetProperty("access_token").GetString();

        HttpRequestMessage userRequest = new(HttpMethod.Get, "https://www.googleapis.com/oauth2/v1/userinfo?alt=json");
        userRequest.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);
        userRequest.Headers.UserAgent.ParseAdd("synaptic-auth");

        HttpResponseMessage userResponse = await _authenticationClient.SendAsync(userRequest);
        JsonDocument userJson = JsonDocument.Parse(await userResponse.Content.ReadAsStringAsync());
        string? googleDisplayName = userJson.RootElement.GetProperty("name").GetString();
        if (googleDisplayName == null)
        {
            Log.Error("[Authentication] Missing Google display name in json response.");
            return new VerificationResult { Success = false, UserName = string.Empty, UserId = string.Empty };
        }

        string? googleUserId = userJson.RootElement.GetProperty("id").GetString();
        if (googleUserId == null)
        {
            Log.Error("[Authentication] Missing Google user id in json response.");
            return new VerificationResult { Success = false, UserName = string.Empty, UserId = string.Empty };
        }
        Log.Information($"[Authentication] Successfully authenticated user {googleDisplayName} with id {googleUserId}.");
        return new VerificationResult { Success = true, UserName = googleDisplayName, UserId = googleUserId };
    }

    public async Task<VerificationResult> VerifyGitHubAuthentication(SynapticServerSettings settings, string clientId, string clientSecret,
        string code, string grantType = "", string codeVerifier = "")
    {
        HttpRequestMessage request = new(HttpMethod.Post, "https://github.com/login/oauth/access_token");
        Dictionary<string, string> parameters = new()
        {
            { "client_id", clientId },
            { "client_secret", clientSecret },
            { "code", code }
        };

        if (!string.IsNullOrEmpty(grantType))
        {
            parameters.Add("grant_type", grantType);
        }

        if (!string.IsNullOrEmpty(codeVerifier))
        {
            parameters.Add("code_verifier", codeVerifier);
        }

        request.Content = new FormUrlEncodedContent(parameters);
        request.Headers.Add("Accept", "application/json");

        HttpResponseMessage response = await _authenticationClient.SendAsync(request);
        JsonDocument payload = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        string? accessToken = payload.RootElement.GetProperty("access_token").GetString();

        HttpRequestMessage userRequest = new(HttpMethod.Get, "https://api.github.com/user");
        userRequest.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);
        userRequest.Headers.UserAgent.ParseAdd("synaptic-auth");

        HttpResponseMessage userResponse = await _authenticationClient.SendAsync(userRequest);
        JsonDocument userJson = JsonDocument.Parse(await userResponse.Content.ReadAsStringAsync());
        string? githubLogin = userJson.RootElement.GetProperty("login").GetString();
        if (githubLogin == null)
        {
            Log.Error("[Authentication] Missing GitHub login in json response.");
            return new VerificationResult { Success = false, UserName = string.Empty, UserId = string.Empty };
        }

        string githubUserName = userJson.RootElement.GetProperty("id").GetInt64().ToString();
        Log.Information($"[Authentication] Successfully authenticated user {githubLogin} with id {githubUserName}.");
        return new VerificationResult { Success = true, UserName = githubLogin, UserId = githubUserName };
    }

    public AccessTokenResult GenerateJwtToken(string jwtSecret, string jwtIssuer,
        TimeSpan lifetime, ClaimsIdentity? claimsIdentity)
    {
        TokenHandler tokenHandler = new();
        byte[] key = Encoding.UTF8.GetBytes(jwtSecret);

        SecurityTokenDescriptor tokenDescriptor = new()
        {
            Subject = claimsIdentity,
            Expires = DateTime.UtcNow.Add(lifetime),
            Issuer = jwtIssuer,
            SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature),
            IssuedAt = DateTime.UtcNow
        };

        SecurityToken? jwt = tokenHandler.CreateToken(tokenDescriptor);
        string? jwtString = tokenHandler.WriteToken(jwt);

        string refreshToken = _refreshTokenHandler.GenerateRefreshToken(jwtSecret, jwtIssuer, claimsIdentity, TimeSpan.FromDays(14));

        return new AccessTokenResult
        {
            AccessToken = jwtString,
            RefreshToken = refreshToken,
            TokenType = "Bearer",
            ExpiresIn = (tokenDescriptor.Expires.Value.ToUniversalTime() - DateTime.UtcNow).Seconds
        };
    }
}
