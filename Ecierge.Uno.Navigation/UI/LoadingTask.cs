namespace Ecierge.Uno.Navigation.UI;

#pragma warning disable CA1068 // CancellationToken parameters must come last
internal class LoadingTask : global::Uno.Toolkit.ILoadable
#pragma warning restore CA1068 // CancellationToken parameters must come last
{
    private readonly CancellationToken ready;

    public bool IsExecuting => !ready.IsCancellationRequested;

    public LoadingTask(CancellationToken ready, FrameworkElement context)
    {
        this.ready = ready;
        if (ready.IsCancellationRequested)
        {
            context.DispatcherQueue.TryEnqueue(() =>
            {
                IsExecutingChanged?.Invoke(this, EventArgs.Empty);
            });
        }
        else
        {
            ready.Register(() =>
            {
                context.DispatcherQueue.TryEnqueue(() =>
                {
                    IsExecutingChanged?.Invoke(this, EventArgs.Empty);
                });
            });
        }
    }

    public event EventHandler? IsExecutingChanged;
}
