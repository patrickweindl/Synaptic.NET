using System.Collections.Concurrent;
using Synaptic.NET.Core.Helpers;
using Synaptic.NET.Domain.Resources;
using UglyToad.PdfPig;
using UglyToad.PdfPig.Writer;

namespace Synaptic.NET.Core;

/// <summary>
/// A class representing the result of a file processing operation with observable progress and status message.
/// </summary>
public class FileProcessor
{
    private readonly IFileMemoryCreationService _fileMemoryCreationService;
    private readonly IMemoryAugmentationService _augmentationService;
    public FileProcessor(IFileMemoryCreationService fileMemoryCreationService, IMemoryAugmentationService augmentationService)
    {
        _fileMemoryCreationService = fileMemoryCreationService;
        _augmentationService = augmentationService;
    }
    public async Task ExecutePdfFile(string fileName, string filePath)
        {
            byte[] fileBytes = await File.ReadAllBytesAsync(filePath);
            string base64String = Convert.ToBase64String(fileBytes);
            await ExecutePdf(fileName, base64String);
        }

        public async Task ExecutePdf(string fileName, string base64String)
        {
            Log.Information("[File Processor] Received a new file to process with name {FileName} and length {Base64StringLength}.", fileName, base64String.Length);
            string storeId = $"project__{FileProcessingHelper.SanitizeFileName(Path.GetFileNameWithoutExtension(fileName))}";
            Guid storeIdentifier = Guid.NewGuid();
            ConcurrentBag<MemorySummary> summaries = new();
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

            Progress = 0.1;

            Message = "Finished file preparation. Starting model processing...";

            Log.Information("[File Processor] Finished file chunking, created {Count} chunks!", base64EncodedChunks.Count);

            int chunksFinished = 0;
            await Parallel.ForEachAsync(base64EncodedChunks, new ParallelOptions { MaxDegreeOfParallelism = 16 }, async (base64EncodedChunk, _) =>
            {
                DateTime chunkStart = DateTime.Now;
                Log.Information("[File Processor] Processing chunk {ChunkIndex} / {TotalChunks}.", chunksFinished + 1,
                    base64EncodedChunks.Count);
                var response =
                    await _fileMemoryCreationService.CreateMemoriesFromPdfFileAsync(fileName, base64EncodedChunk);
                Log.Information("[File Processor] Chunk {ChunkIndex} processed after {TotalSeconds} seconds.", chunksFinished + 1,
                    (DateTime.Now - chunkStart).TotalSeconds);
                foreach (var summary in response.Summaries)
                {
                    summaries.Add(summary);
                }
                Progress += 0.9 / base64EncodedChunks.Count;
                chunksFinished++;
                Message =
                    $"Finished chunk {chunksFinished + 1} of {base64EncodedChunks.Count} after {(DateTime.Now - chunkStart).TotalSeconds:F1} seconds.";
            });

            Log.Information("[File Processor] The model generated {ContentsCount} individual memory chunks out of the file. The pure length shrunk from {Base64StringLength} characters to {Length} characters.", summaries.Count, base64String.Length, summaries.Sum(s => s.Summary.Length));
            Message = $"Finished chunking! Starting to create memories... Chunking and compression reduced the pure length of the file from {summaries.Sum(s => s.Summary.Length)} characters to {base64String.Length} characters.";

            var returnMemories = new List<(string, Memory)>();
            var chunksList = summaries.ToList();
            await Parallel.ForEachAsync(chunksList, async (summary, _) =>
            {
                Memory mem = new()
                {
                    Identifier = Guid.NewGuid(),
                    Title = summary.Identifier,
                    Description = await _augmentationService.GenerateMemoryDescriptionAsync(summary.Summary),
                    Content = summary.Summary,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UnixEpoch,
                    StoreTitle = storeId,
                    StoreIdentifier = storeIdentifier,
                    Reference = fileName
                };
                returnMemories.Add((fileName, mem));
            });

            Log.Information("[File Processor] Finished preprocessing the contents after {TotalSeconds} seconds!", (DateTime.Now - start).TotalSeconds);
            Result = returnMemories;
            Completed = true;
            Progress = 1;
        }

        public async Task ExecuteFile(string fileName, string fileContent)
        {
            Log.Information("[File Processor] Received a new file to process with name {FileName} and length {Base64StringLength}.", fileName, fileContent.Length);
            string storeId = $"project__{FileProcessingHelper.SanitizeFileName(Path.GetFileNameWithoutExtension(fileName))}";
            Guid storeIdentifier = Guid.NewGuid();
            ConcurrentBag<MemorySummary> summaries = new();
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

            int chunksFinished = 0;
            await Parallel.ForEachAsync(chunks, async (chunk, _) =>
            {
                DateTime chunkStart = DateTime.Now;
                Log.Information("[File Processor] Processing chunk {ChunkIndex} / {TotalChunks}.", chunksFinished + 1, chunks.Count);
                if (string.IsNullOrWhiteSpace(chunk))
                {
                    Log.Warning("[File Processor] Chunk {ChunkIndex} is empty, skipping.", chunksFinished + 1);
                    return;
                }

                var response = await _fileMemoryCreationService.CreateMemoriesFromBase64String(fileName, chunk);
                Log.Information("[File Processor] Chunk {ChunkIndex} processed after {TotalSeconds} seconds.", chunksFinished + 1, (DateTime.Now - chunkStart).TotalSeconds);
                foreach (var summary in response.Summaries)
                {
                    summaries.Add(summary);
                }
                chunksFinished++;
                Progress += 0.9 / chunks.Count;
                Message = $"Finished chunk {chunksFinished + 1} of {chunks.Count} after {(DateTime.Now - chunkStart).TotalSeconds:F1} seconds.";
            });

            Log.Information("[File Processor] The model generated {ContentsCount} individual memory chunks out of the file. The pure length shrunk from {Base64StringLength} characters to {Length} characters.", summaries.Count, fileContent.Length, summaries.Sum(s => s.Summary.Length));
            Message = $"Finished chunking! Starting to create memories... Chunking and compression reduced the pure length of the file from {summaries.Sum(s => s.Summary.Length)} characters to {fileContent.Length} characters.";
            var returnMemories = new List<(string, Memory)>();
            var returnSummaries = summaries.ToList();
            await Parallel.ForEachAsync(returnSummaries, async (summary, _) =>
            {
                Memory mem = new()
                {
                    Identifier = Guid.NewGuid(),
                    Description = await _augmentationService.GenerateMemoryDescriptionAsync(summary.Summary),
                    Title = summary.Identifier,
                    Content = summary.Summary,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UnixEpoch,
                    StoreTitle = storeId,
                    StoreIdentifier = storeIdentifier,
                    Reference = fileName
                };
                returnMemories.Add((fileName, mem));
            });

            Log.Information("[File Processor] Finished preprocessing the contents after {TotalSeconds} seconds!", (DateTime.Now - start).TotalSeconds);
            Result = returnMemories;

            string description = await _augmentationService.GenerateStoreDescriptionAsync(storeId, returnMemories.Select(m => m.Item2).ToList());
            StoreDescription = description;
            Completed = true;
            Progress = 100;
        }

        public string Message { get; private set; } = string.Empty;
        public bool Completed { get; private set; }
        public double Progress { get; private set; }
        public List<(string referenceName, Memory memory)> Result { get; private set; } = new();
        public string StoreDescription { get; private set; } = string.Empty;
}
