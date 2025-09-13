using System.ComponentModel.DataAnnotations;

namespace Synaptic.NET.Domain.Resources.Metrics;

public class TokenMetric
{
    [Key]
    public required Guid Id { get; set; }

    public bool IsInput { get; set; }

    public required Guid UserId { get; set; }

    public required long Count { get; set; }

    public required DateTimeOffset Timestamp { get; set; }

    [MaxLength(256)]
    public required string Model { get; set; }

    [MaxLength(16384)]
    public string Operation { get; set; } = string.Empty;
}
