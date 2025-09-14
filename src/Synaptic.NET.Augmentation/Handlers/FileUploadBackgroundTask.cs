using Synaptic.NET.Domain.Abstractions.Services;
using Synaptic.NET.Domain.BackgroundTasks;
using Synaptic.NET.Domain.Resources;
using Synaptic.NET.Domain.Scopes;

namespace Synaptic.NET.Augmentation.Handlers;

public class FileUploadBackgroundTask : BackgroundTaskItem
{
    public string FileName { get; set; } = string.Empty;
    public string FileContent { get; set; } = string.Empty; // Base64 for PDFs, plain text for others
    public string FileExtension { get; set; } = string.Empty;
    public long FileSize { get; set; }
    public override async Task ExecuteAsync(ScopeFactory scopeFactory, IBackgroundTaskQueue taskQueue, CancellationToken cancellationToken)
    {
        // Use the stored user instead of trying to get current user (which won't work in background context)
        var user = User ?? throw new InvalidOperationException("User context is required but was not provided to the background task");
        await using var scope = await scopeFactory.CreateFixedUserScopeAsync(user);
        FileProcessor? fileProcessor = null;
        try
        {
            UpdateStatus(taskQueue, BackgroundTaskState.Processing, "Starting file processing...", 0.1);

            byte[] fileBytes = FileExtension.ToLowerInvariant() == ".pdf"
                ? Convert.FromBase64String(FileContent)
                : System.Text.Encoding.UTF8.GetBytes(FileContent);

            using var ms = new MemoryStream(fileBytes);

            UpdateStatus(taskQueue, BackgroundTaskState.Processing, "File archived, starting processing...", 0.2);

            fileProcessor = await scope.FileMemoryCreationService.GetFileProcessor(scopeFactory, User);

            Task processingTask = FileExtension.ToLowerInvariant() == ".pdf"
                ? fileProcessor.ExecutePdfAsync(FileName, FileContent)
                : fileProcessor.ExecuteFile(FileName, FileContent);

            while (!fileProcessor.Completed && !cancellationToken.IsCancellationRequested)
            {
                double normalizedProgress =
                    fileProcessor.Progress > 1.0 ? fileProcessor.Progress / 100.0 : fileProcessor.Progress;

                double mappedProgress = 0.2 + (normalizedProgress * 0.7);
                UpdateStatus(taskQueue, BackgroundTaskState.Processing, fileProcessor.Message, mappedProgress);

                await Task.Delay(500, cancellationToken);
            }

            await processingTask;

            cancellationToken.ThrowIfCancellationRequested();

            UpdateStatus(taskQueue, BackgroundTaskState.Processing, "Saving memories to store...", 0.9);

            if (fileProcessor.Result == null)
            {
                throw new InvalidOperationException("File processing completed but no result was returned");
            }
            var resultStore = fileProcessor.Result;

            if (resultStore.Memories.Count <= 0)
            {
                throw new InvalidOperationException("File was processed but no memories were created");
            }
            resultStore.UserId = user.Id;
            await scope.MemoryProvider.CreateCollectionAsync(resultStore);

            var result = new FileUploadResult
            {
                FileName = FileName,
                MemoryCount = resultStore.Memories.Count,
                StoreIdentifier = resultStore.Title,
                StoreDescription = fileProcessor.StoreDescription
            };

            var finalStatus = new BackgroundTaskStatus
            {
                TaskId = TaskId,
                UserId = UserId,
                TaskType = GetType().Name,
                Status = BackgroundTaskState.Completed,
                Progress = 1.0,
                Message = $"Successfully processed {FileName} and created {result.MemoryCount} memories",
                CreatedAt = CreatedAt,
                CompletedAt = DateTime.UtcNow,
                Result = result
            };

            taskQueue.UpdateTaskStatus(TaskId, finalStatus);

            if (fileProcessor.References == null)
            {
                return;
            }
            await using var dbContext = scope.DbContextInstance;
            await dbContext.IngestionReferences.AddRangeAsync(fileProcessor.References, cancellationToken);
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            var errorStatus = new BackgroundTaskStatus
            {
                TaskId = TaskId,
                UserId = UserId,
                TaskType = GetType().Name,
                Status = BackgroundTaskState.Failed,
                Message = "File processing failed",
                ErrorMessage = ex.Message,
                CreatedAt = CreatedAt,
                CompletedAt = DateTime.UtcNow
            };

            taskQueue.UpdateTaskStatus(TaskId, errorStatus);
            throw;
        }
        finally
        {
            fileProcessor?.Dispose();
        }
    }

    public override void Dispose()
    {
        FileContent = string.Empty;
    }

    private void UpdateStatus(IBackgroundTaskQueue taskQueue, BackgroundTaskState status, string message, double progress)
    {
        var taskStatus = new BackgroundTaskStatus
        {
            TaskId = TaskId,
            UserId = UserId,
            TaskType = GetType().Name,
            Status = status,
            Message = message,
            Progress = progress,
            CreatedAt = CreatedAt
        };

        if (status == BackgroundTaskState.Processing)
        {
            taskStatus.StartedAt = DateTime.UtcNow;
        }

        taskStatus.Result = new { FileName };
        taskQueue.UpdateTaskStatus(TaskId, taskStatus);
    }
}
