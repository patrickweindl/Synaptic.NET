using Microsoft.EntityFrameworkCore;
using Synaptic.NET.Core.Tests.Mocks;
using Synaptic.NET.Domain.Abstractions.Management;
using Synaptic.NET.Domain.Resources;
using Synaptic.NET.Domain.Resources.Management;
using Synaptic.NET.Domain.Resources.Storage;

namespace Synaptic.NET.Core.Tests;

public class WhenUsingDatabaseContext
{
    private readonly ICurrentUserService _currentUserService = new MockUserService();
    private readonly SynapticDbContext _dbContext;
    private readonly ICurrentUserService _otherCurrentUserService;
    private readonly SynapticDbContext _otherDbContext;

    public WhenUsingDatabaseContext()
    {

        _dbContext = new SynapticDbContextFactory().CreateInMemoryDbContext();
        Guid otherGuid = Guid.Parse("00000000-0000-0000-0000-000000000001");
        string otherUser = "otherUserId";
        string otherUserName = "Other User";
        _otherCurrentUserService = new MockUserService(otherGuid, otherUser, otherUserName);
        _otherDbContext = new SynapticDbContextFactory().CreateInMemoryDbContext(_otherCurrentUserService);
    }

    [Fact]
    public void ShouldNotLeakContext()
    {
        _dbContext.Attach(_currentUserService.GetCurrentUser());
        _dbContext.MemoryStores.Add(new MemoryStore
        {
            Title = "Test Store",
            Description = "Test Store for leakage",
            StoreId = Guid.NewGuid(),
            OwnerUser = _currentUserService.GetCurrentUser(),
            UserId = _currentUserService.GetCurrentUser().Id
        });
        _dbContext.SaveChanges();

        Assert.False(_otherDbContext.MemoryStores.Any());
    }

    [Fact]
    public void ShouldSaveApiKeys()
    {
        _dbContext.Attach(_currentUserService.GetCurrentUser());
        _dbContext.DbUser()?.ApiKeys.Add(new ApiKey()
        {
            Id = Guid.NewGuid(),
            Name ="Unit Test Key",
            Key = "TestKey",
            UserId = _currentUserService.GetCurrentUser().Id,
            Owner = _currentUserService.GetCurrentUser()
        });

        _dbContext.SaveChanges();

        Assert.NotEqual(0, _dbContext.DbUser()?.ApiKeys.Count);
    }

    [Fact]
    public void ShouldAddMemoryStore()
    {
        _dbContext.Attach(_currentUserService.GetCurrentUser());
        _dbContext.MemoryStores.Add(new MemoryStore()
        {
            Title = "Test Store",
            Description = "Test Store",
            StoreId = Guid.NewGuid(),
            UserId = _currentUserService.GetCurrentUser().Id
        });
        _dbContext.SaveChanges();

        Assert.NotEqual(0, _dbContext.MemoryStores.Count());
    }

    [Fact]
    public void ShouldAddMemoryToMemoryStore()
    {
        _dbContext.Attach(_currentUserService.GetCurrentUser());
        _dbContext.MemoryStores.Add(new MemoryStore()
        {
            Title = "Test Store",
            Description = "Test Store",
            StoreId = Guid.NewGuid(),
            UserId = _currentUserService.GetCurrentUser().Id
        });
        _dbContext.SaveChanges();

        _dbContext.Memories.Add(new Memory
        {
            Content = "Test Content",
            Title = "Test Title",
            Description = "Test Description",
            Identifier = Guid.NewGuid(),
            Owner = _currentUserService.GetCurrentUser().Id,
            StoreId = _dbContext.MemoryStores.FirstOrDefault(s => s.Title == "Test Store")?.StoreId ?? Guid.NewGuid(),
            UpdatedAt = DateTimeOffset.UnixEpoch,
            CreatedAt = DateTimeOffset.UtcNow
        });

        _dbContext.SaveChanges();

        Assert.NotEqual(0, _dbContext.MemoryStores.Include(memoryStore => memoryStore.Memories).FirstOrDefault(s => s.Title == "Test Store")?.Memories.Count());
        Assert.NotEqual(0, _dbContext.Memories.Count());

        var memories = _dbContext.Memories.ToList();
        Assert.NotNull(memories.FirstOrDefault()?.OwnerUser ?? null);
    }
}
