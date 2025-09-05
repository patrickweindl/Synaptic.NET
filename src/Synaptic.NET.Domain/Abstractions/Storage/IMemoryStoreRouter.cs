using Synaptic.NET.Domain.Resources.Storage;

namespace Synaptic.NET.Domain.Abstractions.Storage;

public interface IMemoryStoreRouter
{
    Task<List<MemoryStoreRoutingResult>> RankStoresAsync(string query, IEnumerable<MemoryStore> availableStores);

    Task<MemoryStoreRoutingResult> RouteMemoryToStoreAsync(Memory memory, IEnumerable<MemoryStore> availableStores);
}
