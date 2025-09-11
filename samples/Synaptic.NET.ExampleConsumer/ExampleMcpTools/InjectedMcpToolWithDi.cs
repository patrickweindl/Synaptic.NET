using System.ComponentModel;
using JetBrains.Annotations;
using ModelContextProtocol.Server;
using Synaptic.NET.Domain.Abstractions.Management;

namespace Synaptic.NET.ExampleConsumer.ExampleMcpTools;

[McpServerToolType]
[PublicAPI]
public static class InjectedMcpToolWithDi
{
    [McpServerTool(Name = "EchoUserNameTool", Title = "Echo User Name Tool", Destructive = false, OpenWorld = true)]
    [Description("A simple tool that echoes the input string.")]
    public static string Echo(string input, ICurrentUserService userService)
    {
        return $"Echo for {userService.GetCurrentUser().DisplayName}: {input}";
    }
}
