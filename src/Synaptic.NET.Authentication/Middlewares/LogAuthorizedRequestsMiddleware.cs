using Microsoft.AspNetCore.Authorization;
using Synaptic.NET.Core;

namespace Synaptic.NET.Authentication.Middlewares;

public class LogAuthorizedRequestsMiddleware : IAppMiddleware
{
    public Func<HttpContext, Func<Task>, Task> Function => async (context, next) =>
    {
        Endpoint? endpoint = context.GetEndpoint();
        bool isAuthorizedEndpoint = endpoint?.Metadata.GetMetadata<IAuthorizeData>() != null &&
                                     endpoint?.Metadata.GetMetadata<AllowAnonymousAttribute>() == null;

        if (isAuthorizedEndpoint)
        {
            Log.Information("Authorized request received: {Method} {Path}",
                context.Request.Method,
                context.Request.Path);

            await next();

            if (context.Response.StatusCode >= 400)
            {
                Log.Warning(
                    "Authorized request failed: {Method} {Path} \u2192 {StatusCode}",
                    context.Request.Method,
                    context.Request.Path,
                    context.Response.StatusCode);
            }
        }
        else
        {
            await next();
        }
    };
}
