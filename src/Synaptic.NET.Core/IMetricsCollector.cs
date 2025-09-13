using System.Diagnostics.Metrics;
using Synaptic.NET.Core.Metrics;

namespace Synaptic.NET.Core;

public interface IMetricsCollector
{
    /// <summary>
    /// The name of the meter.
    /// </summary>
    string MeterName { get; }

    /// <summary>
    /// A regular meter that can be used to record metrics. Does not persist metrics.
    /// </summary>
    Meter Meter { get; }
}
