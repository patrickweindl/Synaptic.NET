using System.ComponentModel;
using System.Text.Json;
using JetBrains.Annotations;
using ModelContextProtocol.Server;
using Synaptic.NET.Domain.Abstractions.Management;
using Synaptic.NET.Domain.Abstractions.Storage;
using Synaptic.NET.Domain.Constants;
using Synaptic.NET.Domain.Resources.Storage;

namespace Synaptic.NET.Mcp.Tools;

/// <summary>
/// Provides tools for memory acquisition, using Dependency Injection for the individual methods.
/// </summary>
[McpServerToolType]
[PublicAPI]
public static class MemoryAcquisition
{
    [McpServerTool(
        Name = ToolConstants.GetMemoryToolName,
        Title = ToolConstants.GetMemoryToolTitle,
        Destructive = false,
        OpenWorld = false)]
    [Description(ToolConstants.GetMemoryToolDescription)]
    public static async Task<IEnumerable<ContextMemory>> FreeTextSearchAsync(
        [Description("The free text query.")] string query,
        [Description("The maximum count of memories to return")] int limit,
        [Description("The minimum relevance of the memory, between 0.0 und 1.0")] double relevance,
        ICurrentUserService currentUserService,
        IMemoryProvider memoryProvider)
    {
        currentUserService.LockoutUserIfGuest();
        Log.Logger.Information("[MCP Tool Call] Query: {Query}", query);
        Log.Logger.Information("[MCP Tool Call] Current user: {CurrentUser}", currentUserService.GetUserIdentifier());
        var memoryQueryResults =
            await memoryProvider.SearchAsync(query, limit, relevance);
        var memories = await memoryQueryResults.Results.Select(m => new ContextMemory(m.Memory)).ToListAsync();
        return memories;
    }

    [McpServerTool(
        Name = ToolConstants.GetCurrentlyRelevantMemoriesToolName,
        Title = ToolConstants.GetCurrentlyRelevantMemoriesToolTitle,
        Destructive = false,
        OpenWorld = false)]
    [Description(ToolConstants.GetCurrentlyRelevantMemoriesToolDescription)]
    public static async Task<IEnumerable<ContextMemory>> GetPinnedMemoriesAsync(ICurrentUserService currentUserService, IMemoryProvider memoryProvider)
    {
        currentUserService.LockoutUserIfGuest();
        Log.Logger.Information("[MCP Tool Call] Get pinned memories");
        Log.Logger.Information("[MCP Tool Call] Current user: {CurrentUser}", currentUserService.GetUserIdentifier());
        var stores = await memoryProvider.GetStoresAsync();
        var pinnedMemories = stores.SelectMany(s => s.Memories).Where(m => m.Pinned).Select(m => new ContextMemory(m)).ToList();
        return pinnedMemories;
    }

    [McpServerTool(
        Name = ToolConstants.GetStoreIdsAndDescriptionsToolName,
        Title = ToolConstants.GetStoreIdsAndDescriptionsToolTitle,
        Destructive = false,
        OpenWorld = false)]
    [Description(ToolConstants.GetStoreIdsAndDescriptionsToolDescription)]
    public static async Task<string> GetMemoryStoreIdentifiersAndDescriptions(
        ICurrentUserService currentUserService, IMemoryProvider memoryProvider)
    {
        currentUserService.LockoutUserIfGuest();
        Log.Logger.Information("[MCP Tool Call] Get memory store identifiers and descriptions");
        Log.Logger.Information("[MCP Tool Call] Current user: {CurrentUser}", currentUserService.GetUserIdentifier());
        return JsonSerializer.Serialize(await memoryProvider.GetStoreIdentifiersAndDescriptionsAsync());
    }
}
