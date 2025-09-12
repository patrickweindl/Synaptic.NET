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
        _dbContext.Database.EnsureCreated();
        _ = Task.Run(async () => await _dbContext.SetCurrentUserAsync(_currentUserService));

        Guid otherGuid = Guid.Parse("00000000-0000-0000-0000-000000000001");
        string otherUser = "otherUserId";
        string otherUserName = "Other User";
        _otherCurrentUserService = new MockUserService(otherGuid, otherUser, otherUserName);
        _otherDbContext = new SynapticDbContextFactory().CreateInMemoryDbContext();
        _otherDbContext.Database.EnsureCreated();
        _ = Task.Run(async () => await _otherDbContext.SetCurrentUserAsync(_otherCurrentUserService));
    }

    [Fact]
    public async Task ShouldNotLeakContext()
    {

        var firstUser = await _currentUserService.GetCurrentUserAsync();
        _dbContext.Attach(firstUser);
        _dbContext.MemoryStores.Add(new MemoryStore
        {
            Title = "Test Store",
            Description = "Test Store for leakage",
            StoreId = Guid.NewGuid(),
            OwnerUser = firstUser,
            UserId = firstUser.Id
        });
        await _dbContext.SaveChangesAsync();

        Assert.False(_otherDbContext.MemoryStores.Any());
    }

    [Fact]
    public async Task ShouldSaveApiKeys()
    {
        var currentUser = await _currentUserService.GetCurrentUserAsync();
        _dbContext.Attach(currentUser);
        (await _dbContext.DbUserAsync())?.ApiKeys.Add(new ApiKey()
        {
            Id = Guid.NewGuid(),
            Name ="Unit Test Key",
            Key = "TestKey",
            UserId = currentUser.Id,
            Owner = currentUser
        });

        await _dbContext.SaveChangesAsync();

        Assert.NotEqual(0, (await _dbContext.DbUserAsync())?.ApiKeys.Count);
    }

    [Fact]
    public async Task ShouldSaveApiKeysViaContext()
    {
        var currentUser = await _currentUserService.GetCurrentUserAsync();
        _dbContext.ApiKeys.Add(new ApiKey()
        {
            Id = Guid.NewGuid(),
            Name = "Unit Test Key2",
            Key = "TestKey2",
            UserId = currentUser.Id,
            Owner = currentUser
        });

        await _dbContext.SaveChangesAsync();


        Assert.True((await _dbContext.ApiKeys.FirstOrDefaultAsync(k => k.Name == "Unit Test Key2")) != null);
        Assert.NotNull((await _dbContext.ApiKeys.Include(apiKey => apiKey.Owner).FirstOrDefaultAsync(k => k.Name == "Unit Test Key2"))?.Owner);
    }

    [Fact]
    public async Task ShouldAddMemoryStore()
    {
        var currentUser = await _currentUserService.GetCurrentUserAsync();
        _dbContext.Attach(currentUser);
        _dbContext.MemoryStores.Add(new MemoryStore()
        {
            Title = "Test Store",
            Description = "Test Store",
            StoreId = Guid.NewGuid(),
            UserId = currentUser.Id
        });
        await _dbContext.SaveChangesAsync();

        Assert.NotEqual(0, _dbContext.MemoryStores.Count());
    }

    [Fact]
    public async Task ShouldAddMemoryToMemoryStore()
    {
        var currentUser = await _currentUserService.GetCurrentUserAsync();
        _dbContext.Attach(currentUser);
        _dbContext.MemoryStores.Add(new MemoryStore()
        {
            Title = "Test Store",
            Description = "Test Store",
            StoreId = Guid.NewGuid(),
            UserId = currentUser.Id
        });
        await _dbContext.SaveChangesAsync();

        _dbContext.Memories.Add(new Memory
        {
            Content = "Test Content",
            Title = "Test Title",
            Description = "Test Description",
            Identifier = Guid.NewGuid(),
            Owner = currentUser.Id,
            StoreId = _dbContext.MemoryStores.FirstOrDefault(s => s.Title == "Test Store")?.StoreId ?? Guid.NewGuid(),
            UpdatedAt = DateTimeOffset.UtcNow,
            CreatedAt = DateTimeOffset.UtcNow
        });

        await _dbContext.SaveChangesAsync();

        Assert.NotEqual(0, _dbContext.MemoryStores.Include(memoryStore => memoryStore.Memories).FirstOrDefault(s => s.Title == "Test Store")?.Memories.Count());
        Assert.NotEqual(0, _dbContext.Memories.Count());

        var memories = _dbContext.Memories.Include(memory => memory.OwnerUser).ToList();
        Assert.NotNull(memories.FirstOrDefault()?.OwnerUser ?? null);
    }
}
