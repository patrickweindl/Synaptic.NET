using System.Security.Claims;
using Synaptic.NET.Domain.Resources.Management;

namespace Synaptic.NET.Authentication;

public interface IRefreshTokenHandler
{
    Task<RefreshToken> GenerateRefreshTokenAsync(string jwtSecret, string jwtIssuer, ClaimsIdentity? claimsIdentity, TimeSpan lifetime);

    /// <summary>
    /// Validates a refresh token and returns the associated claims identity if valid.
    /// </summary>
    /// <param name="refreshToken">The refresh token instance to validate <see cref="RefreshToken"/>.</param>
    /// <returns>The claims identity associated with the refresh token if valid, otherwise null.</returns>
    Task<ClaimsIdentity?> ValidateRefreshTokenAsync(string refreshToken);

    /// <summary>
    /// Validates a refresh token expiry.
    /// </summary>
    /// <param name="refreshToken">The refresh token value.</param>
    /// <returns>False if the refresh token has expired or is not found, otherwise true.</returns>
    Task<bool> ValidateRefreshTokenExpiryAsync(string refreshToken);

    Task InvalidateRefreshTokenAsync(string refreshToken);
}
