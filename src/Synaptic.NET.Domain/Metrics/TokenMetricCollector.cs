using System.Diagnostics;
using System.Diagnostics.Metrics;
using System.Security.Claims;
using OpenAI.Chat;
using Synaptic.NET.Domain.Providers;

namespace Synaptic.NET.Domain.Metrics;

public class TokenMetricCollector : IMetricsCollector<long>
{
    private readonly Histogram<long> _inputTokenCounter;
    private readonly Histogram<long> _outputTokenCounter;
    private readonly InMemoryMetricsStore _metricsStore;

    public TokenMetricCollector(InMemoryMetricsStore metricsStore)
    {
        _metricsStore = metricsStore;
        Meter = new Meter(MeterName);
        _inputTokenCounter = Meter.CreateHistogram<long>("InputTokenHistogram", "tokens");
        _outputTokenCounter = Meter.CreateHistogram<long>("OutputTokenHistogram", "tokens");
    }

    public string MeterName => "Mneme.Api.Tokens";
    public Meter Meter { get; }

    public InMemoryMeter<long> InMemoryMeter
    {
        get
        {
            if (_metricsStore.Counters.TryGetValue(MeterName, out var meter))
            {
                return meter;
            }
            var newMeter = new InMemoryMeter<long>(MeterName);
            _metricsStore.Counters.TryAdd(MeterName, newMeter);
            return newMeter;
        }
    }

    public void IncrementTokenCountsFromChatCompletion(ClaimsIdentity? user, string operation, ChatCompletion completion)
    {
        IncrementInputTokenCount(user, operation, completion.Model, completion.Usage.InputTokenCount);
        IncrementOutputTokenCount(user, operation, completion.Model, completion.Usage.OutputTokenCount);
    }

    public void IncrementInputTokenCount(ClaimsIdentity? user, string operation, string model, long count)
    {
        InMemoryMeter.Record(new MetricsEvent<long>(count, $"Token incurrence by |{model}|, Input: {operation}", user));
        _inputTokenCounter.Record(count, new TagList
        {
            { "user.id", user },
            { "operation", operation },
            { "ai.model", model },
            { "service.name", MetricsCollectorProvider.ServiceName }
        });
    }

    public void IncrementOutputTokenCount(ClaimsIdentity? user, string operation, string model, long count)
    {
        InMemoryMeter.Record(new MetricsEvent<long>(count, $"Token incurrence by |{model}|, Output: {operation}", user));
        _outputTokenCounter.Record(count, new TagList
        {
            { "user.id", user },
            { "operation", operation },
            { "ai.model", model },
            { "service.name", MetricsCollectorProvider.ServiceName }
        });
    }
}

