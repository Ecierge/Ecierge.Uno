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
    private readonly Navigator root;
    private readonly RouteNotifier.RootState state;

    internal RouteNavigation(RouteNotifier notifier, Navigator root, RouteNotifier.RootState state)
    {
        this.notifier = notifier;
        this.root = root;
        this.state = state;
    }

    /// <summary>
    /// Marks the end of the navigation. When it is the outermost one, the history is updated and the change is
    /// announced on the thread of the user interface.
    /// </summary>
    /// <param name="response">The outcome of the navigation.</param>
    /// <returns>A task that completes when the change was announced.</returns>
    public ValueTask CompleteAsync(NavigationResponse response) => notifier.CompleteAsync(root, state, response);
}

internal class RouteNotifier : IRouteNotifier, IRouteUpdater
{
    internal sealed class RootState
    {
        public int Depth;
        public HistoryMode HistoryMode;
    }

    private readonly ILogger logger;
    private readonly IOptions<NavigationOptions> options;
    private readonly ConditionalWeakTable<Navigator, RootState> states = new();

    public event EventHandler<RouteChangedEventArgs>? RouteChanged;

    public RouteNotifier(ILogger<RouteNotifier> logger, IOptions<NavigationOptions> options)
    {
        this.logger = logger;
        this.options = options;
    }

    public RouteNavigation StartNavigation(Navigator navigator, HistoryMode historyMode, NavigationMode mode)
    {
        var root = navigator.RootNavigator;
        var state = states.GetValue(root, _ => new RootState());
        lock (state)
        {
            // Navigations can overlap: a page may start another one while it is created, and a command may start one
            // from any thread. The outermost one decides what happens to the history.
            if (state.Depth++ == 0)
            {
                state.HistoryMode = historyMode;
            }
        }

        return new RouteNavigation(this, root, state);
    }

    internal async ValueTask CompleteAsync(Navigator root, RootState state, NavigationResponse response)
    {
        HistoryMode historyMode;
        lock (state)
        {
            if (--state.Depth > 0) return;
            historyMode = state.HistoryMode;
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
