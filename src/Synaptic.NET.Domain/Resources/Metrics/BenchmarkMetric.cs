using System.ComponentModel.DataAnnotations;

namespace Synaptic.NET.Domain.Resources.Metrics;

public class BenchmarkMetric
{
    [Key]
    public required Guid Id { get; set; }

    public TimeSpan Duration { get; set; }

    public required Guid UserId { get; set; }

    public required DateTimeOffset TimeStamp { get; set; }

    [MaxLength(16384)]
    public string Operation { get; set; } = string.Empty;
}
