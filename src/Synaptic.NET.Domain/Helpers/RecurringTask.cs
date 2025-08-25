namespace Synaptic.NET.Domain.Helpers;

public static class RecurringTask
{
    public static void Create(Action action, TimeSpan interval, CancellationToken cancellationToken)
    {
        Task.Run(async () =>
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                action.Invoke();
                await Task.Delay(interval, cancellationToken);
            }
        }, cancellationToken);
    }
}
