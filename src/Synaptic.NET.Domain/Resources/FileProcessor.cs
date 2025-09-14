using System.Collections.Concurrent;
using Synaptic.NET.Domain.Chunkers;
using Synaptic.NET.Domain.Helpers;
using Synaptic.NET.Domain.Resources.Management;
using Synaptic.NET.Domain.Resources.Storage;
using Synaptic.NET.Domain.Scopes;
using Synaptic.NET.Domain.StructuredResponses;
using UglyToad.PdfPig;

namespace Synaptic.NET.Domain.Resources;

/// <summary>
/// A class representing the result of a file processing operation with observable progress and status message.
/// </summary>
public class FileProcessor : IDisposable
{
    private readonly ScopeFactory _scopeFactory;
    private readonly User _user;
    private int _chunksCount;
    private int _chunksFinished;

    public FileProcessor(ScopeFactory scopeFactory, User user)
    {
        _scopeFactory = scopeFactory;
        _user = user;
    }
    public async Task ExecutePdfFile(User user, string fileName, string filePath)
    {
        byte[] fileBytes = await File.ReadAllBytesAsync(filePath);
        string base64String = Convert.ToBase64String(fileBytes);
        await ExecutePdfAsync(fileName, base64String);
    }

    public class ChunkResult
    {
        public required IngestionReference OriginalChunk { get; set; }
        public List<(MemorySummary Summary, string Description)> Summaries { get; set; } = new();
    }

    public async Task ExecutePdfAsync(string fileName, string base64String)
    {
        await using var scope = await _scopeFactory.CreateFixedUserScopeAsync(_user);
        Log.Information("[File Processor] Received a new file to process with name {FileName} and length {Base64StringLength}.", fileName, base64String.Length);
        string storeId = $"project__{FileProcessingHelper.SanitizeFileName(Path.GetFileNameWithoutExtension(fileName))}";
        DateTime start = DateTime.Now;
        Log.Information("[File Processor] Starting file chunking...");
        Message = "Starting file chunking...";

        byte[] pdfBytes = Convert.FromBase64String(base64String);
        using var ms = new MemoryStream(pdfBytes);
        using var doc = PdfDocument.Open(ms);

        var chunks = SemanticPdfChunker.ChunkPdf(base64String, fileName);

        _chunksCount = chunks.Count;

        await GenerateResultsFromChunks(true, base64String.Length, start, chunks, fileName, storeId);
    }

    public async Task ExecuteFile(string fileName, string fileContent)
    {
        await using var scope = await _scopeFactory.CreateFixedUserScopeAsync(_user);
        Log.Information("[File Processor] Received a new file to process with name {FileName} and length {Base64StringLength}.", fileName, fileContent.Length);
        string storeId = $"project__{FileProcessingHelper.SanitizeFileName(Path.GetFileNameWithoutExtension(fileName))}";
        DateTime start = DateTime.Now;
        Log.Information("[File Processor] Starting file chunking...");
        Message = "Starting file chunking...";

        var chunks = SemanticTextFileChunker.ChunkFile(fileContent, fileName);
        _chunksCount = chunks.Count;

        await GenerateResultsFromChunks(false, fileContent.Length, start, chunks, fileName, storeId);
    }

    private async Task GenerateResultsFromChunks(bool base64EncodedPdf, int originalContentLength, DateTime start, List<IngestionReference> chunks, string fileName, string storeId)
    {
        await using var scope = await _scopeFactory.CreateFixedUserScopeAsync(_user);
        Progress = 0.1;
        Message = $"Finished file preparation and chunking after {(DateTime.Now - start).TotalSeconds:F1} seconds. Starting model processing...";
        Log.Information("[File Processor] Finished file chunking, created {Count} chunks! Required {Duration} seconds.", chunks.Count, (DateTime.Now - start).TotalSeconds);

        var chunkTasks = chunks.Select(async chunk =>
        {
            try
            {
                return await CreateMemorySummariesFromChunkResult(base64EncodedPdf, fileName, chunk);
            }
            catch (Exception e)
            {
                Log.Logger.Error(e, "Error during memory creation from chunks!");
                return (chunk.Id, new List<MemorySummary>());
            }
        });
        var chunkResults = (await Task.WhenAll(chunkTasks)).Select(s => s).ToList();

        ConcurrentDictionary<string, string> summaryDescriptions = new();

        var resultTasks = chunkResults.Select(async result =>
        {
            await using var taskScope = await _scopeFactory.CreateFixedUserScopeAsync(_user);
            var descriptions = await CreateMemoryDescriptionsFromSummaries(result.Summaries);
            var pdfChunkResult = new ChunkResult { OriginalChunk = chunks.First(c => c.Id == result.ReferenceId) };
            foreach (var summary in result.Summaries)
            {
                string summaryDescription = descriptions.TryGetValue(summary.Identifier, out string? value)
                    ? value
                    : await taskScope.MemoryAugmentationService.GenerateMemoryDescriptionAsync(summary.Summary);
                summaryDescriptions.TryAdd(summary.Identifier, summaryDescription);
                pdfChunkResult.Summaries.Add((summary, summaryDescription));
            }

            return pdfChunkResult;
        });
        var enhancedResults = await Task.WhenAll(resultTasks);
        List<ChunkResult> fileChunkResults = enhancedResults.ToList();

        string description = await scope.MemoryAugmentationService.GenerateStoreDescriptionAsync(storeId, summaryDescriptions.Values.ToList());
        StoreDescription = description;
        var targetStore = new MemoryStore
        {
            StoreId = Guid.NewGuid(),
            Title = FileProcessingHelper.SanitizeFileName(Path.GetFileNameWithoutExtension(fileName)),
            Description = description,
            OwnerUser = _user,
            UserId = _user.Id
        };

        Log.Information("[File Processor] The model generated {ContentsCount} individual memory chunks out of the file. The pure length shrunk from {Base64StringLength} characters to {Length} characters.", chunkResults.SelectMany(s => s.Summaries).Count(), originalContentLength, chunkResults.SelectMany(s => s.Summaries).Sum(s => s.Summary.Length));
        Message = "Finished chunking! Starting to create memories...";

        List<Memory> returnMemories = new();
        foreach (var finalResult in fileChunkResults)
        {
            foreach (var summary in finalResult.Summaries)
            {
                var memoryToStore = new Memory()
                {
                    StoreId = targetStore.StoreId,
                    Identifier = Guid.NewGuid(),
                    Title = summary.Summary.Identifier,
                    Description = summary.Description,
                    Content = summary.Summary.Summary,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow,
                    Owner = _user.Id,
                    OwnerUser = _user,
                    ReferenceType = (int)ReferenceType.Document,
                    Reference = finalResult.OriginalChunk.Id.ToString(),
                    Tags = new List<string>()
                };
                returnMemories.Add(memoryToStore);
            }
        }
        Log.Information("[File Processor] Finished preprocessing the contents after {TotalSeconds} seconds! Created one store with {Memories} memories.", (DateTime.Now - start).TotalSeconds, returnMemories.Count);

        targetStore.Memories = returnMemories;

        Result = targetStore;
        References = fileChunkResults.Select(fileChunkResult => fileChunkResult.OriginalChunk);
        Completed = true;
        Progress = 1;
    }

    private async Task<(Guid ReferenceId, IEnumerable<MemorySummary> Summaries)> CreateMemorySummariesFromChunkResult(bool base64Encoded, string fileName, IngestionReference chunk)
    {
        await using var scope = await _scopeFactory.CreateFixedUserScopeAsync(_user);
        var returnSummaries = new List<MemorySummary>();
        DateTime chunkStart = DateTime.Now;
        Log.Information("[File Processor] Processing chunk {ChunkIndex} / {TotalChunks}.", _chunksFinished + 1,
            _chunksCount);
        MemorySummaries response;
        if (base64Encoded)
        {
            response = await scope.FileMemoryCreationService.CreateMemoriesFromPdfIngestionResult(fileName, chunk);
        }
        else
        {
            response = await scope.FileMemoryCreationService.CreateMemoriesFromRawString(fileName, chunk.OriginalText);
        }
        Log.Information("[File Processor] Chunk {ChunkIndex} processed after {TotalSeconds} seconds.", _chunksFinished + 1,
            (DateTime.Now - chunkStart).TotalSeconds);
        foreach (var summary in response.Summaries)
        {
            returnSummaries.Add(summary);
        }
        Progress += 0.9 / _chunksCount;
        Message =
            $"Finished chunk {_chunksFinished + 1} of {_chunksCount} after {(DateTime.Now - chunkStart).TotalSeconds:F1} seconds.";
        _chunksFinished++;
        return (chunk.Id, returnSummaries);
    }

    private async Task<Dictionary<string, string>> CreateMemoryDescriptionsFromSummaries(IEnumerable<MemorySummary> summaries)
    {
        ConcurrentDictionary<string, string> descriptions = new();

        await Parallel.ForEachAsync(summaries, async (summary, _) =>
        {
            await using var scope = await _scopeFactory.CreateFixedUserScopeAsync(_user);
            descriptions[summary.Identifier] = await scope.MemoryAugmentationService.GenerateMemoryDescriptionAsync(summary.Summary);
        });
        return descriptions.ToDictionary(d => d.Key, d => d.Value);
    }

    public string Message { get; private set; } = string.Empty;
    public bool Completed { get; private set; }
    public double Progress { get; private set; }
    public MemoryStore? Result { get; private set; }
    public IEnumerable<IngestionReference>? References { get; private set; }
    public string StoreDescription { get; private set; } = string.Empty;

    public void Dispose()
    {
        Result = null;
        References = null;
    }
}
