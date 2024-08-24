namespace Ecierge.Uno.Navigation;

public abstract record NavigationResponse
{
    public abstract bool Success { get; }
    public Routing.Route Route { get; init; }
    public Navigator Navigator { get; init; }

    public NavigationResponse(Routing.Route route, Navigator navigator)
    {
        Route = route;
        Navigator = navigator ?? throw new ArgumentNullException(nameof(navigator));
    }
}

public record NavigationFailedResponse(Routing.Route Route, Navigator Navigator) : NavigationResponse(Route, Navigator)
{
    public override bool Success => false;

    public NavigationFailedResponse(Navigator navigator, string route, INavigationData? navigationData = null)
        : this(navigator.ParseRoute(route, navigationData), navigator) { }
}

public record NavigationSuccessfulResponse(Routing.Route Route, Navigator Navigator) : NavigationResponse(Route, Navigator)
{
    public override bool Success => true;
}

public record NoDefaultSegmentNavigationResponse(Routing.Route Route, Navigator Navigator) : NavigationResponse(Route, Navigator)
{
    public override bool Success => true;
}

public record ResultNavigationResponse<TResult>(Routing.Route Route, Navigator Navigator, TResult Result) : NavigationResponse(Route, Navigator)
{
    public override bool Success => true;
}
