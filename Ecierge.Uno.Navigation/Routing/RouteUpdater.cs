namespace Ecierge.Uno.Navigation;

using Ecierge.Uno.Navigation.Regions;

using Microsoft.Extensions.Logging;
using Microsoft.UI.Xaml.Controls;

using System.Globalization;
using System.Text;

public interface IRouteNotifier
{
    event EventHandler<RouteChangedEventArgs> RouteChanged;
}

internal interface IRouteUpdater
{
    void StartNavigation(Navigator navigator, Regions.NavigationRegion region, NavigationRequest request);

    void EndNavigation(Navigator navigator, Regions.NavigationRegion region, NavigationRequest request, NavigationResponse? response);
}

internal class RouteNotifier : IRouteNotifier, IRouteUpdater
{
    public event EventHandler<RouteChangedEventArgs>? RouteChanged;

    private ILogger Logger { get; }

    public RouteNotifier(ILogger<RouteNotifier> logger) => Logger = logger;

    private readonly IDictionary<Guid, StringBuilder> navigationSegments = new Dictionary<Guid, StringBuilder>();
    private readonly IDictionary<Guid, int> runningNavigations = new Dictionary<Guid, int>();

    public void StartNavigation(Navigator navigator, Regions.NavigationRegion region, NavigationRequest request)
    {
        var id = request.Id;
        if (!runningNavigations.TryGetValue(id, out var count) ||
            count == 0)
        {
            runningNavigations[id] = 1;
            navigationSegments[id] = new StringBuilder().AppendLine(CultureInfo.InvariantCulture, $"[{id}] Navigation Start");
            PerformanceTimer.Start(Logger, LogLevel.Trace, id);
        }
        else
        {
            runningNavigations[id] = runningNavigations[id] + 1;
        }
        if (Logger.IsEnabled(LogLevel.Trace))
        {
            //navigationSegments[id].AppendLine(CultureInfo.InvariantCulture, $"[{id} - {PerformanceTimer.Split(id).TotalMilliseconds}] {navigator.GetType().Name} - {region.Segment.Name ?? "unnamed"} - {request.Route} {(request.Route.IsInternal ? "(internal)" : "")}");
        }
    }

    public void EndNavigation(Navigator navigator, Regions.NavigationRegion region, NavigationRequest request, NavigationResponse? response)
    {
        var id = request.Id;
        runningNavigations[id] = runningNavigations[id] - 1;

        if (runningNavigations[id] == 0)
        {
            var elapsed = PerformanceTimer.Stop(Logger, LogLevel.Trace, id);
            if (Logger.IsEnabled(LogLevel.Trace))
            {
                navigationSegments[id].AppendLine(CultureInfo.InvariantCulture, $"[{id} - {elapsed.TotalMilliseconds}] Navigation End");
                Logger.LogTraceMessage($"Post-navigation (summary):\n{navigationSegments[id]}");
            }
            navigationSegments.Remove(id);
            RouteChanged?.Invoke(this, new RouteChangedEventArgs(region, null/*response?.Navigator*/));
        }
    }
}

