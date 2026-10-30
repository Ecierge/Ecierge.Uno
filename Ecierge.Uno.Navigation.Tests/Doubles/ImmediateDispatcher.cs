namespace Ecierge.Uno.Navigation.Tests.Doubles;

using Ecierge.Uno.Navigation;

/// <summary>
/// A dispatcher that runs all the work at once on the calling thread, so a test needs no user interface thread.
/// </summary>
internal sealed class ImmediateDispatcher : INavigationDispatcher
{
    public bool HasThreadAccess => true;

    public bool TryEnqueue(NavigationDispatcherPriority priority, Action action)
    {
        action();
        return true;
    }

    public ValueTask EnqueueAsync(Action action, NavigationDispatcherPriority priority = NavigationDispatcherPriority.Normal)
    {
        action();
        return ValueTask.CompletedTask;
    }

    public ValueTask<T> EnqueueAsync<T>(Func<T> function, NavigationDispatcherPriority priority = NavigationDispatcherPriority.Normal)
     => ValueTask.FromResult(function());

    public ValueTask EnqueueAsync(Func<ValueTask> function, NavigationDispatcherPriority priority = NavigationDispatcherPriority.Normal)
     => function();

    public ValueTask<T> EnqueueAsync<T>(Func<ValueTask<T>> function, NavigationDispatcherPriority priority = NavigationDispatcherPriority.Normal)
     => function();
}
