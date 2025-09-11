using Serilog;
using Serilog.Events;
using Synaptic.NET.Augmentation;
using Synaptic.NET.Authentication;
using Synaptic.NET.Core;
using Synaptic.NET.Domain;
using Synaptic.NET.Mcp;
using Synaptic.NET.OpenAI;
using Synaptic.NET.Qdrant;
using Synaptic.NET.RestApi;
using Synaptic.NET.Web;

namespace Synaptic.NET.AppHost;

public static class SynapticAppHost
{
    public static void Main(string[] args)
    {
        var app = DefaultSynapticApplication(args);
        app.RunDefaultApplication();
    }

    public static void RunDefaultApplication(this WebApplication defaultApp, bool runWithoutAuthorization = false)
    {
        defaultApp.MapStaticAssets();
        defaultApp.ConfigureDomainApplication()
            .ConfigureCoreApplication();
        if (!runWithoutAuthorization)
        {
            defaultApp.ConfigureAuthenticationAndAuthorizationAndMiddlewares();
        }

        defaultApp.MapRazorComponents<SynapticWebApp>().AddInteractiveServerRenderMode();

        if (!runWithoutAuthorization)
        {
            defaultApp.ConfigureMcpApplicationWithAuthorization()
                .ConfigureRestServicesWithAuthorization();
        }
        else
        {
            defaultApp.ConfigureMcpApplicationWithoutAuthorization()
                .ConfigureRestServicesWithoutAuthorization();
        }

        defaultApp.Run();
    }

    public static WebApplication DefaultSynapticApplication(string[] args,
        bool runWithoutAuthorization = false,
        IConfiguration? configurationOverride = null,
        IEnumerable<Type>? additionalToolTypes = null,
        IEnumerable<Type>? additionalPromptTypes = null,
        IEnumerable<Type>? additionalResourceTypes = null)
    {
        Log.Logger = new LoggerConfiguration()
            .WriteTo.File(Path.Combine(AppContext.BaseDirectory, "logs", "Verbose", "VerboseLog.log"), rollingInterval: RollingInterval.Day, shared: true)
            .WriteTo.Logger(l => l.Filter.ByIncludingOnly(e => e.Level >= LogEventLevel.Debug).WriteTo.Console())
            .WriteTo.Logger(l => l.Filter.ByIncludingOnly(e => e.Level >= LogEventLevel.Information).WriteTo.File(Path.Combine(AppContext.BaseDirectory, "logs", "Filtered", "InfoLog.log"), rollingInterval: RollingInterval.Day, shared: true))
            .WriteTo.Logger(l => l.WriteTo.OpenTelemetry())
            .CreateLogger();

        WebApplicationBuilder builder = WebApplication.CreateBuilder(new WebApplicationOptions
        {
            Args = args,
            ContentRootPath = AppContext.BaseDirectory
        });

        builder
            .ConfigureDomainServices(out var synapticSettings);

        if (!runWithoutAuthorization)
        {
            builder.ConfigureAuthenticationAndAuthorization(synapticSettings);
        }
        builder
            .ConfigureOpenAiServices()
            .ConfigureQdrantServices()
            .ConfigureCoreServices()
            .ConfigureAugmentationServices()
            .ConfigureMcpServices(additionalResourceTypes, additionalPromptTypes, additionalToolTypes)
            .ConfigureRestServices();

        builder.Services.AddRazorComponents()
            .AddInteractiveServerComponents()
            .AddAuthenticationStateSerialization(o => o.SerializeAllClaims = true);
        builder.Services.AddRazorPages();
        builder.Services.AddServerSideBlazor();

        return builder.Build();
    }
}
