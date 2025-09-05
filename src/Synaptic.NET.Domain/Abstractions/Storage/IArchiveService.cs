namespace Synaptic.NET.Domain.Abstractions.Storage;

/// <summary>
/// A scoped service for archiving user data, e.g. uploaded files.
/// </summary>
public interface IArchiveService
{
    Task SaveFileAsync(string fileName, Stream content);

    Task<Stream> GetFileAsync(string fileName);

    Task DeleteFileAsync(string fileName);

    Task<IEnumerable<string>> GetFilesAsync();
}
