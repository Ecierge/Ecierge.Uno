namespace Ecierge.Uno.Navigation;

using System;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading.Tasks;

using Ecierge.Uno.Navigation;
using Ecierge.Uno.Navigation.Navigators;
using Ecierge.Uno.Navigation.Routing;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

/// <summary>
/// Implementation of navigation for a specific region type
/// </summary>
public abstract class Navigator
{
    private readonly Lazy<ILogger> logger;
    private readonly WeakReference<Navigator?> child = new(null);

    /// <summary>
    /// Creates a navigator. A navigator is created by the <see cref="NavigationScope"/> of its region, which provides
    /// everything that the navigator needs.
    /// </summary>
    /// <param name="serviceProvider">The services of the scope of the region.</param>
    /// <exception cref="InvalidOperationException">The services have no region, so the navigator was not created by a scope.</exception>
    protected Navigator(IServiceProvider serviceProvider)
    {
        ServiceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        Region = serviceProvider.GetService<Regions.NavigationRegion>()
            ?? throw new InvalidOperationException($"{GetType().Name} must be created by a navigation scope, which provides the region of the navigator.");
        Dispatcher = serviceProvider.GetRequiredInstance<INavigationDispatcher>();
        NavigationStatus = serviceProvider.GetRequiredService<INavigationStatus>();
        logger = new(() => serviceProvider.GetRequiredService<ILoggerFactory>().CreateLogger(this.GetType()));
    }

    /// <summary>
    /// Gets the region that the navigator navigates in.
    /// </summary>
    protected internal Regions.NavigationRegion Region { get; }

    /// <summary>
    /// Gets the services of the scope of the region.
    /// </summary>
    public IServiceProvider ServiceProvider { get; }

    /// <summary>
    /// Gets the dispatcher that the navigation work is run on.
    /// </summary>
    public INavigationDispatcher Dispatcher { get; }

    /// <summary>
    /// Gets the status that announces the start and the end of the navigation of a route.
    /// </summary>
    public INavigationStatus NavigationStatus { get; }

    /// <summary>
    /// Gets the control that the navigator shows the content of its region in, or <see langword="null"/> if the
    /// region has no control, as the region of a dialog that is not shown yet.
    /// </summary>
    public FrameworkElement? Target => Region.Target;

    /// <summary>
    /// Gets the navigator of the region that contains this one, or <see langword="null"/> for the root navigator.
    /// </summary>
    public Navigator? Parent => Region.Parent?.Navigator;

    /// <summary>
    /// Gets the navigator of the root region of the window.
    /// </summary>
    public Navigator RootNavigator => Region.RootRegion.Navigator;

    /// <summary>
    /// Gets a value that tells whether this is the root navigator of a window.
    /// </summary>
    public bool IsRoot => Region.IsRoot;

    /// <summary>
    /// Gets the scope of the services of the region.
    /// </summary>
    protected NavigationScope Scope => Region.Scope;

    /// <summary>
    /// Gets the history of the window: the routes that the user visited. It is the same for all the navigators of
    /// the window.
    /// </summary>
    public NavigationJournal Journal => RootNavigator.ServiceProvider.GetRequiredInstance<NavigationJournal>();

    /// <summary>
    /// Gets the logger of the navigator.
    /// </summary>
    protected ILogger Logger => logger.Value;

    /// <summary>
    /// Gets the navigator of the region inside the content of this region, or <see langword="null"/> if the content
    /// has no region or it is not attached yet.
    /// </summary>
    public Navigator? ChildNavigator
    {
        get { child.TryGetTarget(out var value); return value; }
        internal set
        {
            child.TryGetTarget(out var oldNavigator);
            if (value is not null) value.Route = this.Route;
            child.SetTarget(value);

            ChildNavigatorChanged?.Invoke(this, new NavigatorChangedEventArgs(oldNavigator, value));
        }
    }

    /// <summary>
    /// Raised when the navigator of the region inside the content of this region changes.
    /// </summary>
    public event EventHandler<NavigatorChangedEventArgs>? ChildNavigatorChanged;

    /// <summary>
    /// Gets the navigator of the region inside the content of this region.
    /// </summary>
    /// <returns>The child navigator.</returns>
    /// <exception cref="InvalidOperationException">The content has no region.</exception>
    public Navigator RequireChildNavigator()
     => ChildNavigator
        ?? throw new InvalidOperationException($"The region '{Region.Segment.Name}' has no child region attached.");

    /// <summary>
    /// Gets the navigator of the region inside the content of this region, if there is one.
    /// </summary>
    /// <param name="childNavigator">The child navigator.</param>
    /// <returns><see langword="true"/> if the region has a child region.</returns>
    public bool TryGetChildNavigator([NotNullWhen(true)] out Navigator? childNavigator)
    {
        childNavigator = ChildNavigator;
        return childNavigator is not null;
    }

    /// <summary>
    /// Gets the navigator of the innermost region, following the child navigators down from this one.
    /// </summary>
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
    /// Gets the current route of the navigator: the route from the root to the segment that the navigator shows.
    /// </summary>
    public Routing.Route Route { get; private set; } = Routing.Route.Empty;

    /// <summary>
    /// Gets the route that the navigator was asked to navigate to last.
    /// </summary>
    public Routing.Route RequestedRoute { get; private set; } = Routing.Route.Empty;

    /// <summary>
    /// Gets the route of the innermost navigator, which is the route that is shown.
    /// </summary>
    public Routing.Route ActualRoute => LeafNavigator.Route;

    /// <summary>
    /// Gets the part of the shown route that starts with the segment of this navigator.
    /// </summary>
    public Routing.Route TailRoute => LeafNavigator.Route.TrimHead(Route);

    /// <summary>
    /// Asks the registered authorization services whether the route may be navigated.
    /// </summary>
    /// <param name="route">The route to check.</param>
    /// <returns>
    /// Allowed if at least one service allows the route and none that denies it is alone; denied otherwise.
    /// </returns>
    public async Task<NavigationRuleResult> IsAllowedToNavigateAsync(Routing.Route route)
    {
        var checkers = ServiceProvider.GetServices<IAuthorizationService>().ToList();
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
                if (Debugger.IsAttached) Debugger.Break();
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
    public async Task<bool> CanNavigate(Routing.Route route)
     => (await IsAllowedToNavigateAsync(route)).IsAllowed;

    /// <summary>
    /// Navigates to a specific request
    /// </summary>
    /// <param name="request">The request to navigate to</param>
    /// <returns>The navigation response (awaitable)</returns>
    public async ValueTask<NavigationResult> NavigateAsync(NavigationRequest request)
    {
        request = request ?? throw new ArgumentNullException(nameof(request));

        // Merge navigation data first, so rules see non-null Data
        var mergedRoute = request.Route with
        {
            Data = (this.Parent?.Route.Data ?? NavigationData.Empty).Union(request.Route.Data)
        };
        request = request with { Route = mergedRoute };

        var navigationRuleResult = await IsAllowedToNavigateAsync(mergedRoute);
        if (!navigationRuleResult.IsAllowed)
        {
            return NavigationResult.Denied(navigationRuleResult.Reasons);
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

        NavigationResult result;
        try
        {
            result = await Dispatcher.EnqueueAsync(() => NavigateCoreAsync(request));
        }
        catch
        {
            // The navigator keeps the state that it had, so the next navigation does not start from a half done one
            ChildNavigator = oldChildNavigator;
            RequestedRoute = oldRequestedRoute;
            throw;
        }

        if (result.IsSkipped)
        {
            // Restore child navigator
            ChildNavigator = oldChildNavigator;
            RequestedRoute = oldRequestedRoute;
            oldChildNavigator?.Route.ApplyScopedInstanceServices(ServiceProvider);
        }
        if (result.Success)
        {
            // If it is data requests save new request with data load task
            request = result.Request;
            if (Target is { } target)
            {
                var segmentName = request.NameSegment.Name;
                await Dispatcher.EnqueueAsync(() => target.SetNestedSegmentName(segmentName));
            }
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
        var navigator = dialogScope.Region.Navigator;
        request.Handle = true;
        return navigator.NavigateAsync(request);
    }

    /// <summary>
    /// Shows the content of the request in the region of the navigator.
    /// </summary>
    /// <param name="request">The request to navigate.</param>
    /// <returns>The result of the navigation.</returns>
    protected abstract ValueTask<NavigationResult> NavigateCoreAsync(NavigationRequest request);

    /// <summary>
    /// Waits until the user interface has had the time to create and attach the regions of the content that was
    /// just shown.
    /// </summary>
    /// <returns>A task that completes when the visual tree is ready.</returns>
    public async Task WaitForVisualTreeAsync()
    {
        if (Debugger.IsAttached)
            await Task.Yield();

        if (Target is null)
        {
            // No target, no visual tree
            return;
        }

        var tcs = new TaskCompletionSource();
        if (Dispatcher.TryEnqueue(NavigationDispatcherPriority.Low, () => tcs.TrySetResult()))
        {
            await tcs.Task;
        }
    }

    /// <summary>
    /// Waits until the content that was just shown is loaded. The default does not wait.
    /// </summary>
    /// <returns>A task that completes when the content is loaded.</returns>
    protected virtual ValueTask WaitForVisualTree() => ValueTask.CompletedTask;
}
