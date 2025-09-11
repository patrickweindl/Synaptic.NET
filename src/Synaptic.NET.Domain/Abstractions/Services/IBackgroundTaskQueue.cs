using Synaptic.NET.Domain.BackgroundTasks;

namespace Synaptic.NET.Domain.Abstractions.Services;

public interface IBackgroundTaskQueue
{
    ValueTask QueueBackgroundWorkItemAsync(BackgroundTaskItem workItem);
    ValueTask<BackgroundTaskItem> DequeueAsync(CancellationToken cancellationToken);
    BackgroundTaskStatus? GetTaskStatus(string taskId);
    void UpdateTaskStatus(string taskId, BackgroundTaskStatus status);
    IEnumerable<BackgroundTaskStatus> GetUserTasks(string userId);
    
    event Action<BackgroundTaskStatus>? TaskStatusChanged;
}
