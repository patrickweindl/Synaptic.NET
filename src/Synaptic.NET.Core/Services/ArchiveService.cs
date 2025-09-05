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
        var currentUser = _currentUserService.GetCurrentUser() as IManagedIdentity;
        foreach (var file in Directory.GetFiles(currentUser.GetStorageDirectory(_settings)))
        {
            var dictionary = _files.GetOrAdd(currentUser.Identifier, _ => new ConcurrentDictionary<string, string>());
            dictionary.AddOrUpdate(Path.GetFileName(file), file, (_, _) => file);
        }
    }

    public async Task SaveFileAsync(string fileName, Stream content)
    {
        var currentUser = _currentUserService.GetCurrentUser() as IManagedIdentity;
        string filePath = Path.Join(currentUser.GetStorageDirectory(_settings), fileName);
        await using var fs = new FileStream(filePath, FileMode.Create, FileAccess.Write);
        await content.CopyToAsync(fs);
        var dictionary = _files.GetOrAdd(currentUser.Identifier, _ => new ConcurrentDictionary<string, string>());
        dictionary.AddOrUpdate(fileName, filePath, (_, _) => filePath);
    }

    public Task<Stream> GetFileAsync(string fileName)
    {
        var currentUser = _currentUserService.GetCurrentUser() as IManagedIdentity;
        string filePath = Path.Join(currentUser.GetStorageDirectory(_settings), fileName);
        return Task.FromResult<Stream>(new FileStream(filePath, FileMode.Open, FileAccess.Read));
    }

    public Task DeleteFileAsync(string fileName)
    {
        var currentUser = _currentUserService.GetCurrentUser() as IManagedIdentity;
        string filePath = Path.Join(currentUser.GetStorageDirectory(_settings), fileName);
        File.Delete(filePath);
        var dictionary = _files.GetOrAdd(currentUser.Identifier, _ => new ConcurrentDictionary<string, string>());
        dictionary.TryRemove(fileName, out _);
        return Task.CompletedTask;
    }

    public Task<IEnumerable<string>> GetFilesAsync()
    {
        string currentUser = _currentUserService.GetUserIdentifier();
        return Task.FromResult<IEnumerable<string>>(_files[currentUser].Keys.ToList());
    }
}
