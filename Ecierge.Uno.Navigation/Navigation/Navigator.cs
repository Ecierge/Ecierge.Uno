namespace Ecierge.Uno.Navigation;

using System;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading.Tasks;

using Ecierge.Uno.Navigation.Routing;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.UI.Dispatching;

/// <summary>
/// Implementation of navigation for a specific region type
/// </summary>
public abstract class Navigator
{
    // Region is always set immediately after construction in the NavigationRegion constructor
    internal Regions.NavigationRegion Region { get; set; } = default!;
    public Navigator RootNavigator { get; internal set; } = default!;

    protected IServiceProvider ServiceProvider { get; }
    public FrameworkElement Target => ServiceProvider.GetRequiredService<FrameworkElement>();
    protected NavigationScope Scope => ServiceProvider.GetService<NavigationScope>()!;
    private Lazy<ILogger> logger;
    protected ILogger Logger => logger.Value;

    public Navigator? Parent { get; internal set; }

    WeakReference<Navigator?> child = new WeakReference<Navigator?>(null);
    public Navigator? ChildNavigator
    {
        get { child.TryGetTarget(out var value); return value; }
        internal set => child.SetTarget(value);
    }

    public Navigator(IServiceProvider serviceProvider)
    {
        ServiceProvider = serviceProvider;
        logger = new(() => serviceProvider.GetRequiredService<ILoggerFactory>().CreateLogger(this.GetType()));
    }

    private void SetRoute(NavigationRequest request)
    {
        var parentRoute =
            Parent?.Route
            ?? new Routing.Route(ImmutableArray<RouteSegmentInstance>.Empty, request.NavigationData);

        Routing.Route route;
        switch (request)
        {
            case NameSegmentNavigationRequest nameRequest:
                route = parentRoute.Add(nameRequest.Segment);
                break;
            case DataSegmentNavigationRequest dataRequest:
                DataSegment segment = dataRequest.Segment;
                if (segment.Data is not null)
                {
                    if (dataRequest.RouteData is not null)
                    {
                        var routeData = segment.Data.ToNavigationData(dataRequest.NavigationData, segment.Name, dataRequest.RouteData);
                        route = parentRoute.Add(segment, routeData.Primitive, dataRequest.RouteData) with { Data = routeData.NavigationData };
                    }
                    else
                    {
                        Debugger.Break();
                        // TODO: Implement default value case or allow the last element to be null
                        route = parentRoute.Add(segment, segment.Name, null);
                    }
                }
                else
                {
                    route = parentRoute.Add(segment, dataRequest.RouteData as string, dataRequest.RouteData);
                }
                break;
            // TODO: Implement dialog segment
            default:
                throw new NotSupportedException("Unsupported request type");
        }

        this.Route = route;
    }

    /// <summary>
    /// Gets the current route of the navigator
    /// </summary>
    public Routing.Route Route { get; private set; } = new Routing.Route();

    /// <summary>
    /// Determines whether the navigator can navigate to the specified route
    /// </summary>
    /// <param name="route">The route to test whether navigation is possible</param>
    /// <returns>Awaitable value indicating whether navigation is possible</returns>
    public Task<bool> CanNavigate(Routing.Route route) => throw new NotImplementedException();

    /// <summary>
    /// Navigates to a specific request
    /// </summary>
    /// <param name="request">The request to navigate to</param>
    /// <returns>The navigation response (awaitable)</returns>
    public async ValueTask<NavigationResult> NavigateAsync(NavigationRequest request)
    {
        var result = await NavigateCoreAsync(request);
        FrameworkElement target = Region!.Target!;
        target.SetNestedSegmentName(request.NameSegment.Name);
        if (result.Success)
        {
            SetRoute(request);
            await WaitForVisualTree();
        }
        return result;
    }

    protected abstract ValueTask<NavigationResult> NavigateCoreAsync(NavigationRequest request);

    protected virtual ValueTask WaitForVisualTree() => ValueTask.CompletedTask;
}

public static class NavigatorExtensions
{
    public static Routing.Route ParseRoute([NotNull] this Navigator navigator, string route, INavigationData? navigationData = null)
    {
        DataSegmentInstance CreateDataSegment(DataSegment segment, string primitive)
        {
            Task<object>? data = null;
            if (navigationData is not null)
            {
                data = segment.Data?.FromNavigationData(navigationData, segment.Name);
            }
            return new(segment, primitive, data);
        }

        var segmentNames = route.Split('/');
        List<RouteSegmentInstance> parsedRoute = new(segmentNames.Length);

        RouteSegment? nextSegment = navigator.Region.Segment;
        foreach (var segmentName in segmentNames)
        {
            nextSegment = ProcessSegmentName(nextSegment, segmentName);
        }
        while ((nextSegment = nextSegment?.Nested.FirstOrDefault(s => s.IsDefault)) is not null)
        {
            RouteSegmentInstance instance = nextSegment switch
            {
                NameSegment nameSegment => new NameSegmentInstance(nameSegment),
                // TODO: Handle data segments with default values
                //CreateDataSegment(dataSegment, null),
                DataSegment dataSegment => throw new NotSupportedException("Impossible case as nested for name segment with data is empty"),
                _ => throw new NotSupportedException("Not supported route segment")
            };
            parsedRoute.Add(instance);
        }
        return new Routing.Route(parsedRoute.ToImmutableArray(), navigationData);

        RouteSegment ProcessSegmentName(RouteSegment segment, string segmentName)
        {
            RouteSegment? nextSegment = segment switch
            {
                NameSegment nameSegment when nameSegment.Data is DataSegment nestedDataSegment => nestedDataSegment,
                RouteSegment routeSegment => routeSegment.Nested.FirstOrDefault(s => s.Name == segmentName),
                _ => throw new NotSupportedException("Not supported route segment")
            };
            RouteSegmentInstance instance = nextSegment switch
            {
                // Wrong route
                null => throw new NestedSegmentNotFoundException(segment, segmentName),
                NameSegment nameSegment => new NameSegmentInstance(nameSegment),
                DataSegment dataSegment => CreateDataSegment(dataSegment, segmentName),
                _ => throw new NotSupportedException("Not supported route segment")
            };
            parsedRoute.Add(instance);
            return nextSegment;
        }
    }

    public static async ValueTask<NavigationResponse> NavigateRouteAsync([NotNull] this Navigator navigator, object initiator, string route, INavigationData? navigationData = null)
    {
        var parsedRoute = navigator.ParseRoute(route, navigationData);
        if (parsedRoute.Segments.Length < 1)
            // TODO: Consider another response
            return new NavigationSuccessfulResponse(parsedRoute, navigator);

        return await NavigateRouteAsync(navigator, initiator, parsedRoute);
    }

    public static async ValueTask<NavigationResponse> NavigateRouteAsync(Navigator navigator, object initiator, Routing.Route route)
    {
        var currentNavigator = navigator;
        NavigationResult result = default;
        foreach (var segment in route.NavigatableSegments)
        {
            switch (segment)
            {
                case NameSegmentInstance nameSegmentInstance:
                    result = await currentNavigator.NavigateAsync(new NameSegmentNavigationRequest(initiator, nameSegmentInstance.NameSegment, route.Data));
                    break;
                case DataSegmentInstance dataSegmentInstance:
                    object data = (object?)dataSegmentInstance.Data ?? dataSegmentInstance.Primitive;
                    result = await currentNavigator.NavigateAsync(new DataSegmentNavigationRequest(initiator, dataSegmentInstance.DataSegment, data));
                    break;
                case DialogSegmentInstance _:
                    throw new NotImplementedException("Dialog segments are not implemented");
                default:
                    throw new NotSupportedException("Unknown segment type.");
            }
            if (!result.Success)
            {
                var effectiveRoute = route.TrimTill(segment);
                var parentRoute = currentNavigator.Parent?.Route ?? new Routing.Route();
                var fullRoute = parentRoute.Join(effectiveRoute);
                return new NavigationFailedResponse(fullRoute, navigator);
            }

            currentNavigator = navigator.ChildNavigator!;
        }
        return new NavigationSuccessfulResponse(route, navigator);
    }

    public static NameSegment FindNestedSegmentToNavigate([NotNull] this Navigator navigator, string segmentName)
    {
        NameSegment segment = navigator.Region.Segment;
        ImmutableArray<NameSegment> nested;
        if (segment.Data is DataSegment dataSegment)
            nested = dataSegment.Nested;
        else
            nested = segment.Nested;

        var nestedSegment = nested.FirstOrDefault(x => x.Name == segmentName);
        if (nestedSegment is not null) return nestedSegment;
        else throw new NestedSegmentMissingException(segment.Name, segmentName);
    }

    public static async ValueTask<NavigationResponse> NavigateSegmentAsync([NotNull] this Navigator navigator, object initiator, NameSegment segment, object? data = null)
    {
        NavigationData? navigationData;
        if (segment.Data is DataSegment dataSegment)
        {
            if (dataSegment.IsMandatory && data is null)
                throw new InvalidOperationException($"No data segment value found with name '{dataSegment.Name}'");
            else
                navigationData = new NavigationData([
                    new (dataSegment.Name, data!)
                ]);
        }
        else
        {
            navigationData = null;
        }
        var result = await navigator.NavigateAsync(new NameSegmentNavigationRequest(initiator, segment, navigationData));
        var route = navigator.Route.ReplaceLast(segment);
        if (result.Success)
        {
            await navigator.NavigateNestedDefaultAsync(initiator, segment);
            return new NavigationSuccessfulResponse(route, navigator);
        }
        else return new NavigationFailedResponse(route, navigator);
    }

    public static async ValueTask<NavigationResponse> NavigateNestedSegmentAsync<TRouteData>([NotNull] this Navigator navigator, object initiator, DataSegment segment, TRouteData? routeData)
    {
        var result = await navigator.NavigateAsync(new DataSegmentNavigationRequest(initiator, segment, routeData));
        var data = routeData is null ? null : Task.FromResult<object>(routeData);
        var route = navigator.Route.ReplaceLast(segment, segment.Name, data);
        if (result.Success)
        {
            await navigator.NavigateNestedDefaultAsync(initiator, segment);
            return new NavigationSuccessfulResponse(route, navigator);
        }
        else return new NavigationFailedResponse(route, navigator);
    }


    public static async ValueTask<NavigationResponse> NavigateDefaultAsync([NotNull] this Navigator navigator, object initiator, RouteSegment segment)
    {
        var defaultSegment = segment.Nested.SingleOrDefault(x => x.IsDefault);
        if (defaultSegment is not null)
        {
            return await navigator.NavigateSegmentAsync(initiator, defaultSegment);
        }
        else
        {
            return new NoDefaultSegmentNavigationResponse(navigator.Route, navigator);
        }
    }

    internal static async ValueTask NavigateNestedDefaultAsync([NotNull] this Navigator navigator, object initiator, RouteSegment segment)
    {
        ImmutableArray<NameSegment> nested;
        if (segment is NameSegment nameSegment && nameSegment.Data is DataSegment dataSegment)
            nested = dataSegment.Nested;
        else
            nested = segment.Nested;

        var defaultSegment = segment.Nested.SingleOrDefault(s => s.IsDefault);
        if (defaultSegment is not null)
        {
            await navigator.ChildNavigator!.NavigateSegmentAsync(initiator, defaultSegment);
        }
    }

    public static ValueTask<NavigationResponse> NavigateNestedSegmentAsync([NotNull] this Navigator navigator, object initiator, string segmentName, object? data = null)
    {
        var childNavigator = navigator.ChildNavigator;
        if (childNavigator is null)
        {
            throw new InvalidOperationException("No child navigator found");
        }
        var nestedSegment = childNavigator.FindNestedSegmentToNavigate(segmentName);
        return childNavigator.NavigateSegmentAsync(initiator, nestedSegment, data);
    }

    public static ValueTask<NavigationResponse> NavigateLocalSegmentAsync([NotNull] this Navigator navigator, object initiator, string segmentName, object? data = null)
    {
        var nestedSegment = navigator.FindNestedSegmentToNavigate(segmentName);
        return navigator.NavigateSegmentAsync(initiator, nestedSegment, data);
    }
}
