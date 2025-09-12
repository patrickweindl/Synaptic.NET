using System.Collections.Concurrent;
using Synaptic.NET.Domain.Abstractions.Management;
using Synaptic.NET.Domain.Abstractions.Storage;
using Synaptic.NET.Domain.Resources.Configuration;

namespace Synaptic.NET.Core.Services;

public class ArchiveService : IArchiveService
{
    private readonly ICurrentUserService _currentUserService;
    private readonly SynapticServerSettings _settings;
    private readonly ConcurrentDictionary<string, ConcurrentDictionary<string, string>> _files = new();

    public ArchiveService(ICurrentUserService currentUserService, SynapticServerSettings settings)
    {
        _currentUserService = currentUserService;
        _settings = settings;
        IndexExisting();
    }

    private void IndexExisting()
    {
        var currentUser = Task.Run(async () => await _currentUserService.GetCurrentUserAsync()).Result as IManagedIdentity;
        foreach (var file in Directory.GetFiles(currentUser.GetStorageDirectory(_settings)))
        {
            var dictionary = _files.GetOrAdd(currentUser.Identifier, _ => new ConcurrentDictionary<string, string>());
            dictionary.AddOrUpdate(Path.GetFileName(file), file, (_, _) => file);
        }
    }

    public async Task SaveFileAsync(string fileName, Stream content)
    {
        var currentUser = await _currentUserService.GetCurrentUserAsync() as IManagedIdentity;
        string filePath = Path.Join(currentUser.GetStorageDirectory(_settings), fileName);
        await using var fs = new FileStream(filePath, FileMode.Create, FileAccess.Write);
        await content.CopyToAsync(fs);
        var dictionary = _files.GetOrAdd(currentUser.Identifier, _ => new ConcurrentDictionary<string, string>());
        dictionary.AddOrUpdate(fileName, filePath, (_, _) => filePath);
    }

    public async Task<Stream> GetFileAsync(string fileName)
    {
        var currentUser = await _currentUserService.GetCurrentUserAsync() as IManagedIdentity;
        string filePath = Path.Join(currentUser.GetStorageDirectory(_settings), fileName);
        return new FileStream(filePath, FileMode.Open, FileAccess.Read);
    }

    public async Task DeleteFileAsync(string fileName)
    {
        var currentUser = await _currentUserService.GetCurrentUserAsync() as IManagedIdentity;
        string filePath = Path.Join(currentUser.GetStorageDirectory(_settings), fileName);
        File.Delete(filePath);
        var dictionary = _files.GetOrAdd(currentUser.Identifier, _ => new ConcurrentDictionary<string, string>());
        dictionary.TryRemove(fileName, out _);
    }

    public async Task<IEnumerable<string>> GetFilesAsync()
    {
        string currentUser = await _currentUserService.GetUserIdentifierAsync();
        return _files[currentUser].Keys.ToList();
    }
}
