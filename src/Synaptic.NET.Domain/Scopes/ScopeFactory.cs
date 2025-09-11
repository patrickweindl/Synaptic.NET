using Microsoft.Extensions.DependencyInjection;
using Synaptic.NET.Domain.Abstractions.Services;
using Synaptic.NET.Domain.Resources.Management;

namespace Synaptic.NET.Domain.Scopes;

public class ScopeFactory
{
    private readonly IServiceProvider _serviceProvider;
    public ScopeFactory(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public IBackgroundTaskQueue GetBackgroundTaskQueue()
    {
        return _serviceProvider.GetRequiredService<IBackgroundTaskQueue>();
    }

    public FixedUserScope CreateFixedUserScope(User user)
    {
        return new FixedUserScope(_serviceProvider, user);
    }
}
