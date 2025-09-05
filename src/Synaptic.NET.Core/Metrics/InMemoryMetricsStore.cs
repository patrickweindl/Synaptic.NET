using System.Collections.Concurrent;
using System.Text.Json;
using System.Text.Json.Serialization;
using Synaptic.NET.Domain.Resources.Management;

namespace Synaptic.NET.Core.Metrics;

public class InMemoryMetricsStore
{
    [JsonPropertyName("counters")]
    public ConcurrentDictionary<string, InMemoryMeter<long>> Counters { get; set; } = new();

    [JsonPropertyName("histograms")]
    public ConcurrentDictionary<string, InMemoryMeter<double>> Histograms { get; set; } = new();

    [JsonPropertyName("benchmarks")]
    public ConcurrentDictionary<string, InMemoryMeter<TimeSpan>> Benchmarks { get; set; } = new();

    public void ExportToFile()
    {
        string exportPath = Path.Join(AppDomain.CurrentDomain.BaseDirectory, "metrics.json");
        File.WriteAllText(exportPath, JsonSerializer.Serialize(this, new JsonSerializerOptions { WriteIndented = true }));
    }

    public void InitFromFile()
    {
        if (!File.Exists(Path.Join(AppDomain.CurrentDomain.BaseDirectory, "metrics.json")))
        {
            return;
        }

        string exportPath = Path.Join(AppDomain.CurrentDomain.BaseDirectory, "metrics.json");
        string content = File.ReadAllText(exportPath);
        var savedStore = JsonSerializer.Deserialize<InMemoryMetricsStore>(content) ?? new();
        Counters = savedStore.Counters;
        Histograms = savedStore.Histograms;
        Benchmarks = savedStore.Benchmarks;
    }

    public void IncrementCounter(string meterName, string counterName, string operation, User? user)
    {
        string key = $"{meterName}:{counterName}";
        if (!Counters.TryGetValue(key, out InMemoryMeter<long>? existingMeter))
        {
            Counters.TryAdd(key, new InMemoryMeter<long>(key));
        }
        existingMeter?.Record(new MetricsEvent<long>(1, operation, user));
    }

    public void AddToCounter(long value, string meterName, string counterName, string operation, User? user)
    {
        string key = $"{meterName}:{counterName}";
        if (!Counters.TryGetValue(key, out InMemoryMeter<long>? existingMeter))
        {
            Counters.TryAdd(key, new InMemoryMeter<long>(key));
        }
        existingMeter?.Record(new MetricsEvent<long>(value, operation, user));
    }

    public void AddToHistogram(double value, string meterName, string counterName, string operation, User? user)
    {
        string key = $"{meterName}:{counterName}";
        if (!Histograms.TryGetValue(key, out InMemoryMeter<double>? existingMeter))
        {
            Histograms.TryAdd(key, new InMemoryMeter<double>(key));
        }
        existingMeter?.Record(new MetricsEvent<double>(value, operation, user));
    }

    public void AddToBenchmark(TimeSpan value, string meterName, string counterName, string operation, User? user)
    {
        string key = $"{meterName}:{counterName}";
        if (!Benchmarks.TryGetValue(key, out InMemoryMeter<TimeSpan>? existingMeter))
        {
            Benchmarks.TryAdd(key, new InMemoryMeter<TimeSpan>(key));
        }
        existingMeter?.Record(new MetricsEvent<TimeSpan>(value, operation, user));
    }

}
