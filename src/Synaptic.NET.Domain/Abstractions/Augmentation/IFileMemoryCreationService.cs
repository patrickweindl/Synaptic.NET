using Synaptic.NET.Domain.Resources;
using Synaptic.NET.Domain.Resources.Management;
using Synaptic.NET.Domain.Resources.Storage;
using Synaptic.NET.Domain.Scopes;
using Synaptic.NET.Domain.StructuredResponses;

namespace Synaptic.NET.Domain.Abstractions.Augmentation;

/// <summary>
/// Represents a service responsible for creating <see cref="Memory"/> instances from files.
/// </summary>
public interface IFileMemoryCreationService
{
    /// <summary>
    /// Acquires a file processor (<see cref="FileProcessor"/>) that can be used to process files in an observable manner.
    /// </summary>
    /// <returns>A <see cref="FileProcessor"/> instance.</returns>
    Task<FileProcessor> GetFileProcessor(ScopeFactory scopeFactory, User user);

    /// <summary>
    /// Generate memories from a PDF file, as base 64 string.
    /// </summary>
    /// <param name="fileName">The PDF file name.</param>
    /// <param name="reference">The reference to create memories for.</param>
    /// <returns>A collection of created memories as <see cref="MemorySummaries"/>.</returns>
    Task<MemorySummaries> CreateMemoriesFromPdfIngestionResult(string fileName, IngestionReference reference);

    /// <summary>
    /// Generate memories from a base64 string.
    /// </summary>
    /// <param name="fileName">The file name that was uploaded.</param>
    /// <param name="rawString">The contents of the uploaded file as a base64 string.</param>
    /// <returns>A collection of created memories as <see cref="MemorySummaries"/>.</returns>
    Task<MemorySummaries> CreateMemoriesFromRawString(string fileName, string rawString);
}
