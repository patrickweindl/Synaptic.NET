using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Synaptic.NET.Qdrant;

public static class QdrantServices
{
    public static IHostApplicationBuilder ConfigureQdrantServices(this IHostApplicationBuilder builder)
    {
        builder.Services.AddScoped<QdrantMemoryClient>();
        return builder;
    }
}
