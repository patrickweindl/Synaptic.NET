using System.ComponentModel;
using JetBrains.Annotations;
using ModelContextProtocol.Server;
using Synaptic.NET.Domain.Abstractions.Management;
using Synaptic.NET.Domain.Abstractions.Storage;
using Synaptic.NET.Domain.Constants;
using Synaptic.NET.Domain.Resources.Storage;

namespace Synaptic.NET.Mcp.Tools;

/// <summary>
/// Provides tools for memory replacements, using Dependency Injection for the individual methods.
/// </summary>
[McpServerToolType]
[PublicAPI]
public static class MemoryReplacement
{
    [McpServerTool(Name = ToolConstants.ReplaceMemoryToolName, Destructive = true, OpenWorld = false, Title = ToolConstants.ReplaceMemoryToolTitle)]
    [Description(ToolConstants.ReplaceMemoryToolDescription)]
    public static async Task<string> ReplaceMemoryAsync(
        [Description("The unique identifier of the memory to replace")]
        Guid memoryIdentifier,
        [Description("The updated title of the memory to replace. If it does not exist, a new memory will be created.")]
        string memoryTitle,
        [Description("A short description in one sentence (less than 35 tokens / 20 words) of the memory content.")]
        string memoryDescription,
        [Description("The new content of the memory as a logical unit. Should not exceed 250 tokens / 150 words. If the information can not be condensed to this size or multiple logically closed chunks need to be created, create multiple memories.")]
        string content,
        [Description("Whether the memory is important, hence marked as pinned.")]
        bool pinned,
        [Description("Optional: a list of tags for the memory, useful for later retrieval.")]
        List<string>? tags,
        ICurrentUserService currentUserService,
        IMemoryProvider memoryProvider)
    {
        Log.Logger.Information("[MCP Tool Call] Replace memory");
        Log.Logger.Information("[MCP Tool Call] Current user: {CurrentUser}", currentUserService.GetUserIdentifier());

        Memory newMemory = new()
        {
            Owner = currentUserService.GetCurrentUser().Id,
            Title = memoryTitle,
            Content = content,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UnixEpoch,
            Pinned = pinned,
            Tags = tags ?? new List<string>(),
            Description = memoryDescription
        };

        var replacement = await memoryProvider.ReplaceMemoryEntryAsync(memoryIdentifier, newMemory);

        if (replacement)
        {
            return $"Memory with identifier {memoryTitle} replaced successfully.";
        }
        Log.Logger.Error("Failed to replace or create memory with identifier {Identifier}.", memoryTitle);
        return "Failed to replace or create memory with identifier " + memoryTitle + ".";
    }
}
