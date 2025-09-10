using System.Text.Json.Serialization;
using Synaptic.NET.Domain.Resources.Management;

namespace Synaptic.NET.Core.Metrics;

public record MetricsEvent<T>
{
    [JsonConstructor]
    protected MetricsEvent(string userIdentifier, T value, DateTime timestamp, string operation)
    {
        UserIdentifier = userIdentifier;
        Value = value;
        Timestamp = timestamp;
        Operation = operation;
    }
    public MetricsEvent(T value, string operation, User? userIdentifier = null)
    {
        Timestamp = DateTime.UtcNow;
        Value = value;
        UserIdentifier = userIdentifier?.Identifier ?? string.Empty;
        Operation = operation;
    }

    [JsonPropertyName("user_identifier")]
    public string UserIdentifier { get; private set; }

    [JsonPropertyName("value")]
    public T Value { get; private set; }

    [JsonPropertyName("timestamp")]
    public DateTime Timestamp { get; private set; }

    [JsonPropertyName("operation")]
    public string Operation { get; private set; }

    [JsonIgnore]
    public TimeSpan Age => DateTime.UtcNow - Timestamp;
}
