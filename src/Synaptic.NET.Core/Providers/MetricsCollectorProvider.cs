using Synaptic.NET.Core.Metrics;
using Synaptic.NET.Domain.Helpers;

namespace Synaptic.NET.Core.Providers;

public sealed class MetricsCollectorProvider : IMetricsCollectorProvider, IDisposable
{
    public const string ServiceName = "mneme-api";

    private readonly CancellationTokenSource _tokenSource = new();
    public MetricsCollectorProvider()
    {
        MetricsStore = new();
        try
        {
            MetricsStore.InitFromFile();
        }
        catch {/**/}
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
