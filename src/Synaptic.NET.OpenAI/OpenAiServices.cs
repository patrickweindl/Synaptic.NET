using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Synaptic.NET.OpenAI;

public static class OpenAiServices
{
    public static IHostApplicationBuilder ConfigureOpenAiServices(this IHostApplicationBuilder builder)
    {
        builder.Services.AddSingleton<OpenAiClientFactory>();
        return builder;
    }
}
