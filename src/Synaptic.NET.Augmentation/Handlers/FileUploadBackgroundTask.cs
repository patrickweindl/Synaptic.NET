using Synaptic.NET.Core;
using Synaptic.NET.Core.BackgroundTasks;
using Synaptic.NET.Domain.Resources;

namespace Synaptic.NET.Augmentation.Handlers;

public class FileUploadBackgroundTask : BackgroundTaskItem
{
    public string FileName { get; set; } = string.Empty;
    public string FileContent { get; set; } = string.Empty; // Base64 for PDFs, plain text for others
    public string FileExtension { get; set; } = string.Empty;
    public long FileSize { get; set; }

    public override async Task ExecuteAsync(IArchiveService archiveService, IMemoryProvider memoryProvider, IFileMemoryCreationService fileMemoryCreationService, IBackgroundTaskQueue taskQueue, CancellationToken cancellationToken)
    {
        try
        {
            UpdateStatus(taskQueue, BackgroundTaskState.Processing, "Starting file processing...", 0.1);

            byte[] fileBytes = FileExtension.ToLowerInvariant() == ".pdf"
                ? Convert.FromBase64String(FileContent)
                : System.Text.Encoding.UTF8.GetBytes(FileContent);

            using var ms = new MemoryStream(fileBytes);
            await archiveService.SaveFileAsync(FileName, ms);

            UpdateStatus(taskQueue, BackgroundTaskState.Processing, "File archived, starting processing...", 0.2);

            var fileProcessor = await fileMemoryCreationService.GetFileProcessor();

            Task processingTask = FileExtension.ToLowerInvariant() == ".pdf"
                ? fileProcessor.ExecutePdf(FileName, FileContent)
                : fileProcessor.ExecuteFile(FileName, FileContent);

            while (!fileProcessor.Completed && !cancellationToken.IsCancellationRequested)
            {
                double mappedProgress = 0.2 + fileProcessor.Progress * 0.7;
                UpdateStatus(taskQueue, BackgroundTaskState.Processing, fileProcessor.Message, mappedProgress);

                await Task.Delay(1000, cancellationToken);
            }

            await processingTask;

            UpdateStatus(taskQueue, BackgroundTaskState.Processing, "Saving memories to store...", 0.9);

            if (fileProcessor.Result.Count > 0)
            {
                await Parallel.ForEachAsync(fileProcessor.Result, cancellationToken, async (result, _) =>
                    {
                        Memory memory = result.memory;
                        await memoryProvider.CreateMemoryEntryAsync(memory.StoreIdentifier, memory, fileProcessor.StoreDescription);
                    });

                var result = new FileUploadResult
                {
                    FileName = FileName,
                    MemoryCount = fileProcessor.Result.Count,
                    StoreIdentifier = fileProcessor.Result.FirstOrDefault().memory.StoreTitle,
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
            }
            else
            {
                throw new InvalidOperationException("File was processed but no memories were created");
            }
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
