using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Synaptic.NET.Domain.BackgroundTasks;
using Synaptic.NET.Domain.Scopes;

namespace Synaptic.NET.Core.Services;

public class BackgroundTaskService : BackgroundService
{
    private readonly ILogger<BackgroundTaskService> _logger;
    private readonly ScopeFactory _scopeFactory;

    public BackgroundTaskService(
        ILogger<BackgroundTaskService> logger,
        ScopeFactory scopeFactory)
    {
        _logger = logger;
        _scopeFactory = scopeFactory;
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
                var taskQueue = _scopeFactory.GetBackgroundTaskQueue();
                BackgroundTaskItem workItem = await taskQueue.DequeueAsync(stoppingToken);
                await using var scope = await _scopeFactory.CreateFixedUserScopeAsync(workItem.User);
                var dbContext = scope.DbContextInstance;
                var currentUserService = scope.CurrentUserService;
                await currentUserService.SetCurrentUserAsync(workItem.User);
                await dbContext.SetCurrentUserAsync(workItem.User);

                _logger.LogInformation("Processing background task {TaskId} of type {TaskType} for user {UserId}",
                    workItem.TaskId, workItem.GetType().Name, workItem.UserId);
                try
                {
                    await workItem.ExecuteAsync(_scopeFactory, taskQueue, stoppingToken);

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
