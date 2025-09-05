namespace Synaptic.NET.Domain.BackgroundTasks;

public enum BackgroundTaskState
{
    Queued,
    Processing,
    Completed,
    Failed
}
