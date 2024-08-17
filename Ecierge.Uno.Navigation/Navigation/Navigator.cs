namespace Ecierge.Uno.Navigation;

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading.Tasks;

using Ecierge.Uno.Navigation.Navigation;
using Ecierge.Uno.Navigation.Regions;
using Ecierge.Uno.Navigation.Routing;

using Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Implementation of navigation for a specific region type
/// </summary>
public abstract class Navigator
{
    // Region is always set immediately after construction in the NavigationRegion constructor
    internal NavigationRegion Region { get; set; } = default!;

    protected NavigationScope Scope => Region.Scope;
    protected IServiceProvider ServiceProvider => Scope.ServiceProvider;

    public Navigator? Parent { get; internal set; }
    public Navigator? Child { get; internal set; }

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
    public abstract ValueTask<NavigationResponse> NavigateAsync(NavigationRequest request);
}

public static class NavigatorExtensions
{
    public static NameSegment FindNestedSegmentToNavigate([NotNull] this Navigator navigator, string segmentName)
    {
        NameSegment segment = navigator.Region.Segment;
        ImmutableArray<NameSegment> nested;
        if (segment.Data is DataSegment dataSegment)
            nested = dataSegment.Nested;
        else
            nested = segment.Nested;

        var nestedSegment = nested.FirstOrDefault(x => x.Name == segmentName);
        if (nestedSegment is not null)
        {
            return nestedSegment;
        }
        else
        {
            throw new NestedSegmentMissingException(segment.Name, segmentName);
        }
    }

    public static ValueTask<NavigationResponse> NavigateSegmentAsync([NotNull] this Navigator navigator, object initiator, NameSegment segment, IReadOnlyDictionary<string, object>? queryParameters = null)
    {
        if (segment.Data is DataSegment dataSegment && dataSegment.IsMandatory)
        {
            if (queryParameters is null || !queryParameters.ContainsKey(dataSegment.Name))
            {
                throw new InvalidOperationException($"No data segment value found with name '{dataSegment.Name}'");
            }
        }
        return navigator.NavigateAsync(new NameSegmentNavigationRequest(initiator, segment, queryParameters));
    }

    public static ValueTask<NavigationResponse> NavigateNestedSegmentAsync<TRouteData>([NotNull] this Navigator navigator, object initiator, DataSegment segment, TRouteData? routeData, IReadOnlyDictionary<string, object>? QueryParameters = null)
     => navigator.NavigateAsync(new DataSegmentNavigationRequest<TRouteData>(initiator, segment, routeData, QueryParameters));


    public static ValueTask<NavigationResponse> NavigateDefaultAsync([NotNull] this Navigator navigator, object initiator, RouteSegment segment, IReadOnlyDictionary<string, object>? queryParameters = null)
    {
        var defaultSegment = segment.Nested.SingleOrDefault(x => x.IsDefault);
        if (defaultSegment is not null)
        {
            return navigator.NavigateSegmentAsync(initiator, defaultSegment);
        }
        else
        {
            throw new InvalidOperationException("No default segment found");
        }
    }

    public static ValueTask<NavigationResponse> NavigateNestedAsync([NotNull] this Navigator navigator, object initiator, string segmentName, IReadOnlyDictionary<string, object>? queryParameters = null)
    {
        var childNavigator = navigator.Child;
        if (childNavigator is null)
        {
            throw new InvalidOperationException("No child navigator found");
        }
        var nestedSegment = childNavigator.FindNestedSegmentToNavigate(segmentName);
        return childNavigator.NavigateSegmentAsync(initiator, nestedSegment, queryParameters);
    }

    public static ValueTask<NavigationResponse> NavigateLocalAsync([NotNull] this Navigator navigator, object initiator, string segmentName, IReadOnlyDictionary<string, object>? queryParameters = null)
    {
        var nestedSegment = navigator.FindNestedSegmentToNavigate(segmentName);
        return navigator.NavigateSegmentAsync(initiator, nestedSegment, queryParameters);
    }
}
