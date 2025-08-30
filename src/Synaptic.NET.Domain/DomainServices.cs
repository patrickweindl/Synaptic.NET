using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Synaptic.NET.Domain.Providers;
using Synaptic.NET.Domain.Resources;

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

        Directory.CreateDirectory(configuration.BaseDataPath);
        string basePath = configuration.BaseDataPath;
        builder.Services.AddDbContext<SynapticDbContext>(options =>
            options.UseSqlite($"Data Source={Path.Join(basePath, "synaptic.db")}"));

        return builder;
    }

    public static WebApplication ConfigureDomainApplication(this WebApplication app)
    {
        using var scope = app.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<SynapticDbContext>();
        db.Database.Migrate();

        return app;
    }
}
