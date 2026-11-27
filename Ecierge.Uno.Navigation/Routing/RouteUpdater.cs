namespace Ecierge.Uno.Navigation;

using System;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

/// <summary>
/// Announces that the route of a window changed. It is raised once for a whole navigation of a route, however many
/// navigators took part in it, and after the history was updated.
/// </summary>
public interface IRouteNotifier
{
    /// <summary>
    /// Raised when the navigation of a route has ended.
    /// </summary>
    event EventHandler<RouteChangedEventArgs> RouteChanged;
}

/// <summary>
/// Follows the navigations of routes, so that the history is updated and the change is announced once for the
/// outermost of the navigations that overlap.
/// </summary>
internal interface IRouteUpdater
{
    /// <summary>
    /// Marks the start of the navigation of a route.
    /// </summary>
    /// <param name="navigator">The navigator that the navigation was started on.</param>
    /// <param name="historyMode">What the navigation does to the history, when it is the outermost one.</param>
    /// <param name="mode">How the navigation was started.</param>
    /// <returns>The navigation, which has to be completed.</returns>
    RouteNavigation StartNavigation(Navigator navigator, HistoryMode historyMode, NavigationMode mode);
}

/// <summary>
/// A navigation of a route that is in progress.
/// </summary>
internal sealed class RouteNavigation
{
    private readonly RouteNotifier notifier;

    internal RouteNavigation(RouteNotifier notifier, Navigator root, RouteNotifier.RootState state, HistoryMode historyMode, long started, string description)
    {
        this.notifier = notifier;
        Root = root;
        State = state;
        HistoryMode = historyMode;
        Started = started;
        Description = description;
    }

    internal Navigator Root { get; }

    internal RouteNotifier.RootState State { get; }

    /// <summary>Gets what the navigation does to the history when it is the outermost one.</summary>
    internal HistoryMode HistoryMode { get; }

    /// <summary>Gets the time the navigation started at, as the time provider of the notifier counts it.</summary>
    internal long Started { get; }

    /// <summary>Gets the segment of the region that the navigation was started in, for the log.</summary>
    internal string Description { get; }

    /// <summary>Gets or sets whether the navigation has ended; guarded by the lock of the state.</summary>
    internal bool IsCompleted { get; set; }

    /// <summary>
    /// Gets or sets whether the notifier stopped waiting for the navigation because it was open for too long; guarded
    /// by the lock of the state.
    /// </summary>
    internal bool IsAbandoned { get; set; }

    /// <summary>
    /// Marks the end of the navigation. When it is the outermost one, the history is updated and the change is
    /// announced on the thread of the user interface.
    /// </summary>
    /// <param name="response">The outcome of the navigation.</param>
    /// <returns>A task that completes when the change was announced.</returns>
    public ValueTask CompleteAsync(NavigationResponse response) => notifier.CompleteAsync(this, response);
}

internal class RouteNotifier : IRouteNotifier, IRouteUpdater
{
    internal sealed class RootState
    {
        /// <summary>The navigations that have started and not ended, oldest first; guarded by a lock on the state.</summary>
        public readonly List<RouteNavigation> Open = new();

        /// <summary>What the navigation that opened the group of overlapping navigations does to the history.</summary>
        public HistoryMode HistoryMode;
    }

    private readonly ILogger logger;
    private readonly IOptions<NavigationOptions> options;
    private readonly TimeProvider timeProvider;
    private readonly ConditionalWeakTable<Navigator, RootState> states = new();

    public event EventHandler<RouteChangedEventArgs>? RouteChanged;

    public RouteNotifier(ILogger<RouteNotifier> logger, IOptions<NavigationOptions> options, TimeProvider? timeProvider = null)
    {
        this.logger = logger;
        this.options = options;
        this.timeProvider = timeProvider ?? TimeProvider.System;
    }

    public RouteNavigation StartNavigation(Navigator navigator, HistoryMode historyMode, NavigationMode mode)
    {
        var root = navigator.RootNavigator;
        var state = states.GetValue(root, _ => new RootState());
        var navigation = new RouteNavigation(this, root, state, historyMode, timeProvider.GetTimestamp(), navigator.Region.Segment.Name);
        lock (state)
        {
            AbandonStale(state, navigation.Started);

            // Navigations can overlap: a page may start another one while it is created, and a command may start one
            // from any thread. The outermost one decides what happens to the history.
            if (state.Open.Count == 0)
            {
                state.HistoryMode = historyMode;
            }

            state.Open.Add(navigation);
        }

        return navigation;
    }

    /// <summary>
    /// Stops waiting for the navigations that have been open for longer than the options allow. A navigation that
    /// never ends, whatever the reason, would otherwise hold back the history and the announcements of every route
    /// after it for ever.
    /// </summary>
    private void AbandonStale(RootState state, long now)
    {
        var timeout = options.Value.StaleNavigationTimeout;
        if (timeout == Timeout.InfiniteTimeSpan || state.Open.Count == 0) return;

        for (var index = state.Open.Count - 1; index >= 0; index--)
        {
            var navigation = state.Open[index];
            var open = timeProvider.GetElapsedTime(navigation.Started, now);
            if (open <= timeout) continue;

            navigation.IsAbandoned = true;
            state.Open.RemoveAt(index);
            logger.LogWarning(
                "The navigation started in the region '{Region}' has been open for {Open}, so the history and the announcements of the routes after it no longer wait for it",
                navigation.Description,
                open);
        }
    }

    internal async ValueTask CompleteAsync(RouteNavigation navigation, NavigationResponse response)
    {
        var root = navigation.Root;
        var state = navigation.State;
        HistoryMode historyMode;
        lock (state)
        {
            // An end that is reported twice is one end
            if (navigation.IsCompleted) return;
            navigation.IsCompleted = true;

            if (navigation.IsAbandoned)
            {
                // Nobody waited for it, and it has the last word only when no other navigation is open now
                if (state.Open.Count > 0) return;
                historyMode = navigation.HistoryMode;
            }
            else
            {
                state.Open.Remove(navigation);
                if (state.Open.Count > 0) return;
                historyMode = state.HistoryMode;
            }
        }

        try
        {
            await root.Dispatcher.EnqueueAsync(() => Publish(root, response, historyMode));
        }
        catch (Exception ex)
        {
            // Whoever listens to the change must not break the navigation that has already happened
            logger.LogError(ex, "Failed to announce the change of the route");
        }
    }

    private void Publish(Navigator root, NavigationResponse response, HistoryMode historyMode)
    {
        if (response.Success && historyMode != HistoryMode.Skip)
        {
            var route = root.ActualRoute;
            if (!options.Value.DialogsInHistory) route = route.WithoutDialog();
            if (!route.Segments.IsEmpty)
            {
                root.Journal.Record(route, historyMode);
            }
        }

        if (logger.IsEnabled(LogLevel.Trace))
        {
            logger.LogTrace("Route changed to {Route}, navigation {Success}", root.ActualRoute.ToPath(), response.Success ? "succeeded" : "failed");
        }

        RouteChanged?.Invoke(this, new RouteChangedEventArgs(root.Region, root.LeafNavigator) { Response = response });
    }
}
