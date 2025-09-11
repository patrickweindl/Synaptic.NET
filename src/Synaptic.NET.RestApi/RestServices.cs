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
        app.MapControllers().RequireAuthorization();
        return app;
    }

    public static WebApplication ConfigureRestServicesWithoutAuthorization(this WebApplication app)
    {
        app.MapControllers().AllowAnonymous();
        return app;
    }
}
