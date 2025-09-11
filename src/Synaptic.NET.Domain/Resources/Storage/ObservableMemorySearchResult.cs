namespace Synaptic.NET.Domain.Resources.Storage;

public class ObservableMemorySearchResult
{
    private readonly List<Action<string>> _subscribers = new();
    private readonly List<Action<double>> _progressSubscribers = new();
    private readonly Func<ObservableMemorySearchResult, IAsyncEnumerable<MemorySearchResult>> _resultsProvider;

    public ObservableMemorySearchResult(Func<ObservableMemorySearchResult, IAsyncEnumerable<MemorySearchResult>> resultsProvider)
    {
        _resultsProvider = resultsProvider;
        Message = "Received search request.";
        Results = resultsProvider.Invoke(this);
    }

    public void SubscribeToStatusChanges(Action<string> onChange)
    {
        _subscribers.Add(onChange);
    }

    public void SubscribeToProgressChanges(Action<double> onChange)
    {
        _progressSubscribers.Add(onChange);
    }

    /// <summary>
    /// The progress of the memory search between 0 and 1.
    /// </summary>
    public double Progress
    {
        get;
        set
        {
            field = value;
            _progressSubscribers.ForEach(s => s.Invoke(value));
        }
    }

    /// <summary>
    /// The message associated with the current progress.
    /// </summary>
    public string Message
    {
        get;
        set
        {
            field = value;
            _subscribers.ForEach(s => s.Invoke(value));
        }
    }

    public bool IsComplete
    {
        get;
        set
        {
            field = value;
            Progress = 1;
        }
    }

    /// <summary>
    /// The results of the memory search.
    /// </summary>
    public IAsyncEnumerable<MemorySearchResult> Results { get; private set; }
}
