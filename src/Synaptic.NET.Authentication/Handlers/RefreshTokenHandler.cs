using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.IdentityModel.Tokens;
using Synaptic.NET.Domain.Helpers;

namespace Synaptic.NET.Authentication.Handlers;

public class RefreshTokenHandler : IRefreshTokenHandler
{
    private readonly string _filePath = Path.Join(AppContext.BaseDirectory, "data", "refresh_tokens.json");
    private static ConcurrentDictionary<string, RefreshTokenData> s_refreshTokens = new();
    private readonly Lock _fileLock = new();

    public RefreshTokenHandler()
    {
        if (!string.IsNullOrEmpty(Path.GetDirectoryName(_filePath)))
        {
            Directory.CreateDirectory(Path.GetDirectoryName(_filePath)!);
        }
        LoadFromFile();
        CleanExpiredTokens();
    }

    public string GenerateRefreshToken(string jwtSecret, string jwtIssuer, ClaimsIdentity? claimsIdentity, TimeSpan lifetime)
    {
        CleanExpiredTokens();

        JwtSecurityTokenHandler tokenHandler = new();
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

        string returnString = jwtString ?? Guid.NewGuid().ToString("N");

        var refreshTokenData = new RefreshTokenData
        {
            UserId = claimsIdentity.ToUserId(),
            UserName = claimsIdentity.ToUserName(),
            ExpiresAt = DateTime.UtcNow.Add(lifetime)
        };
        s_refreshTokens[returnString] = refreshTokenData;
        SaveToFile();
        return returnString;
    }

    public bool ValidateRefreshToken(string refreshToken, [MaybeNullWhen(false)] out ClaimsIdentity? claimsIdentity)
    {
        if (s_refreshTokens.TryRemove(refreshToken, out var tokenData))
        {
            claimsIdentity = new ClaimsIdentity();
            claimsIdentity.AddClaim(new Claim(ClaimTypes.NameIdentifier, tokenData.UserId));
            claimsIdentity.AddClaim(new Claim(ClaimTypes.Name, tokenData.UserName));
            return true;
        }

        claimsIdentity = null;
        return false;
    }

    public bool ValidateRefreshTokenExpiry(string refreshToken)
    {
        if (s_refreshTokens.TryGetValue(refreshToken, out var tokenData))
        {
            return tokenData.ExpiresAt > DateTime.UtcNow;
        }

        return false;
    }

    public void InvalidateRefreshToken(string refreshToken)
    {
        s_refreshTokens.TryRemove(refreshToken, out _);
        SaveToFile();
    }

    private void CleanExpiredTokens()
    {
        var expired = s_refreshTokens.Where(kv => kv.Value.ExpiresAt < DateTime.UtcNow).ToList();
        foreach (var kvp in expired)
        {
            s_refreshTokens.TryRemove(kvp);
        }
        SaveToFile();
    }

    private void SaveToFile()
    {
        lock (_fileLock)
        {
            File.WriteAllText(_filePath, JsonSerializer.Serialize(s_refreshTokens));
        }
    }
    private void LoadFromFile()
    {
        lock (_fileLock)
        {
            s_refreshTokens = File.Exists(_filePath)
                ? JsonSerializer.Deserialize<ConcurrentDictionary<string, RefreshTokenData>>(File.ReadAllText(_filePath)) ?? new()
                : new();
        }
    }


}

public class RefreshTokenData
{
    [JsonPropertyName("user_id")]
    public required string UserId { get; set; }
    [JsonPropertyName("user_name")]
    public required string UserName { get; set; }
    [JsonPropertyName("expires_at")]
    public DateTime ExpiresAt { get; set; }
}
