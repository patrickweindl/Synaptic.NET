using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Synaptic.NET.Core.BackgroundTasks;

namespace Synaptic.NET.Core.Services;

public class BackgroundTaskService : BackgroundService
{
    private readonly ILogger<BackgroundTaskService> _logger;
    private readonly IServiceProvider _serviceProvider;

    public BackgroundTaskService(
        ILogger<BackgroundTaskService> logger,
        IServiceProvider serviceProvider)
    {
        _logger = logger;
        _serviceProvider = serviceProvider;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await BackgroundProcessing(stoppingToken);
    }

    private async Task BackgroundProcessing(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _serviceProvider.CreateScope();
                var taskQueue = scope.ServiceProvider.GetRequiredService<IBackgroundTaskQueue>();

                var archiveService = scope.ServiceProvider.GetRequiredService<IArchiveService>();
                var memoryProvider = scope.ServiceProvider.GetRequiredService<IMemoryProvider>();
                var fileMemoryCreationService = scope.ServiceProvider.GetRequiredService<IFileMemoryCreationService>();
                var currentUserService = scope.ServiceProvider.GetRequiredService<ICurrentUserService>();
                var workItem = await taskQueue.DequeueAsync(stoppingToken);

                _logger.LogInformation("Processing background task {TaskId} of type {TaskType} for user {UserId}",
                    workItem.TaskId, workItem.GetType().Name, workItem.UserId);

                try
                {
                    await workItem.ExecuteAsync(currentUserService, archiveService, memoryProvider, fileMemoryCreationService, taskQueue, stoppingToken);

                    var completedStatus = new BackgroundTaskStatus
                    {
                        TaskId = workItem.TaskId,
                        UserId = workItem.UserId,
                        TaskType = workItem.GetType().Name,
                        Status = BackgroundTaskState.Completed,
                        Progress = 1.0,
                        Message = "Task completed successfully",
                        CreatedAt = workItem.CreatedAt,
                        CompletedAt = DateTime.UtcNow
                    };

                    taskQueue.UpdateTaskStatus(workItem.TaskId, completedStatus);

                    _logger.LogInformation("Background task {TaskId} completed successfully", workItem.TaskId);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error occurred executing background task {TaskId}", workItem.TaskId);

                    var failedStatus = new BackgroundTaskStatus
                    {
                        TaskId = workItem.TaskId,
                        UserId = workItem.UserId,
                        TaskType = workItem.GetType().Name,
                        Status = BackgroundTaskState.Failed,
                        Message = "Task failed with error",
                        ErrorMessage = ex.Message,
                        CreatedAt = workItem.CreatedAt,
                        CompletedAt = DateTime.UtcNow
                    };

                    taskQueue.UpdateTaskStatus(workItem.TaskId, failedStatus);
                }
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred in background task service");
                await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
            }
        }
    }

    public override async Task StopAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Background task service is stopping");
        await base.StopAsync(stoppingToken);
    }
}
