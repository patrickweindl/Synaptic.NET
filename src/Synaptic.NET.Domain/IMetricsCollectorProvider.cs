using Synaptic.NET.Domain.Metrics;

namespace Synaptic.NET.Domain;

public interface IMetricsCollectorProvider
{
    TokenMetricCollector TokenMetrics { get; }

    ApiMetricsCollector ApiMetrics { get; }
}
