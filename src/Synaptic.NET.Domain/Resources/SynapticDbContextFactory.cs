using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;
using Synaptic.NET.Domain.Resources.Configuration;

namespace Synaptic.NET.Domain.Resources;

public class SynapticDbContextFactory : IDesignTimeDbContextFactory<SynapticDbContext>
{
    public SynapticDbContext CreateDbContext(string[] args)
    {
        IConfiguration configuration = new ConfigurationBuilder().AddJsonFile("appsettings.json", optional: true).AddEnvironmentVariables().Build();
        var synapticSettings = new SynapticServerSettings(configuration);
        var optionsBuilder = new DbContextOptionsBuilder<SynapticDbContext>().UseNpgsql($"" +
            $"Host={synapticSettings.ServerSettings.PostgresUrl};" +
            $"Port={synapticSettings.ServerSettings.PostgresPort};" +
            $"Database={synapticSettings.ServerSettings.PostgresDatabase};" +
            $"Username={synapticSettings.ServerSettings.PostgresUserName};" +
            $"Password={synapticSettings.ServerSettings.PostgresPassword}");
        return new SynapticDbContext(optionsBuilder.Options);
    }

    public SynapticDbContext CreateInMemoryDbContext()
    {
        var optionsBuilder = new DbContextOptionsBuilder<SynapticDbContext>();
        optionsBuilder.UseInMemoryDatabase("InMemoryDbForTesting");
        return new SynapticDbContext(optionsBuilder.Options);
    }
}
