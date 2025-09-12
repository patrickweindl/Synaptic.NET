using JetBrains.Annotations;
using ModelContextProtocol.Server;
using Synaptic.NET.Domain.Abstractions.Management;

namespace Synaptic.NET.Mcp.Tools;

/// <summary>
/// An example for a static tool that can get injected to the server on startup and is able to access services on the server via using DI's service provider.
/// On invocation, it will return the current local datetime of the server and the requesting username.
/// </summary>
[McpServerToolType]
[PublicAPI]
public static class StaticToolExample
{
    public class StaticToolExampleResult
    {
        public string Requestor { get; set; } = string.Empty;
        public DateTime CurrentTime { get; set; }
    }

    [McpServerTool(Name="GetCurrentTimeAndRequestor", Destructive = false, Idempotent = true, OpenWorld = false, Title = "Get current time and requestor")]
    [PublicAPI]
    public static async Task<StaticToolExampleResult> GetCurrentDateTime(ICurrentUserService currentUserService)
    {
        var currentUserId = await currentUserService.GetCurrentUserAsync();
        return new StaticToolExampleResult
        {
            Requestor = currentUserId.DisplayName, CurrentTime = DateTime.Now
        };
    }
}
