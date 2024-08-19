namespace Ecierge.Uno.Navigation;

using System;
using System.Collections.Immutable;
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

    protected IServiceProvider ServiceProvider { get; }
    public FrameworkElement Target => ServiceProvider.GetRequiredService<FrameworkElement>();
    protected NavigationScope Scope => ServiceProvider.GetService<NavigationScope>()!;
    private Lazy<ILogger> logger;
    protected ILogger Logger => logger.Value;

    public Navigator? Parent { get; internal set; }

    WeakReference<Navigator?> child = new WeakReference<Navigator?>(null);
    public Navigator? Child
    {
        get { child.TryGetTarget(out var value); return value; }
        internal set => child.SetTarget(value);
    }

    public Navigator(IServiceProvider serviceProvider)
    {
        ServiceProvider = serviceProvider;
        logger = new(() => serviceProvider.GetRequiredService<ILoggerFactory>().CreateLogger(this.GetType()));
    }

    /// <summary>
    /// Gets the current route of the navigator
    /// </summary>
    public Routing.Route? Route => throw new NotImplementedException();

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
    public async ValueTask<NavigationResponse> NavigateAsync(NavigationRequest request)
    {
        var result = await NavigateCoreAsync(request);
        FrameworkElement target = Region!.Target!;
        target.SetNestedSegmentName(request.NameSegment.Name);
        if (result.Success)
        {
            TaskCompletionSource tcs = new();
            void Loaded(object? s, object? e)
            {
                var dispatcher = Region.Scope.ServiceProvider.GetRequiredService<DispatcherQueue>();
                dispatcher.TryEnqueue(DispatcherQueuePriority.Low, () => tcs.SetResult());
                //tcs.SetResult();
                target.Loaded -= Loaded;
            }
            target.Loaded += Loaded;
            await tcs.Task;
        }
        return result;
    }

    public abstract ValueTask<NavigationResponse> NavigateCoreAsync(NavigationRequest request);
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
        if (result.Success) await navigator.NavigateNestedDefaultAsync(initiator, segment);
        return result;
    }

    public static async ValueTask<NavigationResponse> NavigateNestedSegmentAsync<TRouteData>([NotNull] this Navigator navigator, object initiator, DataSegment segment, TRouteData? routeData)
    {
        var result = await navigator.NavigateAsync(new DataSegmentNavigationRequest<TRouteData>(initiator, segment, routeData));
        if (result.Success) await navigator.NavigateNestedDefaultAsync(initiator, segment);
        return result;
    }


    public static ValueTask<NavigationResponse> NavigateDefaultAsync([NotNull] this Navigator navigator, object initiator, RouteSegment segment)
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
            await navigator.Child!.NavigateSegmentAsync(initiator, defaultSegment);
        }
    }

    public static ValueTask<NavigationResponse> NavigateNestedSegmentAsync([NotNull] this Navigator navigator, object initiator, string segmentName, object? data = null)
    {
        var childNavigator = navigator.Child;
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
