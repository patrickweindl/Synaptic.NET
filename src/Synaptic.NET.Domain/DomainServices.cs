using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Synaptic.NET.Domain.Providers;

namespace Synaptic.NET.Domain;

public static class DomainServices
{
    public static IHostApplicationBuilder ConfigureDomainServices(this IHostApplicationBuilder builder, out SynapticServerSettings configuration)
    {
        builder.Configuration.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);
        builder.Configuration.AddEnvironmentVariables();

        configuration = new SynapticServerSettings(builder.Configuration);
        builder.Services.AddSingleton(configuration);
        builder.Services.AddSingleton<IMetricsCollectorProvider, MetricsCollectorProvider>();

        return builder;
    }
}
