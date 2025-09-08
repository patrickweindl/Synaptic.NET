using System.ComponentModel;
using System.Text.Json;
using JetBrains.Annotations;
using ModelContextProtocol.Server;
using Synaptic.NET.Domain.Abstractions.Management;
using Synaptic.NET.Domain.Abstractions.Storage;

namespace Synaptic.NET.Mcp.Resources;

/// <summary>
/// Provides memories as resources, using Dependency Injection for the individual methods.
/// </summary>
[McpServerResourceType]
[PublicAPI]
public static class MemoryResources
{
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
