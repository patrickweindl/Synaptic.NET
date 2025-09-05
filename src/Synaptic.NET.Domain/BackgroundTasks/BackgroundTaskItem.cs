using Synaptic.NET.Domain.Abstractions.Augmentation;
using Synaptic.NET.Domain.Abstractions.Management;
using Synaptic.NET.Domain.Abstractions.Services;
using Synaptic.NET.Domain.Abstractions.Storage;

namespace Synaptic.NET.Domain.BackgroundTasks;

public abstract class BackgroundTaskItem
{
    public string TaskId { get; } = Guid.NewGuid().ToString();
    public string UserId { get; set; } = string.Empty;
    public DateTime CreatedAt { get; } = DateTime.UtcNow;

    public abstract Task ExecuteAsync(ICurrentUserService currentUserService, IArchiveService archiveService, IMemoryProvider memoryProvider, IFileMemoryCreationService fileMemoryCreationService, IBackgroundTaskQueue taskQueue, CancellationToken cancellationToken);
}
