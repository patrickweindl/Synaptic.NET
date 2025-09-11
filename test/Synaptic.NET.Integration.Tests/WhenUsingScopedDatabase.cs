using Synaptic.NET.Domain.Resources.Storage;

namespace Synaptic.NET.Integration.Tests;

public class WhenUsingScopedDatabase
{
    private readonly IntegrationTestBuilder _builder;
    public WhenUsingScopedDatabase()
    {
        _builder = new IntegrationTestBuilder();
    }

    [Fact]
    public async Task ShouldNotLeakUserInformation()
    {
        Skip.If(_builder.ShouldSkipIntegrationTest());

        var newMemory = new Memory
        {
            Title = "A test memory",
            Description = "A memory for integration testing",
            Content = "Test Content for a unit test that tests both Qdrant and EF storage.",
            StoreId = Guid.NewGuid(),
            CreatedAt = DateTimeOffset.UtcNow,
            Owner = _builder.CurrentUserService.GetCurrentUser().Id,
            OwnerUser = _builder.CurrentUserService.GetCurrentUser()
        };

        await _builder.MemoryProvider.CreateMemoryEntryAsync(newMemory);

        Assert.True(_builder.DbContext.MemoryStores.ToList().Count > 0);
        Assert.True(_builder.DbContext.MemoryStores.SelectMany(s => s.Memories).ToList().Count > 0);
        Assert.True(_builder.DbContext.Memories.ToList().Count > 0);

        var searchResult = await _builder.MemoryProvider.SearchAsync("Test", 10, -1);
        var contextSearchResults = await searchResult.Results.ToListAsync();
        Assert.True(contextSearchResults.Any());

        Guid otherTestGuid = Guid.Parse("00000001-0000-abcd-0000-000000000000");
        string otherUser = "otherUserId";
        string otherUserName = "Other User";
        var otherBuilder = new IntegrationTestBuilder(otherTestGuid, otherUserName, otherUser);

        var otherSearchResult = await otherBuilder.MemoryProvider.SearchAsync("Test", 10, -1);
        var otherContextSearchResults = await otherSearchResult.Results.ToListAsync();
        Assert.False(otherContextSearchResults.Any());

        Assert.False(otherBuilder.DbContext.MemoryStores.Any());
        Assert.False(otherBuilder.DbContext.Memories.Any());
    }
}
