using System.ComponentModel;
using System.Text.Json;
using System.Text.Json.Serialization;
using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore;
using ModelContextProtocol.Server;
using Synaptic.NET.Domain.Abstractions.Management;
using Synaptic.NET.Domain.Abstractions.Storage;
using Synaptic.NET.Domain.Resources;
using Synaptic.NET.Domain.Resources.Storage;

namespace Synaptic.NET.Mcp.Tools;

[McpServerToolType]
[PublicAPI]
public static class ChatGptTools
{
    [McpServerTool(Name = "search", Title = "Search resources with a free text query.")]
    [PublicAPI]
    public static async Task<string> Search(
        [Description("The free text query.")] string query,
        ICurrentUserService currentUserService,
        IMemoryProvider memoryProvider)
    {
        await currentUserService.LockoutUserIfGuestAsync();
        Log.Logger.Information("[MCP Tool Call] Query: {Query}", query);
        Log.Logger.Information("[MCP Tool Call] Current user: {CurrentUser}", await currentUserService.GetUserIdentifierAsync());
        var memoryQueryResults =
            await memoryProvider.SearchAsync(query);
        var memories = await memoryQueryResults.Results.Select(m => new ContextMemory(m.Memory)).ToListAsync();

        return JsonSerializer.Serialize(memories);
    }

    [McpServerTool(Name = "fetch")]
    [PublicAPI]
    public static async Task<string> Fetch([Description("The unique identifier of the resource to fetch.")] string id,
        ICurrentUserService currentUserService,
        IDbContextFactory<SynapticDbContext> dbContextFactory)
    {
        await currentUserService.LockoutUserIfGuestAsync();
        await using var dbContext = await dbContextFactory.CreateDbContextAsync();
        await dbContext.SetCurrentUserAsync(currentUserService);
        if (await dbContext.Memories.FirstOrDefaultAsync(m => m.Identifier.ToString() == id) is { } memory)
        {
            return JsonSerializer.Serialize(memory);
        }

        return string.Empty;
    }
}
