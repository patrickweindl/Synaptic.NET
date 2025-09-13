using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using Synaptic.NET.Authentication.Resources;
using Synaptic.NET.Domain.Helpers;

namespace Synaptic.NET.Authentication.Providers;

public class RedirectUriProvider
{
    public RedirectUriProvider()
    {
        RecurringTask.Create(() =>
        {
            StateRedirectUris.Clear();
        }, TimeSpan.FromMinutes(10), CancellationToken.None);
    }

    public void AddRedirectUri(string state, RedirectSettings redirectSettings)
    {
        StateRedirectUris.TryAdd(state, redirectSettings);
    }

    public bool GetRedirectUri(string state, [MaybeNullWhen(false)] out RedirectSettings redirectSettings)
    {
        return StateRedirectUris.TryRemove(state, out redirectSettings);
    }

    private ConcurrentDictionary<string, RedirectSettings> StateRedirectUris { get; } = new();

}
