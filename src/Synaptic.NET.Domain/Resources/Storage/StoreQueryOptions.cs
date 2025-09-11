namespace Synaptic.NET.Domain.Resources.Storage;

public class StoreQueryOptions
{
    /// <summary>
    /// Initializes a new instance of the <see cref="StoreQueryOptions"/> class.
    /// This is equal to the default settings, which searches in all stores.
    /// </summary>
    public StoreQueryOptions()
    {

    }

    public bool StoreShouldBeIncluded(MemoryStore store)
    {
        if (StoreSearchMode == StoreSearchMode.All)
        {
            return true;
        }
        return StoreIds.Any(i => i == store.StoreId) || StoreNames.Any(n => n == store.Title);
    }

    public static StoreQueryOptions ById(Guid id) => new StoreQueryOptions()
    {
        StoreSearchMode = StoreSearchMode.SingleById, StoreIds = new List<Guid> { id }
    };

    public static StoreQueryOptions ByTitle(string name) => new StoreQueryOptions()
    {
        StoreSearchMode = StoreSearchMode.SingleByName, StoreNames = new List<string> { name }
    };

    public static StoreQueryOptions ByIds(List<Guid> ids) => new StoreQueryOptions()
    {
        StoreSearchMode = StoreSearchMode.ManyByIds, StoreIds = ids
    };

    public static StoreQueryOptions ByTitles(List<string> names) => new StoreQueryOptions()
    {
        StoreSearchMode = StoreSearchMode.ManyByNames, StoreNames = names
    };

    public static readonly StoreQueryOptions Default = new StoreQueryOptions() { StoreSearchMode = StoreSearchMode.All };
    public StoreSearchMode StoreSearchMode { get; init; } = StoreSearchMode.All;
    public List<Guid> StoreIds { get; init; } = new();
    public List<string> StoreNames { get; init; } = new();
}
