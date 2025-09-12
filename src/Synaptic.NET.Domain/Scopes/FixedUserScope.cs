using Microsoft.Extensions.DependencyInjection;
using Synaptic.NET.Domain.Abstractions.Augmentation;
using Synaptic.NET.Domain.Abstractions.Management;
using Synaptic.NET.Domain.Abstractions.Storage;
using Synaptic.NET.Domain.Resources;
using Synaptic.NET.Domain.Resources.Management;

namespace Synaptic.NET.Domain.Scopes;

public class FixedUserScope : IDisposable, IAsyncDisposable
{
    private readonly IServiceScope _scope;
    public FixedUserScope(IServiceProvider serviceProvider, User user)
    {
        _scope = serviceProvider.CreateScope();
        CurrentUserService = _scope.ServiceProvider.GetRequiredService<ICurrentUserService>();
        CurrentUserService.SetCurrentUserAsync(user);
        DbContext = _scope.ServiceProvider.GetRequiredService<SynapticDbContext>();
        DbContext.SetCurrentUser(user);
        MemoryAugmentationService = _scope.ServiceProvider.GetRequiredService<IMemoryAugmentationService>();
        MemoryProvider = _scope.ServiceProvider.GetRequiredService<IMemoryProvider>();
        FileMemoryCreationService = _scope.ServiceProvider.GetRequiredService<IFileMemoryCreationService>();
        ArchiveService = _scope.ServiceProvider.GetRequiredService<IArchiveService>();
    }
    public ICurrentUserService CurrentUserService { get; }
    public SynapticDbContext DbContext { get; }
    public IMemoryAugmentationService MemoryAugmentationService { get; }
    public IMemoryProvider MemoryProvider { get; }
    public IFileMemoryCreationService FileMemoryCreationService { get; }
    public IArchiveService ArchiveService { get; }
    public void Dispose()
    {
        _scope.Dispose();
        DbContext.Dispose();
    }

    public async ValueTask DisposeAsync()
    {
        if (_scope is IAsyncDisposable scopeAsyncDisposable)
        {
            await scopeAsyncDisposable.DisposeAsync();
        }
        else
        {
            _scope.Dispose();
        }

        await DbContext.DisposeAsync();
    }
}
