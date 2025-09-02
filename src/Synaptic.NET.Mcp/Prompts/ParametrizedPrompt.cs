using System.ComponentModel;
using JetBrains.Annotations;
using Microsoft.Extensions.AI;
using ModelContextProtocol.Server;

namespace Synaptic.NET.Mcp.Prompts;

/// <summary>
/// Provides a parametrized prompt as an example of how to create prompts with arguments.
/// </summary>
[McpServerPromptType]
[PublicAPI]
public static class ParametrizedPrompt
{
    [McpServerPrompt(Name = "complex_prompt"), Description("A prompt with arguments")]
    public static IEnumerable<ChatMessage> ComplexPrompt(
        [Description("Temperature setting")]
        int temperature,
        [Description("The content to process")]
        string content,
        [Description("Output style")]
        string? style = null)
    {
        return [
            new ChatMessage(ChatRole.User,$"This is a complex prompt with arguments: temperature={temperature}, style={style}"),
            new ChatMessage(ChatRole.Assistant, "I understand. You've provided a complex prompt with temperature and style arguments. How would you like me to proceed?"),
            new ChatMessage(ChatRole.User, "Process the following with these parameters:" + content)
        ];
    }
}
