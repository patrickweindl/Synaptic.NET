using Serilog;
using Serilog.Events;
using Synaptic.NET.Augmentation;
using Synaptic.NET.Authentication;
using Synaptic.NET.Core;
using Synaptic.NET.Domain;
using Synaptic.NET.Mcp;
using Synaptic.NET.OpenAI;
using Synaptic.NET.RestApi;
using Synaptic.NET.Web;

namespace Synaptic.NET.AppHost;

public class SynapticAppHost
{
    public static void Main(string[] args)
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

        builder.ConfigureDomainServices(out var synapticSettings);
        builder.ConfigureOpenAiServices();
        builder.ConfigureCoreServices();
        builder.ConfigureAugmentationServices();

        builder.Services.AddRazorComponents()
            .AddInteractiveServerComponents()
            .AddAuthenticationStateSerialization(o => o.SerializeAllClaims = true);
        builder.Services.AddRazorPages();
        builder.Services.AddServerSideBlazor();
        builder.ConfigureMcpServices();
        builder.ConfigureRestServices();

        builder.ConfigureAuthenticationAndAuthorization(synapticSettings);

        var app = builder.Build();

        app.MapStaticAssets();
        app.ConfigureDomainApplication();
        app.ConfigureCoreApplication(synapticSettings);
        app.ConfigureAuthenticationAndAuthorizationAndMiddlewares();
        app.MapRazorComponents<SynapticWebApp>().AddInteractiveServerRenderMode();
        app.ConfigureMcpApplicationWithAuthorization();
        app.ConfigureRestServicesWithAuthorization();
        app.Run();
    }
}
