namespace Synaptic.NET.Domain.Resources.Storage;

public class MemoryQueryOptions
{
    /// <summary>
    /// Initializes a new instance of the <see cref="MemoryQueryOptions"/> class.
    /// This is equal to the default settings, which searches in all groups and personal memories.
    /// </summary>
    public MemoryQueryOptions()
    {

    }

    public static MemoryQueryOptions PersonalOnly => new()
    {
        GroupQueryOptions = GroupQueryOptions.None, SearchInPersonal = true
    };

    public MemoryQueryOptions WithGroupOptions(GroupQueryOptions options)
    {
        GroupQueryOptions = options;
        return this;
    }

    public MemoryQueryOptions WithStoreOptions(StoreQueryOptions options)
    {
        StoreQueryOptions = options;
        return this;
    }

    public MemoryQueryOptions WithPersonalSearch()
    {
        SearchInPersonal = true;
        return this;
    }

    public MemoryQueryOptions WithoutPersonalSearch()
    {
        SearchInPersonal = false;
        return this;
    }

    public static readonly MemoryQueryOptions Default = new();

    public StoreQueryOptions StoreQueryOptions { get; set; } = StoreQueryOptions.Default;
    public GroupQueryOptions GroupQueryOptions { get; set; } = GroupQueryOptions.Default;
    public bool SearchInPersonal { get; set; } = true;
}
