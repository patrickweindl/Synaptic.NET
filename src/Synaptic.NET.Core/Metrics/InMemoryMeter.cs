using System.Collections.Concurrent;
using System.Security.Claims;
using System.Text.Json.Serialization;
using Synaptic.NET.Domain.Helpers;

namespace Synaptic.NET.Core.Metrics;

public class InMemoryMeter<T>
{
    public InMemoryMeter(string name)
    {
        Name = name;
    }

    public void Record(MetricsEvent<T> eventData)
    {
        Events.Add(eventData);
    }

    public IEnumerable<T> GetUserValues(ClaimsIdentity? user)
    {
        return Events.Where(e => e.UserIdentifier == user.ToUserIdentifier()).Select(e => e.Value);
    }

    public IEnumerable<MetricsEvent<T>> GetEvents(string userIdentifier)
    {
        return Events.Where(e => e.UserIdentifier == userIdentifier);
    }

    public IEnumerable<MetricsEvent<T>> GetEvents(ClaimsIdentity? user)
    {
        return Events.Where(e => e.UserIdentifier.Equals(user.ToUserIdentifier()));
    }

    public IEnumerable<T> GetValues()
    {
        return Events.Select(e => e.Value);
    }

    [JsonIgnore]
    public ConcurrentBag<MetricsEvent<T>> Events { get; set; } = new();

    [JsonPropertyName("metrics_events")]
    public List<MetricsEvent<T>> MetricsEvents
    {
        get => Events.ToList();
        set => Events = new ConcurrentBag<MetricsEvent<T>>(value);
    }

    [JsonPropertyName("name")]
    public string Name { get; private set; }
}
