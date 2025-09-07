using System.Collections.Concurrent;
using Synaptic.NET.Domain.Abstractions.Augmentation;
using Synaptic.NET.Domain.Helpers;
using Synaptic.NET.Domain.Resources.Management;
using Synaptic.NET.Domain.Resources.Storage;
using Synaptic.NET.Domain.StructuredResponses;
using UglyToad.PdfPig;
using UglyToad.PdfPig.Writer;

namespace Synaptic.NET.Domain.Resources;

/// <summary>
/// A class representing the result of a file processing operation with observable progress and status message.
/// </summary>
public class FileProcessor
{
    private readonly IFileMemoryCreationService _fileMemoryCreationService;
    private readonly IMemoryAugmentationService _augmentationService;
    private int _chunksCount;
    private int _chunksFinished;

    public FileProcessor(IFileMemoryCreationService fileMemoryCreationService, IMemoryAugmentationService augmentationService)
    {
        _fileMemoryCreationService = fileMemoryCreationService;
        _augmentationService = augmentationService;
    }
    public async Task ExecutePdfFile(User user, string fileName, string filePath)
    {
        byte[] fileBytes = await File.ReadAllBytesAsync(filePath);
        string base64String = Convert.ToBase64String(fileBytes);
        await ExecutePdf(user, fileName, base64String);
    }

    public async Task ExecutePdf(User user, string fileName, string base64String)
    {
        Log.Information("[File Processor] Received a new file to process with name {FileName} and length {Base64StringLength}.", fileName, base64String.Length);
        string storeId = $"project__{FileProcessingHelper.SanitizeFileName(Path.GetFileNameWithoutExtension(fileName))}";
        DateTime start = DateTime.Now;
        Log.Information("[File Processor] Starting file chunking...");
        Message = "Starting file chunking...";

        byte[] pdfBytes = Convert.FromBase64String(base64String);
        using var ms = new MemoryStream(pdfBytes);
        using var doc = PdfDocument.Open(ms);

        List<string> base64EncodedChunks = new();
        int chunkPageSize = 5;
        int chunkOverlap = 1;
        var chunkIndices = FileProcessingHelper.CreateChunks(doc.NumberOfPages, chunkPageSize, chunkOverlap);
        foreach (var chunkIndex in chunkIndices)
        {
            var destStream = new MemoryStream();
            using var destDoc = new PdfDocumentBuilder(destStream, disposeStream: false);

            doc.CopyPagesTo(chunkIndex.start + 1, (chunkIndex.end + 1) > doc.NumberOfPages ? doc.NumberOfPages : chunkIndex.end + 1, destDoc);
            var output = destDoc.Build();
            base64EncodedChunks.Add(Convert.ToBase64String(output));
            Progress += 0.1 / chunkIndices.Count;
        }
        _chunksCount = base64EncodedChunks.Count;

        Progress = 0.1;
        Message = "Finished file preparation. Starting model processing...";
        Log.Information("[File Processor] Finished file chunking, created {Count} chunks!", base64EncodedChunks.Count);

        var chunkTasks = base64EncodedChunks.Select(async chunk => await CreateMemorySummaryFromBase64EncodedString(fileName, chunk));
        var chunkResults = (await Task.WhenAll(chunkTasks)).SelectMany(s => s).ToList();

        var descriptions = await CreateMemoryDescriptionsFromSummaries(chunkResults);

        string description = await _augmentationService.GenerateStoreDescriptionAsync(storeId, descriptions.Values.ToList());
        StoreDescription = description;
        var targetStore = new MemoryStore()
        {
            StoreId = Guid.NewGuid(),
            Title = FileProcessingHelper.SanitizeFileName(Path.GetFileNameWithoutExtension(fileName)),
            Description = description,
            OwnerUser = user,
            UserId = user.Id
        };

        Log.Information("[File Processor] The model generated {ContentsCount} individual memory chunks out of the file. The pure length shrunk from {Base64StringLength} characters to {Length} characters.", chunkResults.Count, base64String.Length, chunkResults.Sum(s => s.Summary.Length));
        Message = $"Finished chunking! Starting to create memories... Chunking and compression reduced the pure length of the file from {chunkResults.Sum(s => s.Summary.Length)} characters to {base64String.Length} characters.";

        var returnMemoryTasks = chunkResults.Select(async summary => await CreateReturnMemory(user, targetStore, descriptions, fileName, summary));
        var returnMemories = (await Task.WhenAll(returnMemoryTasks)).ToList();

        Log.Information("[File Processor] Finished preprocessing the contents after {TotalSeconds} seconds!", (DateTime.Now - start).TotalSeconds);

        targetStore.Memories = returnMemories;

        Result = targetStore;
        Completed = true;
        Progress = 1;
    }

    public async Task ExecuteFile(User user, string fileName, string fileContent)
    {
        Log.Information("[File Processor] Received a new file to process with name {FileName} and length {Base64StringLength}.", fileName, fileContent.Length);
        string storeId = $"project__{FileProcessingHelper.SanitizeFileName(Path.GetFileNameWithoutExtension(fileName))}";
        DateTime start = DateTime.Now;
        Log.Information("[File Processor] Starting file chunking...");
        Message = "Starting file chunking...";

        int pageSize = 1800;
        int chunkSize = pageSize * 5;
        int chunkOverlap = pageSize;
        var chunkIndices = FileProcessingHelper.CreateChunks(fileContent.Length, chunkSize, chunkOverlap);
        var chunks = new List<string>();
        foreach (var chunk in chunkIndices)
        {
            chunks.Add(new string(fileContent.Skip(chunk.start).Take(chunk.end - chunk.start).ToArray()));
            Progress += 0.1 / chunkIndices.Count;
        }
        Log.Information("[File Processor] Finished file chunking, created {Count} chunks!", chunkIndices.Count);
        Message = "Finished file preparation. Starting model processing...";
        Progress = 0.1;

        _chunksCount = chunks.Count;

        var chunkTasks = chunks.Select(async chunk => await CreateMemorySummaryFromRawString(fileName, chunk));
        var chunkResults = (await Task.WhenAll(chunkTasks)).SelectMany(s => s).ToList();

        var descriptions = await CreateMemoryDescriptionsFromSummaries(chunkResults);

        string description = await _augmentationService.GenerateStoreDescriptionAsync(storeId, descriptions.Values.ToList());
        StoreDescription = description;
        var targetStore = new MemoryStore
        {
            StoreId = Guid.NewGuid(),
            Title = FileProcessingHelper.SanitizeFileName(Path.GetFileNameWithoutExtension(fileName)),
            Description = description,
            OwnerUser = user,
            UserId = user.Id
        };

        Log.Information("[File Processor] The model generated {ContentsCount} individual memory chunks out of the file. The pure length shrunk from {Base64StringLength} characters to {Length} characters.", chunkResults.Count, fileContent.Length, chunkResults.Sum(s => s.Summary.Length));
        Message = $"Finished chunking! Starting to create memories... Chunking and compression reduced the pure length of the file from {chunkResults.Sum(s => s.Summary.Length)} characters to {fileContent.Length} characters.";

        var returnMemoryTasks = chunkResults.Select(async summary => await CreateReturnMemory(user, targetStore, descriptions, fileName, summary));
        var returnMemories = (await Task.WhenAll(returnMemoryTasks)).ToList();

        targetStore.Memories = returnMemories;

        Log.Information("[File Processor] Finished preprocessing the contents after {TotalSeconds} seconds!", (DateTime.Now - start).TotalSeconds);
        Result = targetStore;


        Completed = true;
        Progress = 100;
    }

    private async Task<IEnumerable<MemorySummary>> CreateMemorySummaryFromBase64EncodedString(string fileName, string base64EncodedChunk)
    {
        var returnSummaries = new List<MemorySummary>();
        DateTime chunkStart = DateTime.Now;
        Log.Information("[File Processor] Processing chunk {ChunkIndex} / {TotalChunks}.", _chunksFinished + 1,
            _chunksCount);
        var response =
            await _fileMemoryCreationService.CreateMemoriesFromPdfFileAsync(fileName, base64EncodedChunk);
        Log.Information("[File Processor] Chunk {ChunkIndex} processed after {TotalSeconds} seconds.", _chunksFinished + 1,
            (DateTime.Now - chunkStart).TotalSeconds);
        foreach (var summary in response.Summaries)
        {
            returnSummaries.Add(summary);
        }
        Progress += 0.9 / _chunksCount;
        _chunksFinished++;
        Message =
            $"Finished chunk {_chunksFinished + 1} of {_chunksCount} after {(DateTime.Now - chunkStart).TotalSeconds:F1} seconds.";
        return returnSummaries;
    }

    private async Task<IEnumerable<MemorySummary>> CreateMemorySummaryFromRawString(string fileName, string rawString)
    {
        var returnSummaries = new List<MemorySummary>();
        DateTime chunkStart = DateTime.Now;
        Log.Information("[File Processor] Processing chunk {ChunkIndex} / {TotalChunks}.", _chunksFinished + 1, _chunksCount);
        if (string.IsNullOrWhiteSpace(rawString))
        {
            Log.Warning("[File Processor] Chunk {ChunkIndex} is empty, skipping.", _chunksFinished + 1);
            return returnSummaries;
        }

        var response = await _fileMemoryCreationService.CreateMemoriesFromBase64String(fileName, rawString);
        Log.Information("[File Processor] Chunk {ChunkIndex} processed after {TotalSeconds} seconds.", _chunksFinished + 1, (DateTime.Now - chunkStart).TotalSeconds);
        foreach (var summary in response.Summaries)
        {
            returnSummaries.Add(summary);
        }
        _chunksFinished++;
        Progress += 0.9 / _chunksCount;
        Message = $"Finished chunk {_chunksFinished + 1} of {_chunksCount} after {(DateTime.Now - chunkStart).TotalSeconds:F1} seconds.";
        return returnSummaries;
    }

    private async Task<Dictionary<string, string>> CreateMemoryDescriptionsFromSummaries(List<MemorySummary> summaries)
    {
        ConcurrentDictionary<string, string> descriptions = new();

        var summaryTasks = summaries.Select(async s =>
        {
            descriptions[s.Identifier] = await GenerateDescriptionFromSummary(s);
        });
        await Task.WhenAll(summaryTasks);
        return descriptions.ToDictionary(d => d.Key, d => d.Value);
    }

    private async Task<string> GenerateDescriptionFromSummary(MemorySummary summary)
    {
        return await _augmentationService.GenerateMemoryDescriptionAsync(summary.Summary);
    }

    private async Task<Memory> CreateReturnMemory(User currentUser, MemoryStore memoryStore, Dictionary<string, string> descriptions, string fileName, MemorySummary summary)
    {
        string description = descriptions.TryGetValue(summary.Identifier, out string? value) ? value : await _augmentationService.GenerateMemoryDescriptionAsync(summary.Summary);
        Memory mem = new()
        {
            StoreId = memoryStore.StoreId,
            Identifier = Guid.NewGuid(),
            Title = summary.Identifier,
            Description = description,
            Content = summary.Summary,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UnixEpoch,
            Reference = fileName,
            Owner = currentUser.Id,
            OwnerUser = currentUser,
            ReferenceType = (int)ReferenceType.Document,
            Tags = new List<string>()
        };
        return mem;
    }

    public string Message { get; private set; } = string.Empty;
    public bool Completed { get; private set; }
    public double Progress { get; private set; }
    public MemoryStore? Result { get; private set; }
    public string StoreDescription { get; private set; } = string.Empty;
}
