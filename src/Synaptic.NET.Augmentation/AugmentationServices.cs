using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Synaptic.NET.Augmentation.Services;
using Synaptic.NET.Domain.Abstractions.Augmentation;
using Synaptic.NET.Domain.Abstractions.Storage;

namespace Synaptic.NET.Augmentation;

public static class AugmentationServices
{
    public static IHostApplicationBuilder ConfigureAugmentationServices(this IHostApplicationBuilder builder)
    {
        builder.Services.AddScoped<IMemoryQueryResultReranker, MemoryQueryResultReranker>();
        builder.Services.AddScoped<IMemoryStoreRouter, WeightedMemoryStoreRouter>();
        builder.Services.AddScoped<IMemoryAugmentationService, MemoryAugmentationService>();
        builder.Services.AddScoped<IFileMemoryCreationService, FileMemoryCreationService>();
        return builder;
    }
}
