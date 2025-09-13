using System.Diagnostics;
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
        if (Debugger.IsAttached)
        {
            Console.WriteLine($"Request to {context.Request.Path} with method {context.Request.Method}.");
        }
        await next();
        if (isAuthorizedEndpoint && context.Response.StatusCode >= 400)
        {
            Log.Warning(
                "An authorized request failed: {Method} {Path} \u2192 {StatusCode}",
                context.Request.Method,
                context.Request.Path,
                context.Response.StatusCode);
        }
    };
}
