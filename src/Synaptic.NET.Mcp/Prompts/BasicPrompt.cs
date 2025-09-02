using System.ComponentModel;
using Microsoft.Extensions.AI;
using ModelContextProtocol.Server;

namespace Synaptic.NET.Mcp.Prompts;

/// <summary>
/// Provides an example for a basic prompt without parameters or dependency injection.
/// </summary>
[McpServerPromptType]
public static class BasicPrompt
{
    [McpServerPrompt(Name = "basic_prompt"), Description("An example prompt without arguments")]
    public static IEnumerable<ChatMessage> ComplexPrompt()
    {
        return [
            new ChatMessage(ChatRole.User,"This is a basic prompt without arguments that can be retrieved by LLMs via MCP.")
        ];
    }
}
