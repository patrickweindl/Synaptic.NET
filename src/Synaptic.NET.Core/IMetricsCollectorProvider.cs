using Synaptic.NET.Core.Metrics;

namespace Synaptic.NET.Core;

public interface IMetricsCollectorProvider
{
    TokenMetricCollector TokenMetrics { get; }

    ApiMetricsCollector ApiMetrics { get; }
}
