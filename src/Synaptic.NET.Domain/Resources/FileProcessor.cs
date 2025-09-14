using System.Collections.Concurrent;
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
public class FileProcessor
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
        await ExecutePdfAsync(user, fileName, base64String);
    }

    public class PdfChunkResult
    {
        public required IngestionReference OriginalChunk { get; set; }
        public List<(MemorySummary Summary, string Description)> Summaries { get; set; } = new();
    }

    public async Task ExecutePdfAsync(User user, string fileName, string base64String)
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

        Progress = 0.1;
        Message = $"Finished file preparation after {(DateTime.Now - start).TotalSeconds:F1} seconds. Starting model processing...";
        Log.Information("[File Processor] Finished file chunking, created {Count} chunks! Required {Duration} seconds.", chunks.Count, (DateTime.Now - start).TotalSeconds);

        var chunkTasks = chunks.Select(async chunk =>
        {
            try
            {
                return await CreateMemorySummariesFromPdfChunk(fileName, chunk);
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
            var pdfChunkResult = new PdfChunkResult { OriginalChunk = chunks.First(c => c.Id == result.ReferenceId) };
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
        List<PdfChunkResult> pdfChunkResults = enhancedResults.ToList();

        string description = await scope.MemoryAugmentationService.GenerateStoreDescriptionAsync(storeId, summaryDescriptions.Values.ToList());
        StoreDescription = description;
        var targetStore = new MemoryStore
        {
            StoreId = Guid.NewGuid(),
            Title = FileProcessingHelper.SanitizeFileName(Path.GetFileNameWithoutExtension(fileName)),
            Description = description,
            OwnerUser = user,
            UserId = user.Id
        };

        Log.Information("[File Processor] The model generated {ContentsCount} individual memory chunks out of the file. The pure length shrunk from {Base64StringLength} characters to {Length} characters.", chunkResults.SelectMany(s => s.Summaries).Count(), base64String.Length, chunkResults.SelectMany(s => s.Summaries).Sum(s => s.Summary.Length));
        Message = "Finished chunking! Starting to create memories...";

        List<Memory> returnMemories = new();
        foreach (var finalResult in pdfChunkResults)
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
        References = pdfChunkResults.Select(pdfChunkResult => pdfChunkResult.OriginalChunk);
        Completed = true;
        Progress = 1;
    }

    public async Task ExecuteFile(User user, string fileName, string fileContent)
    {
        await using var scope = await _scopeFactory.CreateFixedUserScopeAsync(_user);
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

        string description = await scope.MemoryAugmentationService.GenerateStoreDescriptionAsync(storeId, descriptions.Values.ToList());
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

        var returnMemoryTasks = chunkResults.Select(async summary => await CreateReturnMemory(targetStore, descriptions, fileName, summary));
        var returnMemories = (await Task.WhenAll(returnMemoryTasks)).ToList();

        targetStore.Memories = returnMemories;

        Log.Information("[File Processor] Finished preprocessing the contents after {TotalSeconds} seconds!", (DateTime.Now - start).TotalSeconds);
        Result = targetStore;

        Completed = true;
        Progress = 1;
    }

    private async Task<(Guid ReferenceId, IEnumerable<MemorySummary> Summaries)> CreateMemorySummariesFromPdfChunk(string fileName, IngestionReference chunk)
    {
        await using var scope = await _scopeFactory.CreateFixedUserScopeAsync(_user);
        var returnSummaries = new List<MemorySummary>();
        DateTime chunkStart = DateTime.Now;
        Log.Information("[File Processor] Processing chunk {ChunkIndex} / {TotalChunks}.", _chunksFinished + 1,
            _chunksCount);
        var response = await scope.FileMemoryCreationService.CreateMemoriesFromPdfIngestionResult(fileName, chunk);
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

    private async Task<IEnumerable<MemorySummary>> CreateMemorySummaryFromRawString(string fileName, string rawString)
    {
        await using var scope = await _scopeFactory.CreateFixedUserScopeAsync(_user);
        var returnSummaries = new List<MemorySummary>();
        DateTime chunkStart = DateTime.Now;
        Log.Information("[File Processor] Processing chunk {ChunkIndex} / {TotalChunks}.", _chunksFinished + 1, _chunksCount);
        if (string.IsNullOrWhiteSpace(rawString))
        {
            Log.Warning("[File Processor] Chunk {ChunkIndex} is empty, skipping.", _chunksFinished + 1);
            return returnSummaries;
        }

        var response = await scope.FileMemoryCreationService.CreateMemoriesFromBase64String(fileName, rawString);
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

    private async Task<Memory> CreateReturnMemory(MemoryStore memoryStore, Dictionary<string, string> descriptions, string fileName, MemorySummary summary)
    {
        await using var scope = await _scopeFactory.CreateFixedUserScopeAsync(_user);
        string description = descriptions.TryGetValue(summary.Identifier, out string? value) ? value : await scope.MemoryAugmentationService.GenerateMemoryDescriptionAsync(summary.Summary);
        Memory mem = new()
        {
            StoreId = memoryStore.StoreId,
            Identifier = Guid.NewGuid(),
            Title = summary.Identifier,
            Description = description,
            Content = summary.Summary,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            Reference = fileName,
            Owner = _user.Id,
            OwnerUser = _user,
            ReferenceType = (int)ReferenceType.Document,
            Tags = new List<string>()
        };
        return mem;
    }

    public string Message { get; private set; } = string.Empty;
    public bool Completed { get; private set; }
    public double Progress { get; private set; }
    public MemoryStore? Result { get; private set; }
    public IEnumerable<IngestionReference>? References { get; private set; }
    public string StoreDescription { get; private set; } = string.Empty;
}
