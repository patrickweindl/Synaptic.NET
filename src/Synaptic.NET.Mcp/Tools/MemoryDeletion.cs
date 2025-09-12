using System.ComponentModel;
using JetBrains.Annotations;
using ModelContextProtocol.Server;
using Synaptic.NET.Domain.Abstractions.Management;
using Synaptic.NET.Domain.Abstractions.Storage;
using Synaptic.NET.Domain.Constants;

namespace Synaptic.NET.Mcp.Tools;

/// <summary>
/// Provides tools for memory deletion, using Dependency Injection for the individual methods.
/// </summary>
[McpServerToolType]
[PublicAPI]
public static class MemoryDeletion
{
    [McpServerTool(Name = ToolConstants.DeleteMemoryToolName, Destructive = true, OpenWorld = false, Title = ToolConstants.DeleteMemoryToolTitle)]
    [Description(ToolConstants.DeleteMemoryToolDescription)]
    public static async Task<string> DeleteMemoryAsync(Guid identifier, ICurrentUserService currentUserService,
        IMemoryProvider memoryProvider)
    {
        currentUserService.LockoutUserIfGuestAsync();
        Log.Logger.Information("[MCP Tool Call] Delete memory");
        Log.Logger.Information("[MCP Tool Call] Current user: {CurrentUser}", currentUserService.GetUserIdentifierAsync());

        if (identifier == Guid.Empty)
        {
            Log.Logger.Error("Memory identifier cannot be null or empty.");
            return "Memory identifier cannot be null or empty.";
        }

        var success = await memoryProvider.DeleteMemoryEntryAsync(identifier);
        if (success)
        {
            Log.Logger.Information("Memory with identifier {Identifier} deleted successfully.", identifier);
        }
        else
        {
            Log.Logger.Warning("Failed to delete memory with identifier {Identifier}.", identifier);
        }

        return success
            ? $"Memory with identifier {identifier} deleted successfully."
            : $"Failed to delete memory with identifier {identifier}.";
    }

    [McpServerTool(Name = ToolConstants.DeleteStoreToolName, Destructive = true, OpenWorld = false, Title = ToolConstants.DeleteStoreToolTitle)]
    [Description(ToolConstants.DeleteStoreToolDescription)]
    public static async Task<string> DeleteStoreAsync(
        [Description("The identifier of the store to delete. This should be a valid store identifier, such as 'user__various-thoughts'.")]
        Guid storeIdentifier,
        ICurrentUserService currentUserService,
        IMemoryProvider memoryProvider)
    {
        Log.Logger.Information("[MCP Tool Call] Delete store");
        Log.Logger.Information("[MCP Tool Call] Current user: {CurrentUser}", currentUserService.GetUserIdentifierAsync());
        if (storeIdentifier == Guid.Empty)
        {
            Log.Logger.Error("Store identifier cannot be null or empty.");
            return "Store identifier cannot be null or empty.";
        }
        var success = await memoryProvider.DeleteCollectionAsync(storeIdentifier);
        return success
            ? $"Store with identifier {storeIdentifier} deleted successfully."
            : $"Failed to delete memory with identifier {storeIdentifier}.";
    }
}
