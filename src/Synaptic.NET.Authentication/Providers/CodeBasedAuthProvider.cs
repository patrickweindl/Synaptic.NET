using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;

namespace Synaptic.NET.Authentication.Providers;

public class CodeBasedAuthProvider
{
    public void AddCodeIdentityProvider(string code, string provider)
    {
        CodeIdentityProviders.TryAdd(code, provider);
    }

    public bool GetIdentityProviderByCode(string code, [MaybeNullWhen(false)] out string? provider)
    {
        return CodeIdentityProviders.TryRemove(code, out provider);
    }

    private ConcurrentDictionary<string, string> CodeIdentityProviders { get; } = new();
}
