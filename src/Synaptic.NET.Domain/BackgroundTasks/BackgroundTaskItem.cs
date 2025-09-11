using Synaptic.NET.Domain.Abstractions.Services;
using Synaptic.NET.Domain.Resources.Management;
using Synaptic.NET.Domain.Scopes;

namespace Synaptic.NET.Domain.BackgroundTasks;

public abstract class BackgroundTaskItem
{
    public string TaskId { get; } = Guid.NewGuid().ToString();
    public string UserId { get; set; } = string.Empty;
    public required User User { get; set; }
    public DateTime CreatedAt { get; } = DateTime.UtcNow;

    public abstract Task ExecuteAsync(ScopeFactory scopeFactory, IBackgroundTaskQueue taskQueue, CancellationToken cancellationToken);
}
