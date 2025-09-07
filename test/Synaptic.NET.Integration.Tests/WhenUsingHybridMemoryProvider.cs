using Synaptic.NET.Augmentation.Services;
using Synaptic.NET.Core;
using Synaptic.NET.Core.Providers;
using Synaptic.NET.Core.Services;
using Synaptic.NET.Core.Tests;
using Synaptic.NET.Core.Tests.Mocks;
using Synaptic.NET.Domain.Abstractions.Augmentation;
using Synaptic.NET.Domain.Abstractions.Management;
using Synaptic.NET.Domain.Abstractions.Storage;
using Synaptic.NET.Domain.Resources;
using Synaptic.NET.Domain.Resources.Storage;
using Synaptic.NET.OpenAI;
using Synaptic.NET.Qdrant;

namespace Synaptic.NET.Integration.Tests;

public class WhenUsingHybridMemoryProvider
{
    private IntegrationTestBuilder _builder;

    public WhenUsingHybridMemoryProvider()
    {
        _builder = new IntegrationTestBuilder();
    }

    [Fact]
    public async Task ShouldCreateMemories()
    {
        Skip.If(_builder.ShouldSkipIntegrationTest());

        var newMemory = new Memory
        {
            Title = "A test memory",
            Description = "A memory for integration testing",
            Content = "Test Content for a unit test that tests both Qdrant and EF storage.",
            StoreId = Guid.NewGuid(),
            CreatedAt = DateTimeOffset.UtcNow,
            Owner = _builder.CurrentUserService.GetCurrentUser().Id
        };

        await _builder.MemoryProvider.CreateMemoryEntryAsync(newMemory);

        Assert.True(_builder.DbContext.MemoryStores.ToList().Count > 0);
        Assert.True(_builder.DbContext.Memories.ToList().Count > 0);

        var searchResult = await _builder.MemoryProvider.SearchAsync("Test", 10, -1);
        Assert.True(searchResult.Any());
    }
}
