using Synaptic.NET.Augmentation.Services;
using Synaptic.NET.Core;
using Synaptic.NET.Core.Providers;
using Synaptic.NET.Core.Services;
using Synaptic.NET.Core.Tests;
using Synaptic.NET.Core.Tests.Mocks;
using Synaptic.NET.Domain;
using Synaptic.NET.Domain.Abstractions.Augmentation;
using Synaptic.NET.Domain.Abstractions.Management;
using Synaptic.NET.Domain.Abstractions.Storage;
using Synaptic.NET.Domain.Resources;
using Synaptic.NET.OpenAI;
using Synaptic.NET.Qdrant;

namespace Synaptic.NET.Integration.Tests;

public class IntegrationTestBuilder
{
    private readonly ICurrentUserService _currentUserService = new MockUserService();
    private readonly IMemoryProvider _memoryProvider;
    private readonly TestSettings _testSettings;
    private readonly SynapticDbContext _dbContext;
    private readonly QdrantMemoryClient _qdrantMemoryClient;
    private readonly IMemoryStoreRouter _storeRouter;

    public IntegrationTestBuilder(Guid? overrideGuid = null, string? overrideDisplayName = null, string? overrideUserId = null)
    {
        if (overrideGuid != null && overrideDisplayName != null && overrideUserId != null)
        {
            _currentUserService = new MockUserService(overrideGuid, overrideDisplayName, overrideUserId);
        }
        _testSettings = TestSettings.FromFile();
        _dbContext = new SynapticDbContextFactory().CreateInMemoryDbContext();
        _dbContext.Database.EnsureCreated();
        _ = Task.Run(async () => await _dbContext.SetCurrentUserAsync(_currentUserService));
        OpenAiClientFactory factory = new(_testSettings.OpenAiSettings.ApiKey);
        IMetricsCollectorProvider testMetricsCollectorProvider = new MetricsCollectorProvider(new InMemorySynapticDbContextFactory());
        IMemoryAugmentationService memoryAugmentationService = new MemoryAugmentationService(_testSettings, factory, _currentUserService, testMetricsCollectorProvider);
        _storeRouter = new WeightedMemoryStoreRouter(_currentUserService, testMetricsCollectorProvider, factory, _testSettings);
        IMemoryQueryResultReranker reranker = new MemoryQueryResultReranker(factory, _testSettings);
        _qdrantMemoryClient = new QdrantMemoryClient(_testSettings, memoryAugmentationService);

        _memoryProvider = new HybridMemoryProvider(_currentUserService, new InMemorySynapticDbContextFactory(), _qdrantMemoryClient, _storeRouter, memoryAugmentationService, reranker);
    }

    public TestSettings TestSettings => _testSettings;
    public IMemoryProvider MemoryProvider => _memoryProvider;
    public SynapticDbContext DbContext => _dbContext;
    public QdrantMemoryClient QdrantMemoryClient => _qdrantMemoryClient;
    public ICurrentUserService CurrentUserService => _currentUserService;
    public IMemoryStoreRouter StoreRouter => _storeRouter;

    public bool ShouldSkipIntegrationTest()
    {
        return string.IsNullOrEmpty(_testSettings.OpenAiSettings.ApiKey);
    }
}
