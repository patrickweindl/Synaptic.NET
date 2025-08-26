using System.Diagnostics.CodeAnalysis;
using System.Security.Claims;

namespace Synaptic.NET.Authentication;

public interface IRefreshTokenHandler
{
    string GenerateRefreshToken(string jwtSecret, string jwtIssuer, ClaimsIdentity? claimsIdentity, TimeSpan lifetime);

    bool ValidateRefreshToken(string refreshToken, [MaybeNullWhen(false)] out ClaimsIdentity? claimsIdentity);

    bool ValidateRefreshTokenExpiry(string refreshToken);

    void InvalidateRefreshToken(string refreshToken);
}
