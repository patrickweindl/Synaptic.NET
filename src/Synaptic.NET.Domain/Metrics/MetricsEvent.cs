using System.Security.Claims;
using System.Text.Json.Serialization;
using Synaptic.NET.Domain.Helpers;

namespace Synaptic.NET.Domain.Metrics;

public record MetricsEvent<T>
{
    public MetricsEvent(T value, string operation, ClaimsIdentity? userIdentifier = null)
    {
        Timestamp = DateTime.UtcNow;
        Value = value;
        UserIdentifier = userIdentifier.ToUserIdentifier();
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
