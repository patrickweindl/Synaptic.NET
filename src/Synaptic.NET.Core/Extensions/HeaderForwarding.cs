using System.Net;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.HttpOverrides;
using Synaptic.NET.Domain;

namespace Synaptic.NET.Core.Extensions;

public static class HeaderForwarding
{
    internal static void ConfigureHeaderForwarding(this WebApplication app, SynapticServerSettings configuration)
    {
        List<string> knownProxies = configuration.KnownProxies;
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
