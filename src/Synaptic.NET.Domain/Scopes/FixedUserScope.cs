using Microsoft.EntityFrameworkCore;
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
    private readonly IDbContextFactory<SynapticDbContext> _dbContextFactory;
    private readonly User _user;
    private FixedUserScope(IServiceProvider serviceProvider, User user)
    {
        _scope = serviceProvider.CreateScope();
        CurrentUserService = _scope.ServiceProvider.GetRequiredService<ICurrentUserService>();
        MemoryAugmentationService = _scope.ServiceProvider.GetRequiredService<IMemoryAugmentationService>();
        MemoryProvider = _scope.ServiceProvider.GetRequiredService<IMemoryProvider>();
        FileMemoryCreationService = _scope.ServiceProvider.GetRequiredService<IFileMemoryCreationService>();
        _dbContextFactory = _scope.ServiceProvider.GetRequiredService<IDbContextFactory<SynapticDbContext>>();
        _user = user;
    }

    public static async Task<FixedUserScope> CreateFixedUserScopeAsync(IServiceProvider serviceProvider, User user)
    {
        var userScope = new FixedUserScope(serviceProvider, user);
        await userScope.CurrentUserService.SetCurrentUserAsync(user);
        await userScope.DbContextInstance.SetCurrentUserAsync(userScope.CurrentUserService);
        return userScope;
    }

    public ICurrentUserService CurrentUserService { get; }

    public SynapticDbContext DbContextInstance => Task.Run(async () =>
    {
        var context = await _dbContextFactory.CreateDbContextAsync();
        await context.SetCurrentUserAsync(_user);
        return context;
    }).Result;

    public IMemoryAugmentationService MemoryAugmentationService { get; }
    public IMemoryProvider MemoryProvider { get; }
    public IFileMemoryCreationService FileMemoryCreationService { get; }
    public void Dispose()
    {
        _scope.Dispose();
        DbContextInstance.Dispose();
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

        await DbContextInstance.DisposeAsync();
    }
}
