using System.Net;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.Extensions.DependencyInjection;
using Synaptic.NET.Domain.Resources.Configuration;

namespace Synaptic.NET.Core.Extensions;

public static class HeaderForwarding
{
    internal static void ConfigureHeaderForwarding(this WebApplication app)
    {
        var settings = app.Services.GetRequiredService<SynapticServerSettings>();
        List<string> knownProxies = settings.ServerSettings.KnownProxies;
        List<IPAddress> knownProxyIps = knownProxies.Select(IPAddress.Parse).ToList();
        if (knownProxies.Count > 0)
        {
            var forwardHeaderOptions = new ForwardedHeadersOptions
            {
                ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
            };
            foreach (var knownProxyIp in knownProxyIps)
            {
                forwardHeaderOptions.KnownProxies.Add(knownProxyIp);
            }

            Log.Information("[Configuration] Using forwarded headers with known proxies: {Proxies}.", string.Join(",", forwardHeaderOptions.KnownProxies.Select(i => i.ToString())));
            app.UseForwardedHeaders(forwardHeaderOptions);
        }
    }
}
