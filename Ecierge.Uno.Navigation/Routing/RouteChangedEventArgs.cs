namespace Ecierge.Uno.Navigation;

/// <summary>
/// Event args for when a route has changed
/// </summary>
public class RouteChangedEventArgs : EventArgs
{
    /// <summary>
    /// The root region where the route has changed
    /// </summary>
    public Regions.NavigationRegion Region { get; }

    /// <summary>
    /// The navigator where the route changed (leaf region in hierarchy)
    /// </summary>
    public Navigator? Navigator { get; }

    /// <summary>
    /// The outcome of the navigation that ended, if it was started by navigating a route.
    /// </summary>
    public NavigationResponse? Response { get; init; }

    /// <summary>
    /// Constructs a new instance
    /// </summary>
    /// <param name="region">The root region where route was changed</param>
    /// <param name="navigator">The navigator that changed the route</param>
    public RouteChangedEventArgs(Regions.NavigationRegion region, Navigator? navigator)
    {
        Region = region;
        Navigator = navigator;
    }
}
