namespace Ecierge.Uno.Navigation;

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Ecierge.Uno.Navigation.Regions;

using global::Uno;

/// <summary>
/// Implementation of navigation for a specific region type
/// </summary>
public abstract class Navigator
{
    internal NavigationRegion? Region { get; set; }

    protected NavigationScope Scope => Region!.Scope;
    protected IServiceProvider ServiceProvider => Scope.ServiceProvider;

    public Navigator? Parent { get; internal set; }

    /// <summary>
    /// Gets the current route of the navigator
    /// </summary>
    public Route? Route => throw new NotImplementedException();

    /// <summary>
    /// Determines whether the navigator can navigate to the specified route
    /// </summary>
    /// <param name="route">The route to test whether navigation is possible</param>
    /// <returns>Awaitable value indicating whether navigation is possible</returns>
    public Task<bool> CanNavigate(Route route) => throw new NotImplementedException();

    /// <summary>
    /// Navigates to a specific request
    /// </summary>
    /// <param name="request">The request to navigate to</param>
    /// <returns>The navigation response (awaitable)</returns>
    public abstract Task<NavigationResponse> NavigateAsync(NavigationRequest request);
}

public static class NavigatorExtensions
{
    public static Task<NavigationResponse> NavigateAsync([NotNull] this Navigator navigator, NameSegment segment, IReadOnlyDictionary<string, object>? QueryParameters = null) =>
        navigator.NavigateAsync(new NameSegmentNavigationRequest(navigator, segment, QueryParameters));

    public static Task<NavigationResponse> NavigateAsync<TRouteData>([NotNull] this Navigator navigator, DataSegment segment, TRouteData? routeData, IReadOnlyDictionary<string, object>? QueryParameters = null) =>
        navigator.NavigateAsync(new DataSegmentNavigationRequest<TRouteData>(navigator, segment, routeData, QueryParameters));


    public static Task<NavigationResponse> NavigateDefaultAsync([NotNull] this Navigator navigator, RouteSegment segment, IReadOnlyDictionary<string, object>? QueryParameters = null)
    {
        var defaultSegment = segment.Nested.SingleOrDefault(x => x.IsDefault);
        if (defaultSegment is not null)
        {
            return navigator.NavigateAsync(defaultSegment);
        }
        else
        {
            throw new InvalidOperationException("No default segment found");
        }
    }
}
