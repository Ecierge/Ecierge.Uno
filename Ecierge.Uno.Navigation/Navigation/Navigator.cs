namespace Ecierge.Uno.Navigation;

using System;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading.Tasks;
using Ecierge.Uno.Navigation.Navigators;
using Ecierge.Uno.Navigation.Routing;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.UI.Dispatching;

using MoreLinq;

/// <summary>
/// Implementation of navigation for a specific region type
/// </summary>
public abstract class Navigator
{
    // Region is always set immediately after construction in the NavigationRegion constructor
    protected internal Regions.NavigationRegion Region { get; set; } = default!;
    public Navigator RootNavigator { get; internal set; } = default!;

    public IServiceProvider ServiceProvider { get; }
    public FrameworkElement? Target => ServiceProvider.GetService<FrameworkElement>();
    protected NavigationScope Scope => ServiceProvider.GetService<NavigationScope>()!;
    public INavigationStatus NavigationStatus { get; private set; }

    private Lazy<ILogger> logger;
    protected ILogger Logger => logger.Value;

    public Navigator? Parent { get; internal set; }

    WeakReference<Navigator?> child = new WeakReference<Navigator?>(null);
    public Navigator? ChildNavigator
    {
        get { child.TryGetTarget(out var value); return value; }
        internal set
        {
            if (value is not null) value.Route = this.Route;
            child.SetTarget(value);
        }
    }

    public Navigator LeafNavigator
    {
        get
        {
            Navigator leafNavigator = this;
            while (leafNavigator.ChildNavigator is not null)
            {
                leafNavigator = leafNavigator.ChildNavigator;
            }
            return leafNavigator;
        }
    }

    /// <summary>
    /// Gets the current route of the navigator
    /// </summary>
    public Routing.Route Route { get; private set; } = Routing.Route.Empty;

    public Routing.Route RequestedRoute { get; private set; } = Routing.Route.Empty;
    public Routing.Route ActualRoute => LeafNavigator.Route;

    public Routing.Route TailRoute => LeafNavigator.Route.TrimHead(Route);

    private Stack<NavigationRequest> navigationStack { get; } = new();
    public IReadOnlyCollection<NavigationRequest> NavigationHistory => navigationStack;

    public Navigator(IServiceProvider serviceProvider)
    {
        ServiceProvider = serviceProvider;
        logger = new(() => serviceProvider.GetRequiredService<ILoggerFactory>().CreateLogger(this.GetType()));
        NavigationStatus = ServiceProvider.GetRequiredService<INavigationStatus>();
    }

    public async Task<NavigationRuleResult> IsAllowedToNavigateAsync(string route)
    {
        var checkers = ServiceProvider.GetServices<INavigationRuleChecker>().ToList();
        bool isAllowed = true;
        var errors = new List<string>();
        foreach (var checker in checkers)
        {
            var ruleResult = await checker.CanNavigateAsync(route);
            if (!ruleResult.IsAllowed)
            {
                isAllowed = false;
                errors.AddRange(ruleResult.Reasons);
            }
        }

        if (isAllowed)
        {
            return NavigationRuleResult.Allow();
        }
        
        return NavigationRuleResult.Deny(errors);
    }

    private void SetRoute(NavigationRequest request)
    {
        var parentRoute =
            Parent?.Route
            ?? new Routing.Route(request.Route.Data);

        // TODO: Optimize
        Routing.Route route;
        switch (request)
        {
            case NameSegmentNavigationRequest nameRequest:
                route = parentRoute.Add(nameRequest.Segment, request.Route.Data);
                break;
            case DataSegmentNavigationRequest dataRequest:
                DataSegment segment = dataRequest.Segment;
                if (segment.DataMap is not null)
                {
                    if (dataRequest.RouteData is not null)
                    {
                        var dataMap = (INavigationDataMap)ServiceProvider.GetRequiredService(segment.DataMap);
                        var routeData = dataMap.ToNavigationData(dataRequest.Route.Data, segment.Name, dataRequest.RouteData);
                        route = parentRoute.Add(segment, routeData.Primitive, dataRequest.RouteData, request.Route.Data);
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
                    route = parentRoute.Add(segment, dataRequest.RouteData as string, dataRequest.RouteData, request.Route.Data);
                }
                break;
            case DialogSegmentNavigationRequest dialogRequest:
                route = parentRoute.Add(dialogRequest.Segment);
                break;
            default:
                throw new NotSupportedException("Unsupported request type");
        }

        this.Route = route;
    }

    /// <summary>
    /// Determines whether the navigator can navigate to the specified route
    /// </summary>
    /// <param name="route">The route to test whether navigation is possible</param>
    /// <returns>Awaitable value indicating whether navigation is possible</returns>
    public Task<bool> CanNavigate(Routing.Route route) => throw new NotImplementedException();

    public void ClearHistory() => navigationStack.Clear();

    /// <summary>
    /// Navigates to a specific request
    /// </summary>
    /// <param name="request">The request to navigate to</param>
    /// <returns>The navigation response (awaitable)</returns>
    public async ValueTask<NavigationResult> NavigateAsync(NavigationRequest request)
    {
        var navigationRuleResult = await IsAllowedToNavigateAsync(request.Route.ToString());
        if (!navigationRuleResult.IsAllowed)
        {
            return new NavigationResult(navigationRuleResult.Reasons);
        }

        if (request is DialogSegmentNavigationRequest dialogRequest && !dialogRequest.Handle)
            return await NavigateDialogAsync(dialogRequest);

        var oldChildNavigator = ChildNavigator;
        var oldRequestedRoute = RequestedRoute;
        // When the same level navigation happened, the child navigator should be null
        // if no nested navigation happened
        ChildNavigator = null;
        RequestedRoute = request.Route;
        request.Route.ApplyScopedInstanceServices(ServiceProvider);

        var dispatcherQueue = this.Target?.DispatcherQueue ?? Parent?.Target?.DispatcherQueue;
#if DEBUG
        if (dispatcherQueue is null)
            Debug.Fail("Must be at least a parent with DispatcherQueue");
#endif
        var result = await dispatcherQueue!.EnqueueAsync(() => { return NavigateCoreAsync(request); });

        if (result.IsSkipped)
        {
            // Restore child navigator
            ChildNavigator = oldChildNavigator;
            RequestedRoute = oldRequestedRoute;
            oldChildNavigator?.Route.ApplyScopedInstanceServices(ServiceProvider);
        }
        FrameworkElement target = Region!.Target!;
        if (result.Success)
        {
            target.SetNestedSegmentName(request.NameSegment.Name);
            SetRoute(request);
            await WaitForVisualTree();
        }
        else
        {
            // Restore child navigator
            ChildNavigator = oldChildNavigator;
        }
        return result;
    }

    private ValueTask<NavigationResult> NavigateDialogAsync(DialogSegmentNavigationRequest request)
    {
        var dialogScope = Scope.CreateDialogScope(request.Segment, this);
        var navigator = (ContentDialogNavigator)dialogScope.ServiceProvider.GetRequiredService<Navigator>();
        navigator.Region = new Regions.NavigationRegion(dialogScope);
        request.Handle = true;
        return navigator.NavigateAsync(request);
    }

    protected abstract ValueTask<NavigationResult> NavigateCoreAsync(NavigationRequest request);

    public async Task WaitForVisualTreeAsync()
    {
        var tcs = new TaskCompletionSource();
        this.Target.DispatcherQueue.TryEnqueue(DispatcherQueuePriority.Low, () => tcs.SetResult());
        await tcs.Task;
    }

    protected virtual ValueTask WaitForVisualTree() => ValueTask.CompletedTask;

    public virtual ValueTask<NavigationResult> NavigateBackAsync(object initator)
    {
        if (navigationStack.TryPop(out var request))
            return NavigateAsync(request with { Sender = initator });
        else
            return new(new NavigationResult("Navigation history is empty"));
    }
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
                var dataMap = (INavigationDataMap)navigator.ServiceProvider.GetRequiredService(segment.DataMap!);
                data = dataMap.FromNavigationData(navigationData, segment.Name);
            }
            return new(segment, primitive, data);
        }

        var segmentNames = route.Split('/');
        List<RouteSegmentInstance> parsedRoute = new(segmentNames.Length);

        // Get the last navigatable segment from which nested segments will be searched within
        RouteSegment? nextSegment = navigator.Region.Segment switch
        {
            NameSegment nameSegment when nameSegment.DataSegment is DataSegment nestedDataSegment => nestedDataSegment,
            RouteSegment routeSegment => routeSegment
        };
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
                // The next is data
                NameSegment nameSegment when nameSegment.DataSegment is DataSegment nestedDataSegment => nestedDataSegment,
                // The next is dialog
                RouteSegment routeSegment when segmentName.StartsWith('!') => navigator.FindDialogSegmentToNavigate(segmentName[1..]),
                // The next is nested
                RouteSegment routeSegment => routeSegment.Nested.FirstOrDefault(s => s.Name == segmentName),
                _ => throw new NotSupportedException("Not supported route segment")
            };
            RouteSegmentInstance instance = nextSegment switch
            {
                // Wrong route
                null => throw new NestedSegmentNotFoundException(segment, segmentName),
                DialogSegment dialogSegment => new DialogSegmentInstance(dialogSegment),
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

    public static async ValueTask<NavigationResponse> NavigateRouteAsync([NotNull] this Navigator navigator, object initiator, Routing.Route route)
    {
        navigator.RaiseNavigationStarted(() => route);
        // TODO: Ensure that navigation item mapping resolution happens only once
        route = route with { Data = (navigator.Parent?.Route.Data ?? NavigationData.Empty).Union(route.Data) };
        NavigationResponse? response = null;
        var currentNavigator = navigator;
        NavigationResult result = default;
        async ValueTask<NavigationResponse> CoreNavigateRouteAsync()
        {
            var navigatableSegments = route.GetNavigatableSegments();
            for (int i = 0; i < navigatableSegments.Length; i++)
            {
                var segment = navigatableSegments[i];
                switch (segment)
                {
                    case NameSegmentInstance nameSegmentInstance:
                        result = await currentNavigator.NavigateAsync(new NameSegmentNavigationRequest(initiator, nameSegmentInstance.NameSegment, route));
                        break;
                    case DataSegmentInstance dataSegmentInstance:
                        object data = (object?)dataSegmentInstance.Data ?? dataSegmentInstance.Primitive;
                        result = await currentNavigator.NavigateAsync(new DataSegmentNavigationRequest(initiator, dataSegmentInstance.DataSegment, data, route));
                        break;
                    case DialogSegmentInstance dialogSegmentInstance:
                        var parentSegment =
                            i > 0 ?
                            navigatableSegments[i - 1].Segment :
                            navigator.Region.Segment;
                        result = await currentNavigator.NavigateAsync(new DialogSegmentNavigationRequest(initiator, dialogSegmentInstance.DialogSegment, parentSegment, route));
                        // The next navigator must be inside the dialog instead of ContentDialogNavigator
                        currentNavigator = currentNavigator.ChildNavigator!;
                        break;
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

                // Child navigator is created after UI is created and attached to a visual tree
                await currentNavigator.WaitForVisualTreeAsync();
                if (currentNavigator.ChildNavigator is not null)
                    currentNavigator = currentNavigator.ChildNavigator;
            }
            return new NavigationSuccessfulResponse(currentNavigator.ActualRoute, navigator);
        }

        try
        {
            response = await CoreNavigateRouteAsync();
            return response;
        }
        finally
        {
            response = response ?? new NavigationFailedResponse(route, navigator);
            navigator.RaiseNavigationCompleted(response);
        }
    }

    public static TSegment? FindNestedSegmentToNavigateCore<TSegment>([NotNull] this Navigator navigator, Func<ImmutableArray<NameSegment>, IEnumerable<TSegment>> filter, string segmentName)
        where TSegment : NameSegment
    {
        NameSegment segment = navigator.Region.Segment;
        ImmutableArray<NameSegment> nested;
        if (segment.DataSegment is DataSegment dataSegment)
            nested = dataSegment.Nested;
        else
            nested = segment.Nested;

        return filter(nested).FirstOrDefault(x => x.Name == segmentName);
    }

    public static NameSegment FindNestedSegmentToNavigate([NotNull] this Navigator navigator, string segmentName)
    {
        IEnumerable<NameSegment> OfExactType(ImmutableArray<NameSegment> segments) => segments.Where(s => s.GetType() == typeof(NameSegment));
        var nestedSegment = FindNestedSegmentToNavigateCore(navigator, OfExactType, segmentName);
        if (nestedSegment is not null) return nestedSegment;
        else throw new NestedSegmentMissingException(segmentName, navigator.Region.Segment.Name);
    }

    public static DialogSegment FindDialogSegmentToNavigate([NotNull] this Navigator navigator, string segmentName)
    {
        Navigator targetNavigator = navigator.ChildNavigator ?? navigator;
        Navigator? currentNavigator = targetNavigator;
        DialogSegment? dialogSegment = null;
        do
        {
            dialogSegment = FindNestedSegmentToNavigateCore(currentNavigator, s => s.OfType<DialogSegment>(), segmentName);

        } while (dialogSegment is null && (currentNavigator = currentNavigator!.Parent) is not null);
        if (dialogSegment is not null) return dialogSegment;
        else throw new NestedSegmentMissingException(segmentName, targetNavigator.Region.Segment.Name);
    }

    //public static async ValueTask<NavigationResponse> NavigateSegmentAsync([NotNull] this Navigator navigator, object initiator, NameSegment segment, object? data = null)
    //{
    //    NavigationResponse? response = null;
    //    async ValueTask<NavigationResponse> CoreNavigateSegmentAsync()
    //    {
    //        NavigationResult result;
    //        NavigationData? navigationData = data as NavigationData;
    //        INavigationData? oldNavigationData = navigator.ActualRoute.TrimTill(navigator.Route.LastNamedSegment).Data ?? NavigationData.Empty;
    //        INavigationData ? routeNavigationData = oldNavigationData.Union(navigationData);

    //        if (segment.Data is DataSegment dataSegment)
    //        {
    //            object? routeData;
    //            if (navigationData == default)
    //                routeData = data;
    //            else
    //                routeData = null;

    //            // TODO: Get primitive
    //            navigator.RaiseNavigationStarted(() => navigator.Route.Add(dataSegment, data as string, routeData));

    //            if (dataSegment.IsMandatory && data is null)
    //                throw new InvalidOperationException($"No data segment value found with name '{dataSegment.Name}'");

    //            result = await navigator.NavigateAsync(new DataSegmentNavigationRequest(initiator, dataSegment, routeData, routeNavigationData));
    //            if (result.Success)
    //            {
    //                await navigator.WaitForVisualTreeAsync();
    //                await navigator.NavigateNestedDefaultAsync(initiator, segment);
    //                return new NavigationSuccessfulResponse(navigator.ActualRoute, navigator);
    //            }
    //        }
    //        else
    //        {
    //            navigator.RaiseNavigationStarted(() => navigator.Route.Add(segment));
    //            if (segment is DialogSegment dialogSegment)
    //            {
    //                result = await navigator.NavigateAsync(new DialogSegmentNavigationRequest(initiator, dialogSegment, navigator.Region.Segment, routeNavigationData));
    //                if (result.Success)
    //                {
    //                    await navigator.WaitForVisualTreeAsync();
    //                    await navigator.LeafNavigator.NavigateDefaultAsync(initiator, segment);
    //                    return new NavigationSuccessfulResponse(navigator.ActualRoute, navigator);
    //                }
    //            }
    //            else
    //            {
    //                result = await navigator.NavigateAsync(new NameSegmentNavigationRequest(initiator, segment, routeNavigationData));
    //                if (result.Success)
    //                {
    //                    await navigator.WaitForVisualTreeAsync();
    //                    await navigator.NavigateNestedDefaultAsync(initiator, segment);
    //                    return new NavigationSuccessfulResponse(navigator.ActualRoute, navigator);
    //                }
    //            }
    //        }
    //        return new NavigationFailedResponse(navigator.ActualRoute, navigator);
    //    }

    //    try
    //    {
    //        response = await CoreNavigateSegmentAsync();
    //        return response;
    //    }
    //    finally
    //    {
    //        response = response ?? new NavigationFailedResponse(navigator.ActualRoute, navigator);
    //        navigator.RaiseNavigationCompleted(response);
    //    }
    //}

    //public static async ValueTask<NavigationResponse> NavigateSegmentAsync<TRouteData>([NotNull] this Navigator navigator, object initiator, DataSegment segment, TRouteData? routeData)
    //{
    //    NavigationResponse? response = null;
    //    // TODO: Get primitive
    //    navigator.RaiseNavigationStarted(() => navigator.Route.Add(segment, null, routeData));
    //    async ValueTask<NavigationResponse> CoreNavigateSegmentAsync()
    //    {
    //        var result = await navigator.NavigateAsync(new DataSegmentNavigationRequest(initiator, segment, routeData));
    //        if (result.Success)
    //        {
    //            await navigator.WaitForVisualTreeAsync();
    //            await navigator.NavigateNestedDefaultAsync(initiator, segment);
    //            return new NavigationSuccessfulResponse(navigator.ActualRoute, navigator);
    //        }
    //        else
    //        {
    //            return new NavigationFailedResponse(navigator.ActualRoute, navigator);
    //        }
    //    }

    //    try
    //    {
    //        response = await CoreNavigateSegmentAsync();
    //        return response;
    //    }
    //    finally
    //    {
    //        response = response ?? new NavigationFailedResponse(navigator.ActualRoute, navigator);
    //        navigator.RaiseNavigationCompleted(response);
    //    }
    //}

    public static async ValueTask<NavigationResponse> NavigateDefaultAsync([NotNull] this Navigator navigator, object initiator, RouteSegment segment)
    {
        var defaultSegment = segment.Nested.SingleOrDefault(x => x.IsDefault);
        if (defaultSegment is not null)
        {
            var route = defaultSegment.BuildDefaultRoute();
            return await navigator.NavigateRouteAsync(initiator, route);
        }
        else
        {
            return new NoDefaultSegmentNavigationResponse(navigator.Route, navigator);
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
        var route = nestedSegment.BuildDefaultRoute(data);
        return childNavigator.NavigateRouteAsync(initiator, route);
    }

    public static ValueTask<NavigationResponse> NavigateLocalSegmentAsync([NotNull] this Navigator navigator, object initiator, string segmentName, object? data = null)
    {
        var nestedSegment = navigator.FindNestedSegmentToNavigate(segmentName);
        return navigator.NavigateLocalSegmentAsync(initiator, nestedSegment, data);
    }

    public static ValueTask<NavigationResponse> NavigateLocalSegmentAsync([NotNull] this Navigator navigator, object initiator, NameSegment segment, object? data = null)
    {
        var route = segment.BuildDefaultRoute(data);
        return navigator.NavigateRouteAsync(initiator, route);
    }

    public static ValueTask<NavigationResponse> NavigateDialogSegmentAsync([NotNull] this Navigator navigator, object initiator, string segmentName, object? data = default)
    {
        var dialogSegment = navigator.FindDialogSegmentToNavigate(segmentName);
        var route = dialogSegment.BuildDefaultRoute(data);
        return navigator.NavigateRouteAsync(initiator, route);
    }
}
