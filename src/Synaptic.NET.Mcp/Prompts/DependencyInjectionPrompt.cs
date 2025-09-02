using System.ComponentModel;
using JetBrains.Annotations;
using Microsoft.Extensions.AI;
using ModelContextProtocol.Server;
using Synaptic.NET.Core;

namespace Synaptic.NET.Mcp.Prompts;

/// <summary>
/// Provides a dependency injection using complex prompt as an example of how to create prompts with arguments.
/// </summary>
[McpServerPromptType]
[PublicAPI]
public static class DependencyInjectionPrompt
{
    [McpServerPrompt(Name = "dependency_injection_prompt"), Description("A DI using prompt with arguments")]
    public static IEnumerable<ChatMessage> ReturnCurrentUser(
        ICurrentUserService currentUserService)
    {
        return [
            new ChatMessage(ChatRole.User,$"The current user of the MCP server is {currentUserService.GetCurrentUser().DisplayName}"),
        ];
    }
}
