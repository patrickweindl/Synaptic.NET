using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;
using Synaptic.NET.Core.Extensions;
using Synaptic.NET.Core.Providers;
using Synaptic.NET.Core.Services;
using Synaptic.NET.Domain.Abstractions.Management;
using Synaptic.NET.Domain.Abstractions.Services;
using Synaptic.NET.Domain.Abstractions.Storage;
using Synaptic.NET.Domain.Resources.Configuration;
using Synaptic.NET.Domain.Scopes;

namespace Synaptic.NET.Core;

public static class CoreServices
{
    public static IHostApplicationBuilder ConfigureCoreServices(this IHostApplicationBuilder builder)
    {
        builder.Logging.AddOpenTelemetry(logging =>
        {
            logging.IncludeFormattedMessage = true;
            logging.IncludeScopes = true;
        });

        builder.Services.AddOpenTelemetry()
            .WithMetrics(metrics =>
            {
                metrics.AddAspNetCoreInstrumentation()
                    .AddHttpClientInstrumentation()
                    .AddRuntimeInstrumentation()
                    .AddMeter("Synaptic.Api.TokenMeter")
                    .AddMeter("Synaptic.Api.BenchmarkMeter")
                    .AddPrometheusExporter();
            })
            .WithTracing(tracing =>
            {
                tracing.AddAspNetCoreInstrumentation()
                    .AddHttpClientInstrumentation();
            });

        builder.Services.AddHealthChecks()
            .AddCheck("self", () => HealthCheckResult.Healthy(), ["live"]);

        builder.Services.AddSingleton<IMetricsCollectorProvider, MetricsCollectorProvider>();
        builder.Services.AddSingleton(s => new ScopeFactory(s));
        builder.Services.AddScoped<IUserManager, UserManager>();
        builder.Services.AddScoped<IEncryptionService, ClaimsBasedEncryptionService>();
        builder.Services.AddScoped<IArchiveService, ArchiveService>();
        builder.Services.AddScoped<IMemoryProvider, HybridMemoryProvider>();
        builder.Services.AddSingleton<IBackgroundTaskQueue, BackgroundTaskQueue>();
        builder.Services.AddHostedService<BackgroundTaskService>();
        return builder;
    }

    public static WebApplication ConfigureCoreApplication(this WebApplication app)
    {
        app.ConfigureHeaderForwarding();
        app.MapHealthChecks("/health");
        app.MapHealthChecks("/alive", new HealthCheckOptions
        {
            Predicate = r => r.Tags.Contains("live")
        });
        app.UseOpenTelemetryPrometheusScrapingEndpoint();
        return app;
    }
}
