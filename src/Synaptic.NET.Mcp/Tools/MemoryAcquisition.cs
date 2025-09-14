using System.ComponentModel;
using System.Text.Json;
using System.Text.Json.Serialization;
using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore;
using ModelContextProtocol.Server;
using Synaptic.NET.Domain.Abstractions.Management;
using Synaptic.NET.Domain.Abstractions.Storage;
using Synaptic.NET.Domain.Constants;
using Synaptic.NET.Domain.Resources;
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
        [Description("The memory query options, optional, defaults to search all available stores and personal memories.")] MemoryQueryOptions? options,
        ICurrentUserService currentUserService,
        IMemoryProvider memoryProvider)
    {
        await currentUserService.LockoutUserIfGuestAsync();
        Log.Logger.Information("[MCP Tool Call] Query: {Query}", query);
        Log.Logger.Information("[MCP Tool Call] Current user: {CurrentUser}", currentUserService.GetUserIdentifierAsync());
        var memoryQueryResults =
            await memoryProvider.SearchAsync(query, limit, relevance, options ?? MemoryQueryOptions.Default);
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
        await currentUserService.LockoutUserIfGuestAsync();
        Log.Logger.Information("[MCP Tool Call] Get pinned memories");
        Log.Logger.Information("[MCP Tool Call] Current user: {CurrentUser}", currentUserService.GetUserIdentifierAsync());
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
        await currentUserService.LockoutUserIfGuestAsync();
        Log.Logger.Information("[MCP Tool Call] Get memory store identifiers and descriptions");
        Log.Logger.Information("[MCP Tool Call] Current user: {CurrentUser}", currentUserService.GetUserIdentifierAsync());
        return JsonSerializer.Serialize(await memoryProvider.GetStoreIdentifiersAndDescriptionsAsync());
    }

    [McpServerTool(
        Name = "DeepResearchForReferences",
        Title = "Deep research for reference on memory",
        Destructive = false,
        ReadOnly = true)]
    [Description("Performs a deep search on references of memory identifiers to find original texts of reference carrying memories.")]
    public static async Task<string> GetOriginalTextForReference(
        List<Guid> memoryIdentifiers,
        ICurrentUserService currentUserService,
        IDbContextFactory<SynapticDbContext> dbContextFactory)
    {
        await currentUserService.LockoutUserIfGuestAsync();
        await using var dbContext = await dbContextFactory.CreateDbContextAsync();
        await dbContext.SetCurrentUserAsync(currentUserService);
        var memories = await dbContext.Memories.Where(m => memoryIdentifiers.Contains(m.Identifier)).ToListAsync();

        var returnList = new List<IngestionReferenceSearchResult>();

        foreach (var memory in memories)
        {
            if (Guid.TryParse(memory.Reference, out var referenceGuid))
            {
                var reference = await dbContext.IngestionReferences.FirstOrDefaultAsync(r => r.Id == referenceGuid);
                if (reference != null)
                {
                    returnList.Add(new IngestionReferenceSearchResult{ MemoryIdentifier = memory.Identifier, Reference = reference});
                }
            }
        }

        return JsonSerializer.Serialize(returnList);
    }

    public class IngestionReferenceSearchResult
    {
        [JsonPropertyName("memory_identifier")]
        public Guid MemoryIdentifier { get; set; }

        [JsonPropertyName("reference")]
        public IngestionReference? Reference { get; set; }
    }
}
