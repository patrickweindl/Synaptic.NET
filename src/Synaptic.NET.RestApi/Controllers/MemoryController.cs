using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;
using Synaptic.NET.Domain.Abstractions.Management;
using Synaptic.NET.Domain.Abstractions.Storage;
using Synaptic.NET.Domain.Attributes;
using Synaptic.NET.Domain.Resources.Storage;

namespace Synaptic.NET.RestApi.Controllers;

[ApiController]
[Route("api/memory")]
[Authorize(AuthenticationSchemes = "Bearer,ApiKey,Cookies")]
public class MemoryController : ControllerBase
{
    private readonly IMemoryProvider _memory;
    private readonly ICurrentUserService _currentUserService;

    public MemoryController(IMemoryProvider memory, ICurrentUserService currentUserService)
    {
        _memory = memory;
        _currentUserService = currentUserService;
    }

    [HttpGet("stores/ids")]
    [EndpointSummary("Gets the currently available store identifier and descriptions.")]
    [EndpointDescription("This endpoint will list all available identifiers and their memory store descriptions on the available memory stores.")]
    [Produces("application/json")]
    [ProducesResponseType(200, Type = typeof(IReadOnlyDictionary<Guid, string>))]
    public async Task<ActionResult<IReadOnlyDictionary<Guid, string>>> GetStoreIdsAndDescriptions()
    {
        await _currentUserService.LockoutUserIfGuestAsync();
        var dict = await _memory.GetStoreIdentifiersAndDescriptionsAsync();
        return Ok(dict);
    }

    [HttpGet("stores")]
    [EndpointSummary("Gets all available memory stores.")]
    [EndpointDescription("This endpoint will list all available memory stores.")]
    [Produces("application/json")]
    [ProducesResponseType(200, Type = typeof(List<MemoryStore>))]
    public async Task<ActionResult<List<MemoryStore>>> GetStores()
    {
        await _currentUserService.LockoutUserIfGuestAsync();
        var stores = await _memory.GetStoresAsync();
        return Ok(stores);
    }

    [HttpGet("stores/{id:guid}")]
    [EndpointSummary("Gets a memory store by its identifier.")]
    [EndpointDescription("This endpoint will retrieve a memory store by its identifier.")]
    [Produces("application/json")]
    [ProducesResponseType(200, Type = typeof(MemoryStore))]
    [ProducesResponseType(404, Description = "Returned when no memory store of this identifier is present.")]
    public async Task<ActionResult<MemoryStore>> GetStoreById(
        [Required]
        [FromQuery, SwaggerParameter("The GUID of the memory store to retrieve, must be applicable to any of the available stores.")]
        Guid id)
    {
        await _currentUserService.LockoutUserIfGuestAsync();
        var store = await _memory.GetCollectionAsync(id);
        if (store is null)
        {
            return NotFound();
        }
        return Ok(store);
    }

    [HttpGet("stores/by-title/{title}")]
    [EndpointSummary("Gets a memory store by its title.")]
    [EndpointDescription("This endpoint will retrieve a memory store by its title.")]
    [Produces("application/json")]
    [ProducesResponseType(200, Type = typeof(MemoryStore))]
    [ProducesResponseType(404, Description = "Returned when no memory store with this title is present.")]
    [AssistantConstraint("The title must match an existing memory store title exactly.")]
    public async Task<ActionResult<MemoryStore>> GetStoreByTitle(string title)
    {
        await _currentUserService.LockoutUserIfGuestAsync();
        var store = await _memory.GetCollectionAsync(title);
        if (store is null)
        {
            return NotFound();
        }
        return Ok(store);
    }

    [HttpPost("stores")]
    [EndpointSummary("Creates a new memory store.")]
    [EndpointDescription("This endpoint will create a new memory store using the provided MemoryStore object.")]
    [Produces("application/json")]
    [ProducesResponseType(201, Type = typeof(MemoryStore))]
    [ProducesResponseType(409, Description = "Returned when the collection could not be created due to conflicts.")]
    [AssistantInstruction("If the Id is not provided in the store object, a new GUID will be generated automatically.")]
    [AssistantExample("POST /api/memory/stores with body: {\"Title\": \"My Store\", \"Description\": \"A store for important memories\"}")]
    public async Task<ActionResult<MemoryStore>> CreateStore([FromBody] MemoryStore store)
    {
        await _currentUserService.LockoutUserIfGuestAsync();
        var created = await _memory.CreateCollectionAsync(store);
        if (created is null)
        {
            return Conflict("Collection could not be created.");
        }
        return CreatedAtAction(nameof(GetStoreById), new { id = created.StoreId }, created);
    }

    [HttpPost("stores/by-title")]
    [EndpointSummary("Creates a new memory store by title and description.")]
    [EndpointDescription("This endpoint will create a new memory store using query parameters for title and optional description.")]
    [Produces("application/json")]
    [ProducesResponseType(201, Type = typeof(MemoryStore))]
    [ProducesResponseType(409, Description = "Returned when the collection could not be created due to conflicts.")]
    [AssistantConstraint("The title must be unique and not empty.")]
    [AssistantInstruction("If description is not provided, an empty description will be used.")]
    [AssistantExample("POST /api/memory/stores/by-title?title=ProjectNotes&description=Notes for the current project")]
    public async Task<ActionResult<MemoryStore>> CreateStoreByTitle([FromQuery] string title, [FromQuery] string description = "")
    {
        await _currentUserService.LockoutUserIfGuestAsync();
        var store = await _memory.CreateCollectionAsync(title, description);
        if (store == null)
        {
            return Conflict("Collection could not be created.");
        }
        return CreatedAtAction(nameof(GetStoreById), new { id = store.StoreId }, store);
    }

    [HttpPut("stores/{id:guid}")]
    [EndpointSummary("Replaces an existing memory store.")]
    [EndpointDescription("This endpoint will completely replace an existing memory store with the provided data.")]
    [ProducesResponseType(204, Description = "Store successfully replaced.")]
    [ProducesResponseType(404, Description = "Store with the specified ID was not found.")]
    [AssistantConstraint("The store ID must exist in the available stores.")]
    [AssistantInstruction("This operation completely replaces the existing store - all existing data will be overwritten.")]
    public async Task<IActionResult> ReplaceStore(Guid id, [FromBody] MemoryStore newStore)
    {
        await _currentUserService.LockoutUserIfGuestAsync();
        var ok = await _memory.ReplaceCollectionAsync(id, newStore);
        if (!ok)
        {
            return NotFound();
        }
        return NoContent();
    }

    [HttpPut("stores/by-title/{title}")]
    [EndpointSummary("Replaces an existing memory store by title.")]
    [EndpointDescription("This endpoint will completely replace an existing memory store identified by title with the provided data.")]
    [ProducesResponseType(204, Description = "Store successfully replaced.")]
    [ProducesResponseType(404, Description = "Store with the specified title was not found.")]
    [AssistantConstraint("The title must match an existing memory store title exactly.")]
    [AssistantInstruction("This operation completely replaces the existing store - all existing data will be overwritten.")]
    public async Task<IActionResult> ReplaceStoreByTitle(string title, [FromBody] MemoryStore newStore)
    {
        await _currentUserService.LockoutUserIfGuestAsync();
        var ok = await _memory.ReplaceCollectionAsync(title, newStore);
        if (!ok)
        {
            return NotFound();
        }
        return NoContent();
    }

    [HttpPatch("stores/{id:guid}")]
    [EndpointSummary("Updates an existing memory store.")]
    [EndpointDescription("This endpoint will partially update an existing memory store with the provided data.")]
    [ProducesResponseType(204, Description = "Store successfully updated.")]
    [ProducesResponseType(404, Description = "Store with the specified ID was not found.")]
    [AssistantConstraint("The store ID must exist in the available stores.")]
    [AssistantInstruction("Only the provided fields will be updated - other fields will remain unchanged.")]
    public async Task<IActionResult> UpdateStore(Guid id, [FromBody] MemoryStore updated)
    {
        await _currentUserService.LockoutUserIfGuestAsync();
        var ok = await _memory.UpdateCollectionAsync(id, updated);
        if (!ok)
        {
            return NotFound();
        }
        return NoContent();
    }

    [HttpPatch("stores/by-title/{title}")]
    [EndpointSummary("Updates an existing memory store by title.")]
    [EndpointDescription("This endpoint will partially update an existing memory store identified by title with the provided data.")]
    [ProducesResponseType(204, Description = "Store successfully updated.")]
    [ProducesResponseType(404, Description = "Store with the specified title was not found.")]
    [AssistantConstraint("The title must match an existing memory store title exactly.")]
    [AssistantInstruction("Only the provided fields will be updated - other fields will remain unchanged.")]
    public async Task<IActionResult> UpdateStoreByTitle(string title, [FromBody] MemoryStore updated)
    {
        await _currentUserService.LockoutUserIfGuestAsync();
        var ok = await _memory.UpdateCollectionAsync(title, updated);
        if (!ok)
        {
            return NotFound();
        }
        return NoContent();
    }

    [HttpDelete("stores/{id:guid}")]
    [EndpointSummary("Deletes a memory store.")]
    [EndpointDescription("This endpoint will permanently delete a memory store and all its contents.")]
    [ProducesResponseType(204, Description = "Store successfully deleted.")]
    [ProducesResponseType(404, Description = "Store with the specified ID was not found.")]
    [AssistantConstraint("The store ID must exist in the available stores.")]
    [AssistantInstruction("This operation is irreversible and will delete all memory entries in the store.")]
    public async Task<IActionResult> DeleteStore(Guid id)
    {
        await _currentUserService.LockoutUserIfGuestAsync();
        var ok = await _memory.DeleteCollectionAsync(id);
        if (!ok)
        {
            return NotFound();
        }
        return NoContent();
    }

    [HttpDelete("stores/by-title/{title}")]
    [EndpointSummary("Deletes a memory store by title.")]
    [EndpointDescription("This endpoint will permanently delete a memory store identified by title and all its contents.")]
    [ProducesResponseType(204, Description = "Store successfully deleted.")]
    [ProducesResponseType(404, Description = "Store with the specified title was not found.")]
    [AssistantConstraint("The title must match an existing memory store title exactly.")]
    [AssistantInstruction("This operation is irreversible and will delete all memory entries in the store.")]
    public async Task<IActionResult> DeleteStoreByTitle(string title)
    {
        await _currentUserService.LockoutUserIfGuestAsync();
        var ok = await _memory.DeleteCollectionAsync(title);
        if (!ok)
        {
            return NotFound();
        }
        return NoContent();
    }

    [HttpPost("entries")]
    [EndpointSummary("Creates a memory entry with automatic store routing.")]
    [EndpointDescription("This endpoint will create a memory entry and automatically determine the best store for it.")]
    [ProducesResponseType(202, Description = "Entry creation accepted for processing.")]
    [ProducesResponseType(400, Description = "Entry could not be created due to invalid data.")]
    [AssistantInstruction("The system will automatically select the most appropriate store based on the memory content.")]
    [AssistantExample("POST /api/memory/entries with body: {\"Title\": \"Meeting Notes\", \"Content\": \"Discussed project timeline\"}")]
    public async Task<IActionResult> CreateEntry([FromBody] Memory memory)
    {
        await _currentUserService.LockoutUserIfGuestAsync();
        var ok = await _memory.CreateMemoryEntryAsync(memory);
        if (!ok)
        {
            return BadRequest("Entry could not be created.");
        }
        return Accepted();
    }

    [HttpPost("stores/{storeId:guid}/entries")]
    [AssistantConstraint("The store identifier must be applicable to any of the available stores.")]
    public async Task<IActionResult> CreateEntryInStore(

        Guid storeId,
        [AssistantInstruction("It's recommended to provide a memory description on creation of the memory to prevent additional processing time.")]
        [FromBody] Memory memory,
        [Description("The description of the store. If this parameter is empty, the description will be created from the initially added memory.")]
        [AssistantInstruction("If the store has to be created and this parameter is empty, the description will be created from the initially added memory.")]
        [FromQuery] string storeDescription = "")
    {
        await _currentUserService.LockoutUserIfGuestAsync();
        var ok = await _memory.CreateMemoryEntryAsync(storeId, memory, storeDescription);
        if (!ok)
        {
            return NotFound("Store not found or entry could not be created.");
        }
        return Accepted();
    }

    [HttpPost("stores/by-title/{title}/entries")]
    [EndpointSummary("Creates a memory entry in a specific store identified by title.")]
    [EndpointDescription("This endpoint will create a memory entry in the specified store, creating the store if it doesn't exist.")]
    [ProducesResponseType(202, Description = "Entry creation accepted for processing.")]
    [ProducesResponseType(404, Description = "Store not found or entry could not be created.")]
    [AssistantConstraint("The title must be a valid store title or the store will be created.")]
    [AssistantInstruction("If the store doesn't exist, it will be created with the provided description.")]
    [AssistantExample("POST /api/memory/stores/by-title/ProjectNotes/entries?storeDescription=Project related notes")]
    public async Task<IActionResult> CreateEntryInStoreByTitle(string title, [FromBody] Memory memory, [FromQuery] string storeDescription = "")
    {
        await _currentUserService.LockoutUserIfGuestAsync();
        var ok = await _memory.CreateMemoryEntryAsync(title, memory, storeDescription);
        if (!ok)
        {
            return NotFound("Store not found or entry could not be created.");
        }
        return Accepted();
    }

    [HttpPut("entries/{entryId:guid}")]
    [EndpointSummary("Replaces an existing memory entry.")]
    [EndpointDescription("This endpoint will completely replace an existing memory entry with new data.")]
    [ProducesResponseType(204, Description = "Entry successfully replaced.")]
    [ProducesResponseType(404, Description = "Entry with the specified ID was not found.")]
    [AssistantConstraint("The entry ID must exist in one of the available stores.")]
    [AssistantInstruction("This operation completely replaces the existing entry - all existing data will be overwritten.")]
    public async Task<IActionResult> ReplaceEntry(Guid entryId, [FromBody] Memory newMemory)
    {
        await _currentUserService.LockoutUserIfGuestAsync();
        var ok = await _memory.ReplaceMemoryEntryAsync(entryId, newMemory);
        if (!ok)
        {
            return NotFound();
        }
        return NoContent();
    }

    [HttpPut("stores/{storeId:guid}/entries/{entryId:guid}")]
    [EndpointSummary("Replaces an existing memory entry in a specific store.")]
    [EndpointDescription("This endpoint will completely replace an existing memory entry within the specified store.")]
    [ProducesResponseType(204, Description = "Entry successfully replaced.")]
    [ProducesResponseType(404, Description = "Store or entry was not found.")]
    [AssistantConstraint("Both the store ID and entry ID must exist and be related.")]
    [AssistantInstruction("This operation completely replaces the existing entry - all existing data will be overwritten.")]
    public async Task<IActionResult> ReplaceEntryInStore(Guid storeId, Guid entryId, [FromBody] Memory newMemory)
    {
        await _currentUserService.LockoutUserIfGuestAsync();
        var ok = await _memory.ReplaceMemoryEntryAsync(storeId, entryId, newMemory);
        if (!ok)
        {
            return NotFound();
        }
        return NoContent();
    }

    [HttpPut("stores/by-title/{title}/entries/{entryId:guid}")]
    [EndpointSummary("Replaces an existing memory entry in a store identified by title.")]
    [EndpointDescription("This endpoint will completely replace an existing memory entry within the store identified by title.")]
    [ProducesResponseType(204, Description = "Entry successfully replaced.")]
    [ProducesResponseType(404, Description = "Store or entry was not found.")]
    [AssistantConstraint("The store title must exist and the entry ID must be valid within that store.")]
    [AssistantInstruction("This operation completely replaces the existing entry - all existing data will be overwritten.")]
    public async Task<IActionResult> ReplaceEntryInStoreByTitle(string title, Guid entryId, [FromBody] Memory newMemory)
    {
        await _currentUserService.LockoutUserIfGuestAsync();
        var ok = await _memory.ReplaceMemoryEntryAsync(title, entryId, newMemory);
        if (!ok)
        {
            return NotFound();
        }
        return NoContent();
    }

    [HttpPatch("entries/{entryId:guid}")]
    [EndpointSummary("Updates an existing memory entry.")]
    [EndpointDescription("This endpoint will partially update an existing memory entry with the provided data.")]
    [ProducesResponseType(204, Description = "Entry successfully updated.")]
    [ProducesResponseType(404, Description = "Entry with the specified ID was not found.")]
    [AssistantConstraint("The entry ID must exist in one of the available stores.")]
    [AssistantInstruction("Only the provided fields will be updated - other fields will remain unchanged.")]
    public async Task<IActionResult> UpdateEntry(Guid entryId, [FromBody] Memory newMemory)
    {
        await _currentUserService.LockoutUserIfGuestAsync();
        var ok = await _memory.UpdateMemoryEntryAsync(entryId, newMemory);
        if (!ok)
        {
            return NotFound();
        }
        return NoContent();
    }

    [HttpPatch("stores/{storeId:guid}/entries/{entryId:guid}")]
    [EndpointSummary("Updates an existing memory entry in a specific store.")]
    [EndpointDescription("This endpoint will partially update an existing memory entry within the specified store.")]
    [ProducesResponseType(204, Description = "Entry successfully updated.")]
    [ProducesResponseType(404, Description = "Store or entry was not found.")]
    [AssistantConstraint("Both the store ID and entry ID must exist and be related.")]
    [AssistantInstruction("Only the provided fields will be updated - other fields will remain unchanged.")]
    public async Task<IActionResult> UpdateEntryInStore(Guid storeId, Guid entryId, [FromBody] Memory newMemory)
    {
        await _currentUserService.LockoutUserIfGuestAsync();
        var ok = await _memory.UpdateMemoryEntryAsync(storeId, entryId, newMemory);
        if (!ok)
        {
            return NotFound();
        }
        return NoContent();
    }

    [HttpPatch("stores/by-title/{title}/entries/{entryId:guid}")]
    [EndpointSummary("Updates an existing memory entry in a store identified by title.")]
    [EndpointDescription("This endpoint will partially update an existing memory entry within the store identified by title.")]
    [ProducesResponseType(204, Description = "Entry successfully updated.")]
    [ProducesResponseType(404, Description = "Store or entry was not found.")]
    [AssistantConstraint("The store title must exist and the entry ID must be valid within that store.")]
    [AssistantInstruction("Only the provided fields will be updated - other fields will remain unchanged.")]
    public async Task<IActionResult> UpdateEntryInStoreByTitle(string title, Guid entryId, [FromBody] Memory newMemory)
    {
        await _currentUserService.LockoutUserIfGuestAsync();
        var ok = await _memory.UpdateMemoryEntryAsync(title, entryId, newMemory);
        if (!ok)
        {
            return NotFound();
        }
        return NoContent();
    }

    [HttpDelete("entries/{entryId:guid}")]
    [EndpointSummary("Deletes a memory entry.")]
    [EndpointDescription("This endpoint will permanently delete a memory entry from any store.")]
    [ProducesResponseType(204, Description = "Entry successfully deleted.")]
    [ProducesResponseType(404, Description = "Entry with the specified ID was not found.")]
    [AssistantConstraint("The entry ID must exist in one of the available stores.")]
    [AssistantInstruction("This operation is irreversible and will permanently remove the memory entry.")]
    public async Task<IActionResult> DeleteEntry(Guid entryId)
    {
        await _currentUserService.LockoutUserIfGuestAsync();
        var ok = await _memory.DeleteMemoryEntryAsync(entryId);
        if (!ok)
        {
            return NotFound();
        }
        return NoContent();
    }

    [HttpDelete("stores/{storeId:guid}/entries/{entryId:guid}")]
    [EndpointSummary("Deletes a memory entry from a specific store.")]
    [EndpointDescription("This endpoint will permanently delete a memory entry from the specified store.")]
    [ProducesResponseType(204, Description = "Entry successfully deleted.")]
    [ProducesResponseType(404, Description = "Store or entry was not found.")]
    [AssistantConstraint("Both the store ID and entry ID must exist and be related.")]
    [AssistantInstruction("This operation is irreversible and will permanently remove the memory entry from the specified store.")]
    public async Task<IActionResult> DeleteEntryInStore(Guid storeId, Guid entryId)
    {
        await _currentUserService.LockoutUserIfGuestAsync();
        var ok = await _memory.DeleteMemoryEntryAsync(storeId, entryId);
        if (!ok)
        {
            return NotFound();
        }
        return NoContent();
    }

    [HttpDelete("stores/{storeId:guid}/entries/by-title/{entryTitle}")]
    [EndpointSummary("Deletes a memory entry from a store by entry title.")]
    [EndpointDescription("This endpoint will permanently delete a memory entry identified by title from the specified store.")]
    [ProducesResponseType(204, Description = "Entry successfully deleted.")]
    [ProducesResponseType(404, Description = "Store or entry was not found.")]
    [AssistantConstraint("The store ID must exist and the entry title must match an existing entry in that store.")]
    [AssistantInstruction("This operation is irreversible and will permanently remove the memory entry from the specified store.")]
    public async Task<IActionResult> DeleteEntryInStoreByTitle(Guid storeId, string entryTitle)
    {
        await _currentUserService.LockoutUserIfGuestAsync();
        var ok = await _memory.DeleteMemoryEntryAsync(storeId, entryTitle);
        if (!ok)
        {
            return NotFound();
        }
        return NoContent();
    }

    [HttpGet("search")]
    [EndpointSummary("Searches memory entries across all stores.")]
    [EndpointDescription("This endpoint will search for memory entries matching the query across all available stores using semantic search.")]
    [Produces("application/json")]
    [ProducesResponseType(200, Type = typeof(IEnumerable<ContextMemory>))]
    [ProducesResponseType(400, Description = "Query parameter is empty or invalid.")]
    [AssistantConstraint("The query parameter must not be empty or whitespace.")]
    [AssistantInstruction("Use limit to control the number of results returned (default: 10). Use relevanceThreshold to filter results by relevance score (default: 0.5).")]
    [AssistantExample("GET /api/memory/search?query=project meetings&limit=5&relevanceThreshold=0.7")]
    public async Task<ActionResult<IEnumerable<ContextMemory>>> Search(
        [FromQuery] string query,
        [FromQuery] int limit = 10,
        [FromQuery] double relevanceThreshold = 0.5,
        [FromQuery] bool includePersonal = true,
        [FromQuery] Guid? limitToGroup = null,
        [FromQuery] Guid? limitToStore = null)
    {
        await _currentUserService.LockoutUserIfGuestAsync();
        if (string.IsNullOrWhiteSpace(query))
        {
            return BadRequest("Query must not be empty.");
        }

        MemoryQueryOptions options = MemoryQueryOptions.Default;

        if (!includePersonal)
        {
            options = options.WithoutPersonalSearch();
        }

        if (limitToGroup != null)
        {
            options = options.WithGroupOptions(GroupQueryOptions.ById(limitToGroup.Value));
        }

        if (limitToStore != null)
        {
            options = options.WithStoreOptions(StoreQueryOptions.ById(limitToStore.Value));
        }

        var results = await _memory.SearchAsync(query, limit, relevanceThreshold, options);
        var contextMemoryResults = await results.Results.ToListAsync();
        return Ok(contextMemoryResults.Select(m => new ContextMemory(m.Memory)));
    }
}
