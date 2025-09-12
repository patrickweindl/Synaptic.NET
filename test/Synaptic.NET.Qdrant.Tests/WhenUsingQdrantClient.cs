using Synaptic.NET.Augmentation.Services;
using Synaptic.NET.Core;
using Synaptic.NET.Core.Providers;
using Synaptic.NET.Core.Tests;
using Synaptic.NET.Core.Tests.Mocks;
using Synaptic.NET.Domain.Abstractions.Augmentation;
using Synaptic.NET.Domain.Abstractions.Management;
using Synaptic.NET.Domain.Resources.Storage;
using Synaptic.NET.OpenAI;

namespace Synaptic.NET.Qdrant.Tests;

public class WhenUsingQdrantClient
{
    private readonly TestSettings _testSettings;
    private readonly ICurrentUserService _currentUserService = new MockUserService();
    private readonly IMemoryAugmentationService _memoryAugmentationService;
    private readonly IMetricsCollectorProvider _testMetricsCollectorProvider;

    public WhenUsingQdrantClient()
    {
        _testSettings = TestSettings.FromFile();
        OpenAiClientFactory factory = new(_testSettings.OpenAiSettings.ApiKey);
        _testMetricsCollectorProvider = new MetricsCollectorProvider();
        _memoryAugmentationService =
            new MemoryAugmentationService(_testSettings, factory, _currentUserService, _testMetricsCollectorProvider);
    }
    [Fact]
    public async Task ShouldAcceptAndRetrieveMemory()
    {
        Skip.If(string.IsNullOrEmpty(_testSettings.OpenAiSettings.ApiKey));
        var qdrantClient = new QdrantMemoryClient(_testSettings, _memoryAugmentationService);
        var currentUser = await _currentUserService.GetCurrentUserAsync();
        await qdrantClient.UpsertMemoryAsync(currentUser,
            new Memory
            {
                Title = "A test memory",
                Description = "A memory for testing",
                Content = "Test Content",
                StoreId = Guid.NewGuid(),
                CreatedAt = DateTimeOffset.UtcNow,
                Owner = currentUser.Id,
                OwnerUser = currentUser
            });

        var results = await qdrantClient.SearchAsync("Test", 10, -1, currentUser);
        Assert.True(results.Any());
    }
}
