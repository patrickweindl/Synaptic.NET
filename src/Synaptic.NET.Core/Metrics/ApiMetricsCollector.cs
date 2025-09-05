using System.Diagnostics;
using System.Diagnostics.Metrics;
using Synaptic.NET.Core.Providers;
using Synaptic.NET.Domain.Resources.Management;

namespace Synaptic.NET.Core.Metrics;

public class ApiMetricsCollector : IMetricsCollector<TimeSpan>
{
    private readonly Histogram<double> _benchmarkMeter;
    private readonly InMemoryMetricsStore _metricsStore;

    public ApiMetricsCollector(InMemoryMetricsStore metricsStore)
    {
        _metricsStore = metricsStore;
        Meter = new Meter(MeterName);
        _benchmarkMeter = Meter.CreateHistogram<double>("BenchmarkCounter", "ms");
    }

    public string MeterName => "Synaptic.Api.BenchmarkMeter";
    public Meter Meter { get; }

    public InMemoryMeter<TimeSpan> InMemoryMeter
    {
        get
        {
            if (_metricsStore.Benchmarks.TryGetValue(MeterName, out var meter))
            {
                return meter;
            }
            var newMeter = new InMemoryMeter<TimeSpan>(MeterName);
            _metricsStore.Benchmarks.TryAdd(MeterName, newMeter);
            return newMeter;
        }
    }

    public void RecordBenchmark(TimeSpan value, string operation, User? user)
    {
        InMemoryMeter.Record(new MetricsEvent<TimeSpan>(value, operation, user));
        _benchmarkMeter.Record(value.TotalMilliseconds, new TagList
        {
            { "user.id", user },
            { "operation", operation },
            { "service.name", MetricsCollectorProvider.ServiceName }
        });
    }
}

