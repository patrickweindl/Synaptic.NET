using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Synaptic.NET.Domain.Abstractions.Management;

namespace Synaptic.NET.Domain.Resources;

public class SynapticDbContextFactory : IDesignTimeDbContextFactory<SynapticDbContext>
{
    public SynapticDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<SynapticDbContext>();
        optionsBuilder.UseSqlite("Data Source=synaptic.db");

        return new SynapticDbContext(optionsBuilder.Options);
    }

    public SynapticDbContext CreateInMemoryDbContext(ICurrentUserService? currentUserService = null)
    {
        var optionsBuilder = new DbContextOptionsBuilder<SynapticDbContext>();
        optionsBuilder.UseInMemoryDatabase("InMemoryDbForTesting");
        return new SynapticDbContext(optionsBuilder.Options, currentUserService);
    }
}
