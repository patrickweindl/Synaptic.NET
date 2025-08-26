using Microsoft.AspNetCore.Http;

namespace Synaptic.NET.Core;

public interface IAppMiddleware
{
    Func<HttpContext, Func<Task>, Task> Function { get; }
}
