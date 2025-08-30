namespace Synaptic.NET.Core.BackgroundTasks;

public abstract class BackgroundTaskItem
{
    public string TaskId { get; } = Guid.NewGuid().ToString();
    public string UserId { get; set; } = string.Empty;
    public DateTime CreatedAt { get; } = DateTime.UtcNow;

    public abstract Task ExecuteAsync(ICurrentUserService currentUserService, IArchiveService archiveService, IMemoryProvider memoryProvider, IFileMemoryCreationService fileMemoryCreationService, IBackgroundTaskQueue taskQueue, CancellationToken cancellationToken);
}
