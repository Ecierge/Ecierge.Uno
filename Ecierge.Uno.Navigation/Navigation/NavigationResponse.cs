namespace Ecierge.Uno.Navigation;

using System;
using System.Collections.Generic;
using System.Threading.Tasks;

/// <summary>
/// The outcome of navigating a whole route, through all the navigators that its segments pass.
/// </summary>
public abstract record NavigationResponse
{
    /// <summary>
    /// Gets a value that tells whether the route was navigated.
    /// </summary>
    public abstract bool Success { get; }

    /// <summary>
    /// Gets the route that was navigated, or for a failure the route up to the segment that failed.
    /// </summary>
    public Routing.Route Route { get; init; }

    /// <summary>
    /// Gets the navigator that the navigation was started on.
    /// </summary>
    public Navigator Navigator { get; init; }

    /// <summary>
    /// Gets how the navigation was started.
    /// </summary>
    public NavigationMode Mode { get; init; } = NavigationMode.New;

    /// <summary>
    /// Creates a response.
    /// </summary>
    /// <param name="route">The route of the response.</param>
    /// <param name="navigator">The navigator that the navigation was started on.</param>
    protected NavigationResponse(Routing.Route route, Navigator navigator)
    {
        Route = route;
        Navigator = navigator ?? throw new ArgumentNullException(nameof(navigator));
    }
}

/// <summary>
/// The route could not be navigated.
/// </summary>
/// <param name="Route">The route up to the segment that failed.</param>
/// <param name="Navigator">The navigator that the navigation was started on.</param>
public record NavigationFailedResponse(Routing.Route Route, Navigator Navigator) : NavigationResponse(Route, Navigator)
{
    /// <inheritdoc/>
    public override bool Success => false;

    /// <summary>
    /// Gets the exception that ended the navigation, for example the failure of loading the entity of a data segment.
    /// </summary>
    public Exception? Error { get; init; }

    /// <summary>
    /// Gets the reasons that the navigators gave.
    /// </summary>
    public IReadOnlyList<string> Errors { get; init; } = Array.Empty<string>();

    /// <summary>
    /// Gets the whole route that was requested, from the root.
    /// </summary>
    public Routing.Route? RequestedRoute { get; init; }

    /// <summary>
    /// Gets a function that repeats the navigation, or <see langword="null"/> if it cannot be repeated. The pages
    /// that are shown already stay, and an entity that failed to load loads again, so a retry succeeds when the cause
    /// of the failure, such as a server that was down, is gone.
    /// </summary>
    public Func<ValueTask<NavigationResponse>>? Retry { get; init; }

    /// <summary>
    /// Creates a response for a route that is given as text.
    /// </summary>
    /// <param name="navigator">The navigator that the navigation was started on.</param>
    /// <param name="route">The route text.</param>
    /// <param name="navigationData">The data of the route.</param>
    public NavigationFailedResponse(Navigator navigator, string route, INavigationData? navigationData = null)
        : this(navigator.ParseRoute(route, navigationData), navigator) { }
}

/// <summary>
/// The navigation rules refused the route.
/// </summary>
/// <param name="Route">The route up to the segment that was refused.</param>
/// <param name="Navigator">The navigator that the navigation was started on.</param>
/// <param name="Reasons">The reasons that the rules gave.</param>
public record NavigationDeniedResponse(Routing.Route Route, Navigator Navigator, IReadOnlyList<string> Reasons)
    : NavigationFailedResponse(Route, Navigator)
{
    /// <inheritdoc/>
    public override bool Success => false;
}

/// <summary>
/// The route was navigated.
/// </summary>
/// <param name="Route">The route that is current now.</param>
/// <param name="Navigator">The navigator that the navigation was started on.</param>
public record NavigationSuccessfulResponse(Routing.Route Route, Navigator Navigator) : NavigationResponse(Route, Navigator)
{
    /// <inheritdoc/>
    public override bool Success => true;
}

/// <summary>
/// The segment has no default nested segment to navigate to.
/// </summary>
/// <param name="Route">The route that is current.</param>
/// <param name="Navigator">The navigator that the navigation was started on.</param>
public record NoDefaultSegmentNavigationResponse(Routing.Route Route, Navigator Navigator) : NavigationResponse(Route, Navigator)
{
    /// <inheritdoc/>
    public override bool Success => true;
}

/// <summary>
/// The route was navigated and produced a result.
/// </summary>
/// <typeparam name="TResult">The type of the result.</typeparam>
/// <param name="Route">The route that is current now.</param>
/// <param name="Navigator">The navigator that the navigation was started on.</param>
/// <param name="Result">The result.</param>
public record ResultNavigationResponse<TResult>(Routing.Route Route, Navigator Navigator, TResult Result) : NavigationResponse(Route, Navigator)
{
    /// <inheritdoc/>
    public override bool Success => true;
}
