using System.ComponentModel;
using System.Text.Json;
using JetBrains.Annotations;
using ModelContextProtocol.Server;
using Synaptic.NET.Core;

namespace Synaptic.NET.Mcp.Tools;

/// <summary>
/// Provides tools for memory acquisition, using Dependency Injection for the individual methods.
/// </summary>
[McpServerResourceType]
[PublicAPI]
public static class MemoryAcquisition
{
    [McpServerResource(Name = "FreeTextSearchResource", MimeType = "application/json")]
    [Description("Search memories with a free text query.")]
    public static async Task<string> FreeTextSearchAsync(
        [Description("The free text query.")] string query,
        [Description("The maximum count of memories to return")] int limit,
        [Description("The minimum relevance of the memory, between 0.0 und 1.0")] double relevance,
        ICurrentUserService currentUserService,
        IMemoryProvider memoryProvider)
    {
        Log.Logger.Information("[MCP Tool Call] Query: {Query}", query);
        Log.Logger.Information("[MCP Tool Call] Current user: {CurrentUser}", currentUserService.GetUserIdentifier());
        var memoryQueryResults =
            await memoryProvider.SearchAsync(query, limit, relevance);
        var memories = memoryQueryResults.Select(m => m.Memory).ToList();
        return JsonSerializer.Serialize(memories);
    }

    [McpServerResource(Name = "GetPinnedMemoriesResource", MimeType = "application/json")]
    [Description("Retrieve all pinned memories")]
    public static async Task<string> GetPinnedMemoriesAsync(ICurrentUserService currentUserService, IMemoryProvider memoryProvider)
    {
        Log.Logger.Information("[MCP Tool Call] Get pinned memories");
        Log.Logger.Information("[MCP Tool Call] Current user: {CurrentUser}", currentUserService.GetUserIdentifier());
        var stores = await memoryProvider.GetStoresAsync();
        var pinnedMemories = stores.SelectMany(s => s.Memories).Where(m => m.Pinned).ToList();
        return JsonSerializer.Serialize(pinnedMemories);
    }

    [McpServerResource(Name = "GetStoreIdentifiersandDescriptionsResource", MimeType = "application/json")]
    [Description("Retrieve memory store identifiers with short descriptions")]
    public static async Task<string> GetMemoryStoreIdentifiersAndDescriptions(
        ICurrentUserService currentUserService, IMemoryProvider memoryProvider)
    {
        Log.Logger.Information("[MCP Tool Call] Get memory store identifiers and descriptions");
        Log.Logger.Information("[MCP Tool Call] Current user: {CurrentUser}", currentUserService.GetUserIdentifier());
        return JsonSerializer.Serialize(await memoryProvider.GetStoreIdentifiersAndDescriptionsAsync());
    }
}
