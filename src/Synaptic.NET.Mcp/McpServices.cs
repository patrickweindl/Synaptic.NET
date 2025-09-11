using System.Reflection;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;
using Synaptic.NET.Domain.Constants;
using Synaptic.NET.Mcp.Tools;

namespace Synaptic.NET.Mcp;

/// <summary>
/// Provides extension methods for configuring MCP-related services and application setup in a .NET web server.
/// </summary>
public static class McpServices
{
    /// <summary>
    /// Configures MCP services for the specified application builder, allowing customization through additional resource types, prompt types, and tool types.
    /// </summary>
    /// <param name="builder">The application builder to configure MCP services for.</param>
    /// <param name="additionalResourceTypes">An optional collection of additional resource types to include.</param>
    /// <param name="additionalPromptTypes">An optional collection of additional prompt types to include.</param>
    /// <param name="additionalToolTypes">An optional collection of additional tool types to include.</param>
    /// <returns>Returns the configured application builder instance.</returns>
    public static IHostApplicationBuilder ConfigureMcpServices(
        this IHostApplicationBuilder builder,
        IEnumerable<Type>? additionalResourceTypes = null,
        IEnumerable<Type>? additionalPromptTypes = null,
        IEnumerable<Type>? additionalToolTypes = null)
    {
        List<Type> tools = [typeof(StaticToolExample)];
        if (additionalToolTypes != null)
        {
            tools.AddRange(additionalToolTypes.Where(t => t.GetCustomAttribute<McpServerToolTypeAttribute>() != null));
        }
        List<Type> resources = new();
        if (additionalResourceTypes != null)
        {
            resources.AddRange(additionalResourceTypes.Where(t => t.GetCustomAttribute<McpServerResourceTypeAttribute>() != null));
        }
        List<Type> prompts = new();
        if (additionalPromptTypes != null)
        {
            prompts.AddRange(additionalPromptTypes.Where(t => t.GetCustomAttribute<McpServerPromptTypeAttribute>() != null));
        }

        builder.Services.AddMcpServer(o =>
            {
                o.ServerInfo = new Implementation
                {
                    Name = ServerDescriptions.McpServerName,
                    Title = ServerDescriptions.McpServerTitle,
                    Version = "1.0"
                };
                o.InitializationTimeout = TimeSpan.FromMinutes(10);
                o.ServerInstructions = ServerDescriptions.SynapticServerDescription;
            })
            .WithHttpTransport(o => o.IdleTimeout = TimeSpan.FromHours(1))
            .WithToolsFromAssembly()
            .WithResourcesFromAssembly()
            .WithPromptsFromAssembly()
            .WithTools(tools)
            .WithResources(resources)
            .WithPrompts(prompts);

        return builder;
    }

    public static WebApplication ConfigureMcpApplicationWithAuthorization(this WebApplication app)
    {
        app.MapMcp("/mcp").RequireAuthorization();
        return app;
    }

    public static WebApplication ConfigureMcpApplicationWithoutAuthorization(this WebApplication app)
    {
        app.MapMcp("/mcp");
        return app;
    }
}
