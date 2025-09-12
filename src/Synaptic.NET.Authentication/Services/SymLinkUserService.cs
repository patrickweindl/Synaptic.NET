using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using Synaptic.NET.Domain.Abstractions.Management;
using Synaptic.NET.Domain.Helpers;
using Synaptic.NET.Domain.Resources;
using Synaptic.NET.Domain.Resources.Management;

namespace Synaptic.NET.Authentication.Services;

public class SymLinkUserService : ISymLinkUserService
{
    private readonly IDbContextFactory<SynapticDbContext> _dbContextFactory;
    public SymLinkUserService(IDbContextFactory<SynapticDbContext> dbContextFactory)
    {
        _dbContextFactory = dbContextFactory;
    }

    private async Task AddSymLinkAsync(string mainUserIdentifier, string userIdentifier)
    {
        await using var dbContext = await _dbContextFactory.CreateDbContextAsync();
        var user = await dbContext.Users.FirstOrDefaultAsync(u => u.Identifier == mainUserIdentifier);
        if (user == null)
        {
            return;
        }
        var symLinkUser = await dbContext.Users.FirstOrDefaultAsync(u => u.Identifier == userIdentifier);
        if (symLinkUser == null)
        {
            return;
        }

        user.SymLinkUserIds.Add(new SymLinkUser { Id = Guid.NewGuid(), UserId = user.Id, SymLinkUserId = symLinkUser.Id });
        await dbContext.SaveChangesAsync();
    }

    private async Task RemoveSymLinkAsync(string mainUserIdentifier, string userIdentifier)
    {
        await using var dbContext = await _dbContextFactory.CreateDbContextAsync();
        var user = await dbContext.Users.FirstOrDefaultAsync(u => u.Identifier == mainUserIdentifier);
        if (user == null)
        {
            return;
        }
        var symLinkUser = await dbContext.Users.FirstOrDefaultAsync(u => u.Identifier == userIdentifier);
        if (symLinkUser == null)
        {
            return;
        }
        var symLinkUserId = user.SymLinkUserIds.FirstOrDefault(u => u.SymLinkUserId == symLinkUser.Id);
        if (symLinkUserId == null)
        {
            return;
        }
        user.SymLinkUserIds.Remove(symLinkUserId);
        await dbContext.SaveChangesAsync();
    }

    public async Task<List<ClaimsIdentity>> GetSymLinkUsersAsync(ClaimsIdentity claimsIdentity)
    {
        string storageId = $"{claimsIdentity.ToUserIdentifier()}";
        await using var dbContext = await _dbContextFactory.CreateDbContextAsync();
        var users = await dbContext.Users.Include(u => u.SymLinkUserIds).ToListAsync();
        var symLinkUser = users.FirstOrDefault(u => u.Identifier == storageId);
        if (symLinkUser == null)
        {
            return [claimsIdentity];
        }

        var symLinkUsers = users.Where(u => symLinkUser.SymLinkUserIds.Any(s => s.SymLinkUserId == u.Id)).ToList();
        var symLinkUserClaimsIdentities = symLinkUsers.Select(u => ClaimsHelper.ClaimsIdentityFromUserNameAndId(u.UserName, u.Identifier)).ToList();
        symLinkUserClaimsIdentities.Add(claimsIdentity);
        return symLinkUserClaimsIdentities;
    }

    public async Task<ClaimsIdentity> GetMainIdentityAsync(ClaimsIdentity claimsIdentity)
    {
        string storageId = $"{claimsIdentity.ToUserIdentifier()}";
        await using var dbContext = await _dbContextFactory.CreateDbContextAsync();
        var requestUser = await dbContext.Users.FirstOrDefaultAsync(u => u.Identifier == storageId);
        if (requestUser != null)
        {
            var mainUser = await dbContext.Users.FirstOrDefaultAsync(u => u.SymLinkUserIds.Any(s => s.SymLinkUserId == requestUser.Id));
            if (mainUser != null)
            {
                return mainUser.Identifier.ToClaimsIdentity();
            }
        }
        return claimsIdentity;
    }

    public async Task AddSymLinkAsync(ClaimsIdentity mainIdentity, ClaimsIdentity subIdentity)
    {
        string mainUserIdentifier = mainIdentity.ToUserIdentifier();
        string subUserIdentifier = subIdentity.ToUserIdentifier();
        await AddSymLinkAsync(mainUserIdentifier, subUserIdentifier);
    }

    public async Task RemoveSymLinkAsync(ClaimsIdentity mainIdentity, ClaimsIdentity subIdentity)
    {
        string mainUserIdentifier = mainIdentity.ToUserIdentifier();
        string subUserIdentifier = subIdentity.ToUserIdentifier();
        await RemoveSymLinkAsync(mainUserIdentifier, subUserIdentifier);
    }
}
