using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using Synaptic.NET.Domain.Helpers;

namespace Synaptic.NET.Authentication.Providers;

public class CodeBasedAuthProvider
{
    public CodeBasedAuthProvider()
    {
        RecurringTask.Create(() =>
        {
            CodeIdentityProviders.Clear();
        }, TimeSpan.FromMinutes(10), CancellationToken.None);
    }
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
