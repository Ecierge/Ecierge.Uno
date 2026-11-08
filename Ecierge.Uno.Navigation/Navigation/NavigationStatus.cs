namespace Ecierge.Uno.Navigation;

using System;
using System.Threading.Tasks;

/// <summary>
/// Announces the start and the end of every navigation of a route, wherever it was started from.
/// </summary>
public interface INavigationStatus
{
    /// <summary>
    /// Raised when a route starts to be navigated. The route is created when it is first read.
    /// </summary>
    event EventHandler<Lazy<Routing.Route>> NavigationStarted;

    /// <summary>
    /// Raised when a route has been navigated or the navigation failed.
    /// </summary>
    event EventHandler<NavigationResponse> NavigationFinished;

    /// <summary>
    /// Raised, after <see cref="NavigationFinished"/>, when the navigation failed or was refused.
    /// </summary>
    event EventHandler<NavigationFailedResponse> NavigationFailed;

    /// <summary>
    /// Raised when a navigation waits for an entity that is not loaded, before it creates the pages below the data
    /// segment of the entity. A page that shows progress can follow the task.
    /// </summary>
    event EventHandler<NavigationEntityLoadingEventArgs> EntityLoading;
}

/// <summary>
/// Describes an entity that a navigation waits for.
/// </summary>
public sealed class NavigationEntityLoadingEventArgs : EventArgs
{
    internal NavigationEntityLoadingEventArgs(Navigator navigator, INavigationEntity entity, Task task)
    {
        Navigator = navigator;
        Entity = entity;
        Task = task;
    }

    /// <summary>Gets the navigator that the navigation was started on.</summary>
    public Navigator Navigator { get; }

    /// <summary>Gets the handle of the entity.</summary>
    public INavigationEntity Entity { get; }

    /// <summary>Gets the task that loads the entity; it faults if the load fails.</summary>
    public Task Task { get; }
}

internal class NavigationStatus : INavigationStatus
{
    public event EventHandler<Lazy<Routing.Route>>? NavigationStarted;
    public event EventHandler<NavigationResponse>? NavigationFinished;
    public event EventHandler<NavigationFailedResponse>? NavigationFailed;
    public event EventHandler<NavigationEntityLoadingEventArgs>? EntityLoading;

    public void OnEntityLoading(NavigationEntityLoadingEventArgs args) => EntityLoading?.Invoke(this, args);

    public void OnNavigationStarted(Lazy<Routing.Route> route) => NavigationStarted?.Invoke(this, route);

    public void OnNavigationCompleted(NavigationResponse response)
    {
        NavigationFinished?.Invoke(this, response);
        if (response is NavigationFailedResponse failed)
        {
            NavigationFailed?.Invoke(this, failed);
        }
    }
}

internal static class NavigationStatusExtensions
{
    public static void RaiseNavigationStarted(this Navigator navigator, Lazy<Routing.Route> route)
    {
        var status = (NavigationStatus)navigator.NavigationStatus;
        var dispatcher = navigator.Dispatcher;
        if (dispatcher.HasThreadAccess)
            status.OnNavigationStarted(route);
        else
            dispatcher.TryEnqueue(NavigationDispatcherPriority.High, () => status.OnNavigationStarted(route));
    }

    public static void RaiseNavigationStarted(this Navigator navigator, Func<Routing.Route> routeFactory)
     => navigator.RaiseNavigationStarted(new Lazy<Routing.Route>(routeFactory));

    public static void RaiseEntityLoading(this Navigator navigator, INavigationEntity entity, Task task)
    {
        var status = (NavigationStatus)navigator.NavigationStatus;
        var args = new NavigationEntityLoadingEventArgs(navigator, entity, task);
        var dispatcher = navigator.Dispatcher;
        if (dispatcher.HasThreadAccess)
            status.OnEntityLoading(args);
        else
            dispatcher.TryEnqueue(NavigationDispatcherPriority.High, () => status.OnEntityLoading(args));
    }

    public static void RaiseNavigationCompleted(this Navigator navigator, NavigationResponse response)
    {
        var status = (NavigationStatus)navigator.NavigationStatus;
        var dispatcher = navigator.Dispatcher;
        if (dispatcher.HasThreadAccess)
            status.OnNavigationCompleted(response);
        else
            dispatcher.TryEnqueue(NavigationDispatcherPriority.High, () => status.OnNavigationCompleted(response));
    }
}
