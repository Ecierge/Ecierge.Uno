namespace Ecierge.Uno.Navigation;

using System;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Dispatching;

public interface INavigationStatus
{
    event EventHandler<Lazy<Routing.Route>> NavigationStarted;
    event EventHandler<NavigationResponse> NavigationFinished;
}

internal class NavigationStatus : INavigationStatus
{
    public event EventHandler<Lazy<Routing.Route>>? NavigationStarted;
    public event EventHandler<NavigationResponse>? NavigationFinished;

    public void OnNavigationStarted(Lazy<Routing.Route> route) => NavigationStarted?.Invoke(this, route);
    public void OnNavigationCompleted(NavigationResponse route) => NavigationFinished?.Invoke(this, route);
}

internal static class NavigationStatusExtensions
{
    public static void RaiseNavigationStarted(this Navigator navigator, Lazy<Routing.Route> route)
    {
        var status = (NavigationStatus)navigator.NavigationStatus;
        var dispatcher = navigator.Target.DispatcherQueue;
        if (dispatcher.HasThreadAccess)
            status.OnNavigationStarted(route);
        else
            dispatcher.TryEnqueue(DispatcherQueuePriority.High, () => status.OnNavigationStarted(route));
    }
    public static void RaiseNavigationStarted(this Navigator navigator, Func<Routing.Route> routeFactory)
    {
        var status = (NavigationStatus)navigator.NavigationStatus;
        var dispatcher = navigator.Target.DispatcherQueue;
        if (dispatcher.HasThreadAccess)
            status.OnNavigationStarted(new Lazy<Routing.Route>(routeFactory));
        else
            dispatcher.TryEnqueue(DispatcherQueuePriority.High, () => status.OnNavigationStarted(new Lazy<Routing.Route>(routeFactory)));
    }

    public static void RaiseNavigationCompleted(this Navigator navigator, NavigationResponse response)
    {
        var status = (NavigationStatus)navigator.NavigationStatus;
        var dispatcher = navigator.Target.DispatcherQueue;
        if (dispatcher.HasThreadAccess)
            status.OnNavigationCompleted(response);
        else
            dispatcher.TryEnqueue(DispatcherQueuePriority.High, () => status.OnNavigationCompleted(response));
    }
}
