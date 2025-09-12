using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Synaptic.NET.Domain.Helpers;
using Synaptic.NET.Domain.Resources;
using Synaptic.NET.Domain.Resources.Management;

namespace Synaptic.NET.Authentication.Handlers;

public class RefreshTokenHandler : IRefreshTokenHandler
{
    private readonly IDbContextFactory<SynapticDbContext> _dbContextFactory;

    public RefreshTokenHandler(IDbContextFactory<SynapticDbContext> dbContextFactory)
    {
        _dbContextFactory = dbContextFactory;
    }

    public async Task<RefreshToken> GenerateRefreshTokenAsync(string jwtSecret, string jwtIssuer, ClaimsIdentity? claimsIdentity, TimeSpan lifetime)
    {
        await CleanExpiredAndConsumedTokensAsync();

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

        var refreshTokenData = new RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = claimsIdentity.ToUserId(),
            UserName = claimsIdentity.ToUserName(),
            ExpiresAt = DateTime.UtcNow.Add(lifetime),
            Token = returnString,
            IsConsumed = false
        };

        await using var dbContext = await _dbContextFactory.CreateDbContextAsync();
        await dbContext.RefreshTokens.AddAsync(refreshTokenData);
        await dbContext.SaveChangesAsync();

        return refreshTokenData;
    }

    public async Task<ClaimsIdentity?> ValidateRefreshTokenAsync(string refreshToken)
    {
        await using var dbContext = await _dbContextFactory.CreateDbContextAsync();
        var token = await dbContext.RefreshTokens.FirstOrDefaultAsync(t => t.Token == refreshToken);

        if (token == null)
        {
            return null;
        }

        var claimsIdentity = new ClaimsIdentity();
        claimsIdentity.AddClaim(new Claim(ClaimTypes.NameIdentifier, token.UserId));
        claimsIdentity.AddClaim(new Claim(ClaimTypes.Name, token.UserName));
        return claimsIdentity;
    }

    public async Task<bool> ValidateRefreshTokenExpiryAsync(string refreshToken)
    {
        await using var dbContext = await _dbContextFactory.CreateDbContextAsync();
        var token = await dbContext.RefreshTokens.FirstOrDefaultAsync(t => t.Token == refreshToken);
        return token is { IsExpired: false };
    }

    public async Task InvalidateRefreshTokenAsync(string refreshToken)
    {
        await using var dbContext = await _dbContextFactory.CreateDbContextAsync();
        var token = await dbContext.RefreshTokens.FirstOrDefaultAsync(t => t.Token == refreshToken);
        if (token != null)
        {
            token.IsConsumed = true;
            await dbContext.SaveChangesAsync();
        }
    }

    private async Task CleanExpiredAndConsumedTokensAsync()
    {
        await using var dbContext = await _dbContextFactory.CreateDbContextAsync();
        var refreshTokens = await dbContext.RefreshTokens.ToListAsync();
        var invalidTokens = refreshTokens.Where(t => t.IsExpired || t.IsConsumed).ToList();
        foreach (var token in invalidTokens)
        {
            dbContext.RefreshTokens.Remove(token);
        }

        await dbContext.SaveChangesAsync();
    }
}
