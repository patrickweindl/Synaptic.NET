using Microsoft.EntityFrameworkCore;
using Synaptic.NET.Domain.Resources;

namespace Synaptic.NET.Domain;

public class InMemorySynapticDbContextFactory : IDbContextFactory<SynapticDbContext>
{
    public SynapticDbContext CreateDbContext()
    {
        var optionsBuilder = new DbContextOptionsBuilder<SynapticDbContext>();
        optionsBuilder.UseInMemoryDatabase("InMemoryDbForTesting");
        return new SynapticDbContext(optionsBuilder.Options);
    }
}
