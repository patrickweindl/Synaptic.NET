using Microsoft.EntityFrameworkCore;
using OpenTelemetry.Metrics;
using Synaptic.NET.Core.Metrics;
using Synaptic.NET.Domain.Resources;

namespace Synaptic.NET.Core.Providers;

public sealed class MetricsCollectorProvider : IMetricsCollectorProvider
{
    public const string ServiceName = "Synaptic.NET";

    public MetricsCollectorProvider(IDbContextFactory<SynapticDbContext> dbContextFactory)
    {
        TokenMetrics = new TokenMetricCollector(dbContextFactory);
        ApiMetrics = new ApiMetricsCollector(dbContextFactory);
    }

    public TokenMetricCollector TokenMetrics { get; }

    public ApiMetricsCollector ApiMetrics { get; }
}
