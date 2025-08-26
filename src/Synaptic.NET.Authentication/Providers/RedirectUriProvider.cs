using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using Synaptic.NET.Authentication.Resources;

namespace Synaptic.NET.Authentication.Providers;

public class RedirectUriProvider
{
    public RedirectUriProvider()
    {

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
