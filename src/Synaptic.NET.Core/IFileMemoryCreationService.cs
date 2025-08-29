using Synaptic.NET.Domain.Resources;

namespace Synaptic.NET.Core;

/// <summary>
/// Represents a service responsible for creating <see cref="Synaptic.NET.Domain.Resources.Memory"/> instances from files.
/// </summary>
public interface IFileMemoryCreationService
{
    /// <summary>
    /// Acquires a file processor (<see cref="FileProcessor"/>) that can be used to process files in an observable manner.
    /// </summary>
    /// <returns>A <see cref="FileProcessor"/> instance.</returns>
    Task<FileProcessor> GetFileProcessor();

    /// <summary>
    /// Generate memories from a PDF file, as base 64 string.
    /// </summary>
    /// <param name="fileName">The PDF file name.</param>
    /// <param name="base64Pdf">The contents of the PDF as base64 string.</param>
    /// <returns>A collection of created memories as <see cref="MemorySummaries"/>.</returns>
    Task<MemorySummaries> CreateMemoriesFromPdfFileAsync(string fileName, string base64Pdf);

    /// <summary>
    /// Generate memories from a base64 string.
    /// </summary>
    /// <param name="fileName">The file name that was uploaded.</param>
    /// <param name="base64String">The contents of the uploaded file as a base64 string.</param>
    /// <returns>A collection of created memories as <see cref="MemorySummaries"/>.</returns>
    Task<MemorySummaries> CreateMemoriesFromBase64String(string fileName, string base64String);
}
