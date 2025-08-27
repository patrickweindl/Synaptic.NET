using Synaptic.NET.Domain.Resources;

namespace Synaptic.NET.Core;

public interface IMemoryStoreRouter
{
    Task<List<MemoryStoreRoutingResult>> RankStoresAsync(string query, IEnumerable<MemoryStore> availableStores);

    Task<MemoryStoreRoutingResult> RouteMemoryToStoreAsync(Memory memory, IEnumerable<MemoryStore> availableStores);
}
