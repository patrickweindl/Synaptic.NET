using System.Diagnostics;
using System.Diagnostics.Metrics;
using Microsoft.EntityFrameworkCore;
using Synaptic.NET.Core.Providers;
using Synaptic.NET.Domain.Resources;
using Synaptic.NET.Domain.Resources.Management;
using Synaptic.NET.Domain.Resources.Metrics;

namespace Synaptic.NET.Core.Metrics;

public class ApiMetricsCollector : IMetricsCollector
{
    private readonly Histogram<double> _benchmarkMeter;
    private readonly IDbContextFactory<SynapticDbContext> _dbContextFactory;


    public ApiMetricsCollector(IDbContextFactory<SynapticDbContext> dbContextFactory)
    {
        _dbContextFactory = dbContextFactory;
        Meter = new Meter(MeterName);
        _benchmarkMeter = Meter.CreateHistogram<double>(MeterName, "ms");
    }

    public string MeterName => $"{MetricsCollectorProvider.ServiceName}.BenchmarkMeter";
    public Meter Meter { get; }

    public async Task RecordBenchmark(TimeSpan value, string operation, User? user)
    {
        await using var dbContext = await _dbContextFactory.CreateDbContextAsync();
        var benchmarkMetric = new BenchmarkMetric { TimeStamp = DateTimeOffset.UtcNow, Id = Guid.NewGuid(), Operation = operation, UserId = user?.Id ?? Guid.Empty, Duration = value };
        dbContext.BenchmarkMetrics.Add(benchmarkMetric);
        await dbContext.SaveChangesAsync();
        _benchmarkMeter.Record(value.TotalMilliseconds, new TagList
        {
            { "user.id", user },
            { "operation", operation },
            { "service.name", MetricsCollectorProvider.ServiceName }
        });
    }
}

