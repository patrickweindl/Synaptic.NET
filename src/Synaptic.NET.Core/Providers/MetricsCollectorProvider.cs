using Synaptic.NET.Domain.Helpers;
using Synaptic.NET.Domain.Metrics;

namespace Synaptic.NET.Domain.Providers;

public sealed class MetricsCollectorProvider : IMetricsCollectorProvider, IDisposable
{
    public const string ServiceName = "mneme-api";

    private readonly CancellationTokenSource _tokenSource = new();
    public MetricsCollectorProvider()
    {
        MetricsStore = new();
        MetricsStore.InitFromFile();
        TokenMetrics = new TokenMetricCollector(MetricsStore);
        ApiMetrics = new ApiMetricsCollector(MetricsStore);
        RecurringTask.Create(MetricsStore.ExportToFile, TimeSpan.FromMinutes(1), _tokenSource.Token);
    }

    public InMemoryMetricsStore MetricsStore { get; }

    public TokenMetricCollector TokenMetrics { get; }

    public ApiMetricsCollector ApiMetrics { get; }

    public void Dispose()
    {
        _tokenSource.Cancel();
    }
}
