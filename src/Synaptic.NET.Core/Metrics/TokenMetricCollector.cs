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
        ReadPersistentInfoFromDatabase();
    }

    private void ReadPersistentInfoFromDatabase()
    {
        using var dbContext = _dbContext.CreateDbContext();
        var persistentMetrics = dbContext.TokenMetrics.ToList();
        foreach (var metric in persistentMetrics)
        {
            if (metric.IsInput)
            {
                _inputTokenCounter.Record(metric.Count, new TagList
                {
                    { "user.id", metric.UserId },
                    { "operation", metric.Operation },
                    { "ai.model", metric.Model },
                    { "service.name", MetricsCollectorProvider.ServiceName }
                });
            }
            else
            {
                _outputTokenCounter.Record(metric.Count, new TagList
                {
                    { "user.id", metric.UserId },
                    { "operation", metric.Operation },
                    { "ai.model", metric.Model },
                    { "service.name", MetricsCollectorProvider.ServiceName }
                });
            }
        }
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
        Guid operationId = Guid.NewGuid();
        await IncrementInputTokenCountAsync(user, operation, completion.Model, completion.Usage.InputTokenCount, operationId);
        await IncrementOutputTokenCountAsync(user, operation, completion.Model, completion.Usage.OutputTokenCount, operationId);
    }

    public async Task IncrementInputTokenCountAsync(User? user, string operation, string model, long count, Guid? operationId = null)
    {
        var tokenMetric = new TokenMetric { OperationId = operationId ?? Guid.NewGuid(), Timestamp = DateTimeOffset.UtcNow, UserId = user?.Id ?? Guid.Empty, Model = model, Count = count, Operation = operation, Id = Guid.NewGuid(), IsInput = true };
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

    public async Task IncrementOutputTokenCountAsync(User? user, string operation, string model, long count, Guid? operationId = null)
    {
        var tokenMetric = new TokenMetric { OperationId = operationId ?? Guid.NewGuid(), Timestamp = DateTimeOffset.UtcNow, UserId = user?.Id ?? Guid.Empty, Model = model, Count = count, Operation = operation, Id = Guid.NewGuid(), IsInput = false };
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

