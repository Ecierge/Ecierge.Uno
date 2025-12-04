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
    public FrameworkElement Target => ServiceProvider.GetRequiredService<FrameworkElement>();
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

    public async Task<NavigationRuleResult> IsAllowedToNavigateAsync(Routing.Route route)
    {
        var checkers = ServiceProvider.GetServices<INavigationRuleChecker>().ToList();
        bool isProhibited = false;
        bool isAllowed = false;
        var errors = new List<string>();
        var exceptions = new List<Exception>();
        foreach (var checker in checkers)
        {
            try
            {
                var ruleResult = await checker.CanNavigateAsync(route);
                if (!ruleResult.IsAllowed)
                {
                    isProhibited = true;
                    errors.AddRange(ruleResult.Reasons);
                }
                else
                {
                    isAllowed = true;
                }
            }
            catch (Exception ex)
            {
                exceptions.Add(ex);
                Logger.LogError(ex, "Navigation rule checker of type {type} crashed", checker.GetType());
            }
        }

        if (isProhibited && !isAllowed)
        {
            return NavigationRuleResult.Deny(errors);
        }
        if (!isAllowed && exceptions.Any())
        {
            return NavigationRuleResult.Deny("All rule checkers crashed");
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
            case PrimitiveDataSegmentNavigationRequest dataRequest:
                DataSegment segment = dataRequest.Segment;
                var primitive = dataRequest.RouteDataPrimitive;
                route = parentRoute.Add(segment, primitive, null, request.Route.Data);
                break;
            case TaskDataSegmentNavigationRequest dataRequest:
                segment = dataRequest.Segment;
                primitive = dataRequest.RouteDataPrimitive;
                var data = dataRequest.RouteDataTask;
                route = parentRoute.Add(segment, primitive, data, request.Route.Data);
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
        // Merge navigation data first, so rules see non-null Data
        var mergedRoute = request.Route with
        {
            Data = (this.Parent?.Route.Data ?? NavigationData.Empty).Union(request.Route.Data)
        };
        request = request with { Route = mergedRoute };

        var navigationRuleResult = await IsAllowedToNavigateAsync(mergedRoute);
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
        // TODO: Back request must be resolved into named before passing
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
            // If it is data requests save new request with data load task
            request = result.Request!;
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
        if (this.Target is null)
        {
            // No target, no visual tree
            tcs.SetResult();
        }
        else
        {
            this.Target.DispatcherQueue.TryEnqueue(DispatcherQueuePriority.Low, () => tcs.SetResult());
            await tcs.Task;
        }
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
        navigationData = (navigator.Parent?.Route.Data ?? NavigationData.Empty).Union(navigationData);

        DataSegmentInstance CreateDataSegment(DataSegment segment, string primitive)
        {
            Task? data = null;
            var dataMap = segment.DataMap is null ? null : (INavigationDataMap)navigator.ServiceProvider.GetRequiredService(segment.DataMap);
            if (dataMap is not null)
            {
                dataMap.TryGetEntityTask(navigationData, segment.Name, out data);
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
                DataSegment => throw new NotSupportedException("Impossible case as nested for name segment with data is empty"),
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
                        if (dataSegmentInstance.Data is { } task)
                        {
                            // If data is not task we need to replace it so that all child navigators receive it during navigation
                            var key = dataSegmentInstance.DataSegment.Name;
                            var newData = route.Data.SetItem(key, dataSegmentInstance.Data!);
                            route = route with { Data = newData };
                            result = await currentNavigator.NavigateAsync(new TaskDataSegmentNavigationRequest(initiator, dataSegmentInstance.DataSegment, dataSegmentInstance.Primitive, task, route));
                        }
                        else
                            result = await currentNavigator.NavigateAsync(new PrimitiveDataSegmentNavigationRequest(initiator, dataSegmentInstance.DataSegment, dataSegmentInstance.Primitive, route));
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
                // TODO: Handle case then navigation is failed due to permission check
                // Introduce denied result subclass and short-circuit in that case
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
            var route = defaultSegment.BuildDefaultRoute(navigator.ServiceProvider);
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
        var route = nestedSegment.BuildDefaultRoute(navigator.ServiceProvider, data);
        return childNavigator.NavigateRouteAsync(initiator, route);
    }

    public static ValueTask<NavigationResponse> NavigateLocalSegmentAsync([NotNull] this Navigator navigator, object initiator, string segmentName, object? data = null)
    {
        var nestedSegment = navigator.FindNestedSegmentToNavigate(segmentName);
        return navigator.NavigateLocalSegmentAsync(initiator, nestedSegment, data);
    }

    public static ValueTask<NavigationResponse> NavigateLocalSegmentAsync([NotNull] this Navigator navigator, object initiator, NameSegment segment, object? data = null)
    {
        var route = segment.BuildDefaultRoute(navigator.ServiceProvider, data);
        return navigator.NavigateRouteAsync(initiator, route);
    }

    public static ValueTask<NavigationResponse> NavigateDialogSegmentAsync([NotNull] this Navigator navigator, object initiator, string segmentName, object? data = default)
    {
        var dialogSegment = navigator.FindDialogSegmentToNavigate(segmentName);
        var route = dialogSegment.BuildDefaultRoute(navigator.ServiceProvider, data);
        return navigator.NavigateRouteAsync(initiator, route);
    }
}
