using System.Diagnostics;
using System.Diagnostics.Metrics;
using Microsoft.EntityFrameworkCore;
using OpenAI.Chat;
using Synaptic.NET.Core.Providers;
using Synaptic.NET.Domain.Resources;
using Synaptic.NET.Domain.Resources.Management;
using Synaptic.NET.Domain.Resources.Metrics;

namespace Synaptic.NET.Core.Metrics;

public class TokenMetricCollector : IMetricsCollector
{
    private readonly Histogram<long> _inputTokenCounter;
    private readonly Histogram<long> _outputTokenCounter;
    private readonly IDbContextFactory<SynapticDbContext> _dbContext;
    public TokenMetricCollector(IDbContextFactory<SynapticDbContext> dbContext)
    {
        _dbContext = dbContext;
        Meter = new Meter(MeterName);
        _inputTokenCounter = Meter.CreateHistogram<long>($"{MeterName}.InputTokens", "tokens");
        _outputTokenCounter = Meter.CreateHistogram<long>($"{MeterName}.OutputTokens", "tokens");
    }

    public string MeterName => $"{MetricsCollectorProvider.ServiceName}.TokenMeter";
    public Meter Meter { get; }

    public async Task<IReadOnlyList<TokenMetric>> GetTokenMetricsAsync()
    {
        await using var dbContext = await _dbContext.CreateDbContextAsync();
        return await dbContext.TokenMetrics.ToListAsync();
    }

    public async Task IncrementTokenCountsFromChatCompletionAsync(User? user, string operation, ChatCompletion completion)
    {
        await IncrementInputTokenCountAsync(user, operation, completion.Model, completion.Usage.InputTokenCount);
        await IncrementOutputTokenCountAsync(user, operation, completion.Model, completion.Usage.OutputTokenCount);
    }

    public async Task IncrementInputTokenCountAsync(User? user, string operation, string model, long count)
    {
        string operationString = $"Token incurrence with model |{model}|, Input: {operation}";
        var tokenMetric = new TokenMetric { Timestamp = DateTimeOffset.UtcNow, UserId = user?.Id ?? Guid.Empty, Model = model, Count = count, Operation = operationString, Id = Guid.NewGuid(), IsInput = true };
        await using var dbContext = await _dbContext.CreateDbContextAsync();
        dbContext.TokenMetrics.Add(tokenMetric);
        await dbContext.SaveChangesAsync();
        _inputTokenCounter.Record(count, new TagList
        {
            { "user.id", user },
            { "operation", operation },
            { "ai.model", model },
            { "service.name", MetricsCollectorProvider.ServiceName }
        });
    }

    public async Task IncrementOutputTokenCountAsync(User? user, string operation, string model, long count)
    {
        string operationString = $"Token incurrence with model |{model}|, Output: {operation}";
        var tokenMetric = new TokenMetric { Timestamp = DateTimeOffset.UtcNow, UserId = user?.Id ?? Guid.Empty, Model = model, Count = count, Operation = operationString, Id = Guid.NewGuid(), IsInput = false };
        await using var dbContext = await _dbContext.CreateDbContextAsync();
        dbContext.TokenMetrics.Add(tokenMetric);
        await dbContext.SaveChangesAsync();
        _outputTokenCounter.Record(count, new TagList
        {
            { "user.id", user },
            { "operation", operation },
            { "ai.model", model },
            { "service.name", MetricsCollectorProvider.ServiceName }
        });
    }
}

