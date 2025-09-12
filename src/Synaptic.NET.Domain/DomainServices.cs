using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Synaptic.NET.Domain.Abstractions.Management;
using Synaptic.NET.Domain.Resources;
using Synaptic.NET.Domain.Resources.Configuration;

namespace Synaptic.NET.Domain;

public static class DomainServices
{
    public static IHostApplicationBuilder ConfigureDomainServices(this IHostApplicationBuilder builder, out SynapticServerSettings configuration, IConfiguration? configurationOverride = null)
    {
        builder.Configuration.AddJsonFile("appsettings.json", optional: true, reloadOnChange: true);
        builder.Configuration.AddEnvironmentVariables();

        if (configurationOverride != null)
        {
            builder.Configuration.AddConfiguration(configurationOverride);
        }

        configuration = new SynapticServerSettings(builder.Configuration);
        builder.Services.AddSingleton(configuration);

        Directory.CreateDirectory(configuration.BaseDataPath);
        var lambdaSettings = configuration;

        builder.Services.AddDbContextFactory<SynapticDbContext>(options =>
        {
            options.UseNpgsql($"" +
                              $"Host={lambdaSettings.ServerSettings.PostgresUrl};" +
                              $"Port={lambdaSettings.ServerSettings.PostgresPort};" +
                              $"Database={lambdaSettings.ServerSettings.PostgresDatabase};" +
                              $"Username={lambdaSettings.ServerSettings.PostgresUserName};" +
                              $"Password={lambdaSettings.ServerSettings.PostgresPassword}");
        });
        builder.Services.AddScoped(async sp =>
        {
            var factory = sp.GetRequiredService<IDbContextFactory<SynapticDbContext>>();
            var ctx = await factory.CreateDbContextAsync();
            var userService = sp.GetRequiredService<ICurrentUserService>();
            await ctx.SetCurrentUserAsync(userService);
            return ctx;
        });
        return builder;
    }

    public static WebApplication ConfigureDomainApplication(this WebApplication app)
    {
        using var scope = app.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<SynapticDbContext>();
        db.Database.EnsureCreated();

        return app;
    }
}
