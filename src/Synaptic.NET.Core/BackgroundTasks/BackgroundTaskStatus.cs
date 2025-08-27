namespace Synaptic.NET.Core.BackgroundTasks;

public class BackgroundTaskStatus
{
    public string TaskId { get; set; } = string.Empty;
    public string UserId { get; set; } = string.Empty;
    public string TaskType { get; set; } = string.Empty;
    public BackgroundTaskState Status { get; set; }
    public string Message { get; set; } = string.Empty;
    public double Progress { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public string? ErrorMessage { get; set; }
    public object? Result { get; set; }
}
