using System.ComponentModel;
using JetBrains.Annotations;
using ModelContextProtocol.Server;

namespace Synaptic.NET.ExampleConsumer.ExampleMcpTools;

[McpServerToolType]
[PublicAPI]
public static class InjectedMcpTool
{
    [McpServerTool(Name = "EchoTool", Title = "Echo Tool", Destructive = false, OpenWorld = true)]
    [Description("A simple tool that echoes the input string.")]
    public static string Echo(string input)
    {
        return $"Echo: {input}";
    }
}
