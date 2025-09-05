using System.Diagnostics.Metrics;
using Synaptic.NET.Domain.Metrics;

namespace Synaptic.NET.Domain;

public interface IMetricsCollector<T>
{
    /// <summary>
    /// The name of the meter.
    /// </summary>
    string MeterName { get; }

    /// <summary>
    /// A regular meter that can be used to record metrics. Does not persist metrics.
    /// </summary>
    Meter Meter { get; }

    /// <summary>
    /// The in memory meter that usually collects values together with the classical <see cref="Meter"/> but persists the values.
    /// </summary>
    InMemoryMeter<T> InMemoryMeter { get; }
}
