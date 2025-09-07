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
    private readonly ICurrentUserService _currentUserService = new MockUserService();
    private readonly IMemoryProvider _memoryProvider;
    private readonly TestSettings _testSettings;
    private readonly SynapticDbContext _dbContext;
    private readonly QdrantMemoryClient _qdrantMemoryClient;

    public WhenUsingHybridMemoryProvider()
    {
        _testSettings = TestSettings.FromFile();
        _dbContext = new SynapticDbContextFactory().CreateInMemoryDbContext();
        _dbContext.Database.EnsureCreated();
        OpenAiClientFactory factory = new(_testSettings.OpenAiApiKey);
        IMetricsCollectorProvider testMetricsCollectorProvider = new MetricsCollectorProvider();
        IMemoryAugmentationService memoryAugmentationService = new MemoryAugmentationService(_testSettings, factory, _currentUserService, testMetricsCollectorProvider);
        IMemoryStoreRouter storeRouter = new WeightedMemoryStoreRouter(_currentUserService, testMetricsCollectorProvider, factory, _testSettings);
        IMemoryQueryResultReranker reranker = new MemoryQueryResultReranker(factory, _testSettings);
        _qdrantMemoryClient = new QdrantMemoryClient(_testSettings, memoryAugmentationService);

        _memoryProvider = new HybridMemoryProvider(_currentUserService, _dbContext, _qdrantMemoryClient, storeRouter, memoryAugmentationService, reranker);
    }

    [Fact]
    public async Task ShouldCreateMemories()
    {
        Skip.If(string.IsNullOrEmpty(_testSettings.OpenAiApiKey));

        var newMemory = new Memory
        {
            Title = "A test memory",
            Description = "A memory for integration testing",
            Content = "Test Content for a unit test that tests both Qdrant and EF storage.",
            StoreId = Guid.NewGuid(),
            CreatedAt = DateTimeOffset.UtcNow,
            Owner = _currentUserService.GetCurrentUser().Id
        };

        await _memoryProvider.CreateMemoryEntryAsync(newMemory);

        Assert.True(_dbContext.MemoryStores.ToList().Count > 0);
        Assert.True(_dbContext.Memories.ToList().Count > 0);

        var searchResult = await _memoryProvider.SearchAsync("Test", 10, -1);
        Assert.True(searchResult.Any());
    }
}
