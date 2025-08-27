using Synaptic.NET.Domain.Resources;

namespace Synaptic.NET.Core;

/// <summary>
/// Represents a service responsible for creating <see cref="Synaptic.NET.Domain.Resources.Memory"/> instances from files.
/// </summary>
public interface IFileMemoryCreationService
{
    Task<FileProcessor> GetFileProcessor();

    Task<List<Memory>> CreateMemoriesFromPdfFileAsync(string fileName, string base64Pdf);

    Task<List<Memory>> CreateMemoriesFromTextFileAsync(string fileName, string fileContent);
}
