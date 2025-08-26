using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Serilog;
using Serilog.Events;
using Synaptic.NET.Authentication;
using Synaptic.NET.Core;
using Synaptic.NET.Domain;

namespace Synaptic.NET.AppHost;

public class SynapticAppHost
{
    public static void Main(string[] args)
    {
        Log.Logger = new LoggerConfiguration()
            .WriteTo.File(Path.Combine(AppContext.BaseDirectory, "logs", "Verbose", "VerboseLog.log"), rollingInterval: RollingInterval.Day, shared: true)
            .WriteTo.Logger(l => l.Filter.ByIncludingOnly(e => e.Level >= LogEventLevel.Debug).WriteTo.Console())
            .WriteTo.Logger(l => l.Filter.ByIncludingOnly(e => e.Level >= LogEventLevel.Information).WriteTo.File(Path.Combine(AppContext.BaseDirectory, "logs", "Filtered", "InfoLog.log"), rollingInterval: RollingInterval.Day, shared: true))
            .CreateLogger();

        WebApplicationBuilder builder = WebApplication.CreateBuilder(new WebApplicationOptions
        {
            Args = args,
            ContentRootPath = AppContext.BaseDirectory
        });

        builder.Services.AddRazorComponents()
            .AddInteractiveServerComponents()
            .AddAuthenticationStateSerialization(o => o.SerializeAllClaims = true);
        builder.WebHost.UseStaticWebAssets();

        builder.ConfigureDomainServices(out var synapticSettings);
        builder.ConfigureCoreServices();
        builder.ConfigureAuthenticationAndAuthorization(synapticSettings);

        var app = builder.Build();
        app.MapStaticAssets();

        app.ConfigureCoreApplication(synapticSettings);
        app.ConfigureAuthenticationAndAuthorizationAndMiddlewares();


        app.Run();
    }
}
