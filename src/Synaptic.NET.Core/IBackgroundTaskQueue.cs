using Synaptic.NET.Core.BackgroundTasks;

namespace Synaptic.NET.Core;

public interface IBackgroundTaskQueue
{
    ValueTask QueueBackgroundWorkItemAsync(BackgroundTaskItem workItem);
    ValueTask<BackgroundTaskItem> DequeueAsync(CancellationToken cancellationToken);
    BackgroundTaskStatus? GetTaskStatus(string taskId);
    void UpdateTaskStatus(string taskId, BackgroundTaskStatus status);
    IEnumerable<BackgroundTaskStatus> GetUserTasks(string userId);
}
