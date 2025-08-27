using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Synaptic.NET.Augmentation.Services;
using Synaptic.NET.Core;

namespace Synaptic.NET.Augmentation;

public static class AugmentationServices
{
    public static IHostApplicationBuilder ConfigureAugmentationServices(this IHostApplicationBuilder builder)
    {
        builder.Services.AddScoped<IMemoryStoreRouter, WeightedMemoryStoreRouter>();
        builder.Services.AddScoped<IMemoryAugmentationService, MemoryAugmentationService>();

        return builder;
    }
}
