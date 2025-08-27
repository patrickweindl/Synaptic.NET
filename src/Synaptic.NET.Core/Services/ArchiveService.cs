using System.Collections.Concurrent;
using Synaptic.NET.Domain;
using Synaptic.NET.Domain.Resources;

namespace Synaptic.NET.Core.Services;

public class ArchiveService : IArchiveService
{
    private readonly IUserManager _userManager;
    private readonly ICurrentUserService _currentUserService;
    private readonly SynapticServerSettings _settings;
    private readonly ConcurrentDictionary<string, ConcurrentDictionary<string, string>> _files = new();

    public ArchiveService(IUserManager userManager, ICurrentUserService currentUserService, SynapticServerSettings settings)
    {
        _userManager = userManager;
        _currentUserService = currentUserService;
        _settings = settings;
        IndexExisting();
    }

    private void IndexExisting()
    {
        _ = _userManager.GetOrCreateUser(_currentUserService.GetUserClaimIdentity(), out User user);
        foreach (var file in Directory.GetFiles(user.GetStorageDirectory(_settings)))
        {
            var dictionary = _files.GetOrAdd(user.UserIdentifier, _ => new ConcurrentDictionary<string, string>());
            dictionary.AddOrUpdate(Path.GetFileName(file), file, (_, _) => file);
        }
    }

    public async Task SaveFileAsync(string fileName, Stream content)
    {
        _ = _userManager.GetOrCreateUser(_currentUserService.GetUserClaimIdentity(), out User user);
        string filePath = Path.Join(user.GetStorageDirectory(_settings), fileName);
        await using var fs = new FileStream(filePath, FileMode.Create, FileAccess.Write);
        await content.CopyToAsync(fs);
        var dictionary = _files.GetOrAdd(user.UserIdentifier, _ => new ConcurrentDictionary<string, string>());
        dictionary.AddOrUpdate(fileName, filePath, (_, _) => filePath);
    }

    public Task<Stream> GetFileAsync(string fileName)
    {
        _ = _userManager.GetOrCreateUser(_currentUserService.GetUserClaimIdentity(), out User user);
        string filePath = Path.Join(user.GetStorageDirectory(_settings), fileName);
        return Task.FromResult<Stream>(new FileStream(filePath, FileMode.Open, FileAccess.Read));
    }

    public Task DeleteFileAsync(string fileName)
    {
        _ = _userManager.GetOrCreateUser(_currentUserService.GetUserClaimIdentity(), out User user);
        string filePath = Path.Join(user.GetStorageDirectory(_settings), fileName);
        File.Delete(filePath);
        var dictionary = _files.GetOrAdd(user.UserIdentifier, _ => new ConcurrentDictionary<string, string>());
        dictionary.TryRemove(fileName, out _);
        return Task.CompletedTask;
    }

    public Task<IEnumerable<string>> GetFilesAsync()
    {
        string currentUser = _currentUserService.GetUserIdentifier();
        return Task.FromResult<IEnumerable<string>>(_files[currentUser].Keys.ToList());
    }
}
