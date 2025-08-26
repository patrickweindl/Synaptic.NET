using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Hosting;

namespace Synaptic.NET.RestApi;

public static class RestServices
{
    public static IHostApplicationBuilder ConfigureRestServices(this IHostApplicationBuilder builder)
    {
        return builder;
    }

    public static WebApplication ConfigureRestServicesWithAuthorization(this WebApplication app)
    {
        return app;
    }
}
