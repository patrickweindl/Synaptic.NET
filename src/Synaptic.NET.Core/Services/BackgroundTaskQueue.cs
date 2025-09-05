using System.Collections.Concurrent;
using System.Threading.Channels;
using Synaptic.NET.Domain.Abstractions.Services;
using Synaptic.NET.Domain.BackgroundTasks;

namespace Synaptic.NET.Core.Services;

public class BackgroundTaskQueue : IBackgroundTaskQueue
{
    private readonly Channel<BackgroundTaskItem> _queue;
    private readonly ConcurrentDictionary<string, BackgroundTaskStatus> _taskStatuses = new();

    public BackgroundTaskQueue(int capacity = 100)
    {
        var options = new BoundedChannelOptions(capacity)
        {
            FullMode = BoundedChannelFullMode.Wait
        };
        _queue = Channel.CreateBounded<BackgroundTaskItem>(options);
    }

    public async ValueTask QueueBackgroundWorkItemAsync(BackgroundTaskItem workItem)
    {
        if (workItem == null)
        {
            throw new ArgumentNullException(nameof(workItem));
        }

        // Initialize task status
        var status = new BackgroundTaskStatus
        {
            TaskId = workItem.TaskId,
            UserId = workItem.UserId,
            TaskType = workItem.GetType().Name,
            Status = BackgroundTaskState.Queued,
            CreatedAt = DateTime.UtcNow,
            Message = "Task queued for processing"
        };
        _taskStatuses[workItem.TaskId] = status;

        await _queue.Writer.WriteAsync(workItem);
    }

    public async ValueTask<BackgroundTaskItem> DequeueAsync(CancellationToken cancellationToken)
    {
        var workItem = await _queue.Reader.ReadAsync(cancellationToken);

        // Update status to processing
        if (_taskStatuses.TryGetValue(workItem.TaskId, out var status))
        {
            status.Status = BackgroundTaskState.Processing;
            status.StartedAt = DateTime.UtcNow;
            status.Message = "Task is being processed";
        }

        return workItem;
    }

    public BackgroundTaskStatus? GetTaskStatus(string taskId)
    {
        return _taskStatuses.TryGetValue(taskId, out var status) ? status : null;
    }

    public void UpdateTaskStatus(string taskId, BackgroundTaskStatus status)
    {
        _taskStatuses[taskId] = status;

        var cutoff = DateTime.UtcNow.AddHours(-24);
        var completedTasks = _taskStatuses
            .Where(kvp => kvp.Value.Status is BackgroundTaskState.Completed or BackgroundTaskState.Failed &&
                         kvp.Value.CompletedAt < cutoff)
            .Select(kvp => kvp.Key)
            .ToList();

        foreach (var completedTaskId in completedTasks)
        {
            _taskStatuses.TryRemove(completedTaskId, out _);
        }
    }

    public IEnumerable<BackgroundTaskStatus> GetUserTasks(string userId)
    {
        return _taskStatuses.Values
            .Where(status => status.UserId == userId)
            .OrderByDescending(status => status.CreatedAt);
    }
}
