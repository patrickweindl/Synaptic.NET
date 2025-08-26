using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using ModelContextProtocol.Protocol;
using Synaptic.NET.Domain.Constants;

namespace Synaptic.NET.Mcp;

public static class McpServices
{
    public static IHostApplicationBuilder ConfigureMcpServices(this IHostApplicationBuilder builder)
    {
        builder.Services.AddMcpServer(o =>
            {
                o.ServerInfo = new Implementation
                {
                    Name = ServerDescriptions.McpServerName,
                    Title = ServerDescriptions.McpServerTitle,
                    Version = "1.0"
                };
                o.InitializationTimeout = TimeSpan.FromSeconds(15);
                o.ServerInstructions = ServerDescriptions.SynapticServerDescription;
            })
            .WithHttpTransport(o => o.IdleTimeout = TimeSpan.FromMinutes(45))
            .WithToolsFromAssembly()
            .WithResourcesFromAssembly()
            .WithPromptsFromAssembly();
        return builder;
    }

    public static WebApplication ConfigureMcpApplication(this WebApplication app)
    {
        app.MapMcp("/mcp").RequireAuthorization();
        return app;
    }
}
