using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;
using Synaptic.NET.Domain.Constants;
using Synaptic.NET.Mcp.Tools;

namespace Synaptic.NET.Mcp;

public static class McpServices
{
    public static IHostApplicationBuilder ConfigureMcpServices(
        this IHostApplicationBuilder builder,
        Func<IEnumerable<McpServerTool>>? customTools = null,
        Func<IEnumerable<McpServerResource>>? customResources = null,
        Func<IEnumerable<McpServerPrompt>>? customPrompts = null)
    {
        List<McpServerTool> additionalTools = [new NonStaticToolExample()];

        if (customTools != null)
        {
            additionalTools.AddRange(customTools.Invoke());
        }

        List<McpServerResource> additionalResources = new();
        if (customResources != null)
        {
            additionalResources.AddRange(customResources.Invoke());
        }

        List<McpServerPrompt> additionalPrompts = new();
        if (customPrompts != null)
        {
            additionalPrompts.AddRange(customPrompts.Invoke());
        }

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
            .WithTools(additionalTools)
            .WithResourcesFromAssembly()
            .WithResources(additionalResources)
            .WithPromptsFromAssembly()
            .WithPrompts(additionalPrompts);

        return builder;
    }

    public static WebApplication ConfigureMcpApplicationWithAuthorization(this WebApplication app)
    {
        app.MapMcp("/mcp");
        return app;
    }
}
