namespace Ecierge.Uno.Navigation;

using System;
using System.Threading.Tasks;

using Microsoft.UI.Dispatching;

/// <summary>
/// The priority of the work that is queued to the thread of the user interface.
/// </summary>
public enum NavigationDispatcherPriority
{
    /// <summary>The work runs when the thread has nothing else to do.</summary>
    Low,

    /// <summary>The work runs in its turn.</summary>
    Normal,

    /// <summary>The work runs before the other queued work.</summary>
    High
}

/// <summary>
/// Runs navigation work on the thread of the user interface.
/// </summary>
/// <remarks>
/// <para>
/// The navigators go through this service instead of the <see cref="DispatcherQueue"/> of a control, so that a
/// navigator without a control, such as the navigator of a dialog that is not shown yet or a navigator in a unit
/// test, has a thread to run on. An application gets the service of its window; a test replaces it with one that
/// runs the work at once.
/// </para>
/// </remarks>
public interface INavigationDispatcher
{
    /// <summary>
    /// Gets a value that tells whether the calling code runs on the thread of the dispatcher.
    /// </summary>
    bool HasThreadAccess { get; }

    /// <summary>
    /// Queues the action without waiting for it.
    /// </summary>
    /// <param name="priority">The priority of the action.</param>
    /// <param name="action">The action to run.</param>
    /// <returns><see langword="true"/> if the action was queued, <see langword="false"/> if the dispatcher is closed.</returns>
    bool TryEnqueue(NavigationDispatcherPriority priority, Action action);

    /// <summary>
    /// Runs the action on the thread of the dispatcher, at once when the caller is already on it.
    /// </summary>
    /// <param name="action">The action to run.</param>
    /// <param name="priority">The priority of the action when it has to be queued.</param>
    /// <returns>A task that completes when the action has run.</returns>
    ValueTask EnqueueAsync(Action action, NavigationDispatcherPriority priority = NavigationDispatcherPriority.Normal);

    /// <summary>
    /// Runs the function on the thread of the dispatcher, at once when the caller is already on it.
    /// </summary>
    /// <typeparam name="T">The type of the result.</typeparam>
    /// <param name="function">The function to run.</param>
    /// <param name="priority">The priority of the function when it has to be queued.</param>
    /// <returns>A task with the result of the function.</returns>
    ValueTask<T> EnqueueAsync<T>(Func<T> function, NavigationDispatcherPriority priority = NavigationDispatcherPriority.Normal);

    /// <summary>
    /// Runs the asynchronous function on the thread of the dispatcher and waits for it.
    /// </summary>
    /// <param name="function">The function to run.</param>
    /// <param name="priority">The priority of the function when it has to be queued.</param>
    /// <returns>A task that completes when the function has completed.</returns>
    ValueTask EnqueueAsync(Func<ValueTask> function, NavigationDispatcherPriority priority = NavigationDispatcherPriority.Normal);

    /// <summary>
    /// Runs the asynchronous function on the thread of the dispatcher and waits for its result.
    /// </summary>
    /// <typeparam name="T">The type of the result.</typeparam>
    /// <param name="function">The function to run.</param>
    /// <param name="priority">The priority of the function when it has to be queued.</param>
    /// <returns>A task with the result of the function.</returns>
    ValueTask<T> EnqueueAsync<T>(Func<ValueTask<T>> function, NavigationDispatcherPriority priority = NavigationDispatcherPriority.Normal);
}

/// <summary>
/// The dispatcher of a window, backed by its <see cref="DispatcherQueue"/>.
/// </summary>
internal sealed class DispatcherQueueNavigationDispatcher : INavigationDispatcher
{
    private readonly DispatcherQueue queue;

    /// <summary>Creates the dispatcher of a queue.</summary>
    /// <exception cref="ArgumentNullException"><paramref name="queue"/> is <see langword="null"/>.</exception>
    public DispatcherQueueNavigationDispatcher(DispatcherQueue queue)
        => this.queue = queue ?? throw new ArgumentNullException(nameof(queue));

    public bool HasThreadAccess => queue.HasThreadAccess;

    private static DispatcherQueuePriority Map(NavigationDispatcherPriority priority)
     => priority switch
        {
            NavigationDispatcherPriority.Low => DispatcherQueuePriority.Low,
            NavigationDispatcherPriority.High => DispatcherQueuePriority.High,
            _ => DispatcherQueuePriority.Normal
        };

    public bool TryEnqueue(NavigationDispatcherPriority priority, Action action)
     => queue.TryEnqueue(Map(priority), () => action());

    public ValueTask EnqueueAsync(Action action, NavigationDispatcherPriority priority)
     => queue.EnqueueAsync(action, Map(priority));

    public ValueTask<T> EnqueueAsync<T>(Func<T> function, NavigationDispatcherPriority priority)
     => queue.EnqueueAsync(function, Map(priority));

    public ValueTask EnqueueAsync(Func<ValueTask> function, NavigationDispatcherPriority priority)
     => queue.EnqueueAsync(function, Map(priority));

    public ValueTask<T> EnqueueAsync<T>(Func<ValueTask<T>> function, NavigationDispatcherPriority priority)
     => queue.EnqueueAsync(function, Map(priority));
}
