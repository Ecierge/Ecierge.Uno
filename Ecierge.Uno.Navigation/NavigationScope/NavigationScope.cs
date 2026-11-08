namespace Ecierge.Uno.Navigation;

using System;
using System.Threading;
using System.Threading.Tasks;

using Ecierge.Uno;
using Ecierge.Uno.Navigation;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.UI.Dispatching;

/// <summary>
/// The services of one navigation region. A scope creates the region and the navigator of the region, in this order,
/// and gives the navigator everything that it needs when it is constructed.
/// </summary>
public sealed class NavigationScope : IServiceScope, IAsyncDisposable
{
    private static readonly Type WindowType = typeof(Window);
    private static readonly Type FrameworkElementType = typeof(FrameworkElement);
    private static readonly Type ContentDialogType = typeof(ContentDialog);
    private static readonly Type DispatcherType = typeof(DispatcherQueue);
    private static readonly Type NavigationDispatcherType = typeof(INavigationDispatcher);
    private static readonly Type NavigationScopeType = typeof(NavigationScope);
    private static readonly Type IServiceScopeType = typeof(IServiceScope);
    private static readonly Type NameSegmentType = typeof(NameSegment);
    private static readonly Type NavigatorType = typeof(Navigator);
    private static readonly Type RegionType = typeof(Regions.NavigationRegion);
    private static readonly Type JournalType = typeof(NavigationJournal);

    private readonly IServiceScope serviceScope;
    private int _disposed;

    internal NameSegment Segment { get; }

    /// <summary>
    /// Gets the region that the scope was created for.
    /// </summary>
    public Regions.NavigationRegion Region { get; }

    /// <summary>
    /// Gets the services of the scope.
    /// </summary>
    public IServiceProvider ServiceProvider => serviceScope.ServiceProvider;

    /// <summary>
    /// Creates the scope of the root region of a window.
    /// </summary>
    /// <param name="serviceScope">The scope of the services of the application.</param>
    /// <param name="window">The window that the navigation is in.</param>
    /// <param name="segment">The root segment of the routes.</param>
    /// <param name="element">The control of the root region.</param>
    public NavigationScope(IServiceScope serviceScope, Window window, NameSegment segment, FrameworkElement element)
        : this(
            serviceScope,
            segment,
            parentNavigator: null,
            element ?? throw new ArgumentNullException(nameof(element)),
            boundary: null,
            navigatorType: null,
            window ?? throw new ArgumentNullException(nameof(window)),
            dispatcher: null)
    {
    }

    private NavigationScope(
        IServiceScope serviceScope,
        NameSegment segment,
        Navigator? parentNavigator,
        FrameworkElement? element,
        FrameworkElement? boundary,
        Type? navigatorType,
        Window? window,
        INavigationDispatcher? dispatcher)
    {
        this.serviceScope = serviceScope ?? throw new ArgumentNullException(nameof(serviceScope));
        Segment = segment ?? throw new ArgumentNullException(nameof(segment));

        var serviceProvider = this.ServiceProvider;
        serviceProvider.AddScopedInstance(NavigationScopeType, this);
        serviceProvider.AddScopedInstance(IServiceScopeType, this);
        serviceProvider.AddScopedInstance(NameSegmentType, segment);

        if (parentNavigator is not null)
        {
            var scopedInstanceOptions = serviceProvider.GetService<IOptions<ScopedInstanceRepositoryOptions>>()?.Value;
            if (scopedInstanceOptions is not null)
            {
                foreach (var type in scopedInstanceOptions.TypesToClone)
                {
                    serviceProvider.TryCloneScopedInstance(type, parentNavigator.ServiceProvider);
                }
            }
        }

        if (window is not null)
        {
            serviceProvider.AddScopedInstance(WindowType, window);
            serviceProvider.AddScopedInstance(DispatcherType, window.DispatcherQueue);
            dispatcher ??= new DispatcherQueueNavigationDispatcher(window.DispatcherQueue);
        }

        if (dispatcher is not null)
        {
            serviceProvider.AddScopedInstance(NavigationDispatcherType, dispatcher);
        }

        if (element is not null)
        {
            serviceProvider.AddScopedInstance(FrameworkElementType, element);
        }

        // The region comes first: the navigator takes it from the scope when it is constructed
        Region = new Regions.NavigationRegion(this, parentNavigator?.Region, element, boundary);
        serviceProvider.AddScopedInstance(RegionType, Region);

        if (parentNavigator is null)
        {
            // One history for the window, whichever navigator showed the page
            var capacity = serviceProvider.GetService<IOptions<NavigationOptions>>()?.Value.JournalCapacity ?? new NavigationOptions().JournalCapacity;
            serviceProvider.AddScopedInstance(JournalType, new NavigationJournal(capacity));
        }

        var navigator = CreateNavigator(element, navigatorType);
        Region.AttachNavigator(navigator);
        serviceProvider.AddScopedInstance(NavigatorType, navigator);
        if (parentNavigator is not null)
        {
            parentNavigator.ChildNavigator = navigator;
        }
    }

    private Navigator CreateNavigator(FrameworkElement? element, Type? navigatorType)
    {
        if (navigatorType is null)
        {
            navigatorType = element?.GetNavigatorType();
            if (navigatorType is null)
            {
                if (element is null)
                    throw new InvalidOperationException($"The scope of the segment '{Segment.Name}' has neither an element nor a navigator type.");

                Type elementType = element.GetType();
                var options = this.ServiceProvider.GetRequiredService<IOptions<NavigationOptions>>().Value;
                if (!options.TryGetNavigatorType(elementType, out navigatorType))
                    throw new InvalidOperationException($"No navigator found for {elementType.Name}");
            }
        }

        return (Navigator)this.ServiceProvider.GetRequiredService(navigatorType);
    }

    /// <summary>
    /// Creates the scope of the root region of a navigation tree that has no window and no control. A test uses it to
    /// run the navigators without a user interface.
    /// </summary>
    /// <param name="serviceScope">The scope of the services of the application.</param>
    /// <param name="dispatcher">The dispatcher that the navigators run their work on.</param>
    /// <param name="segment">The root segment of the routes.</param>
    /// <param name="navigatorType">The type of the navigator of the root region.</param>
    /// <returns>The scope of the root region.</returns>
    internal static NavigationScope CreateRoot(
        IServiceScope serviceScope,
        INavigationDispatcher dispatcher,
        NameSegment segment,
        Type navigatorType)
     => new(
            serviceScope,
            segment,
            parentNavigator: null,
            element: null,
            boundary: null,
            navigatorType ?? throw new ArgumentNullException(nameof(navigatorType)),
            window: null,
            dispatcher ?? throw new ArgumentNullException(nameof(dispatcher)));

    /// <inheritdoc/>
    public void Dispose() => _ = DisposeAsync();

    /// <inheritdoc/>
    public async ValueTask DisposeAsync()
    {
        if (Interlocked.Exchange(ref _disposed, 1) == 1) return;
        await Task.Delay(TimeSpan.FromSeconds(30));
        serviceScope.Dispose();
    }

    /// <summary>
    /// Creates the scope of a region inside a control of the parent region.
    /// </summary>
    /// <param name="parentNavigator">The navigator of the parent region.</param>
    /// <param name="segment">The segment that the new region shows the content of.</param>
    /// <param name="element">The control of the new region.</param>
    /// <returns>The scope of the new region.</returns>
    public NavigationScope CreateScope(Navigator parentNavigator, NameSegment segment, FrameworkElement element)
     => CreateScope(parentNavigator, segment, element, boundary: null);

    /// <summary>
    /// Creates the scope of a region inside a control of the parent region.
    /// </summary>
    /// <param name="parentNavigator">The navigator of the parent region.</param>
    /// <param name="segment">The segment that the new region shows the content of.</param>
    /// <param name="element">The control of the new region.</param>
    /// <param name="boundary">The control that bounds the search of the regions inside the new region.</param>
    /// <returns>The scope of the new region.</returns>
    public NavigationScope CreateScope(Navigator parentNavigator, NameSegment segment, FrameworkElement element, FrameworkElement? boundary)
    {
        parentNavigator = parentNavigator ?? throw new ArgumentNullException(nameof(parentNavigator));
        element = element ?? throw new ArgumentNullException(nameof(element));
        return new NavigationScope(
            parentNavigator.ServiceProvider.CreateScope(),
            segment,
            parentNavigator,
            element,
            boundary,
            navigatorType: null,
            window: null,
            dispatcher: null);
    }

    /// <summary>
    /// Creates the scope of a region that has no control, with a navigator of the given type. A test uses it to
    /// build the regions that a control would attach.
    /// </summary>
    /// <param name="parentNavigator">The navigator of the parent region.</param>
    /// <param name="segment">The segment that the new region shows the content of.</param>
    /// <param name="navigatorType">The type of the navigator of the new region.</param>
    /// <returns>The scope of the new region.</returns>
    internal NavigationScope CreateScope(Navigator parentNavigator, NameSegment segment, Type navigatorType)
    {
        parentNavigator = parentNavigator ?? throw new ArgumentNullException(nameof(parentNavigator));
        return new NavigationScope(
            parentNavigator.ServiceProvider.CreateScope(),
            segment,
            parentNavigator,
            element: null,
            boundary: null,
            navigatorType ?? throw new ArgumentNullException(nameof(navigatorType)),
            window: null,
            dispatcher: null);
    }

    /// <summary>
    /// Creates the scope of a dialog. The dialog is shown over the last region of the navigators below
    /// <paramref name="parentNavigator"/>.
    /// </summary>
    /// <param name="segment">The segment of the dialog.</param>
    /// <param name="parentNavigator">The navigator that the dialog is requested through.</param>
    /// <returns>The scope of the dialog, which has no control until the dialog is shown.</returns>
    public NavigationScope CreateDialogScope(DialogSegment segment, Navigator parentNavigator)
    {
        segment = segment ?? throw new ArgumentNullException(nameof(segment));
        var lastNavigator = (parentNavigator ?? throw new ArgumentNullException(nameof(parentNavigator))).LeafNavigator;

        var viewType = segment.ViewMap?.View
            ?? throw new InvalidOperationException($"The dialog segment '{segment.Name}' has no view.");
        var options = lastNavigator.ServiceProvider.GetRequiredService<IOptions<NavigationOptions>>().Value;
        Type? navigatorType;
        if (ContentDialogType.IsAssignableFrom(viewType))
        {
            if (!options.TryGetNavigatorType(viewType, out navigatorType))
                throw new InvalidOperationException($"No navigator found for {viewType.Name}");
        }
        else
        {
            if (!options.TryGetNavigatorType(ContentDialogType, out navigatorType))
                throw new InvalidOperationException($"No navigator found for {viewType.Name}");
        }

        return new NavigationScope(
            lastNavigator.ServiceProvider.CreateScope(),
            segment,
            lastNavigator,
            element: null,
            boundary: lastNavigator.Region.Root,
            navigatorType,
            window: null,
            dispatcher: null);
    }

    /// <summary>
    /// Creates the view model of the view that the request navigates to.
    /// </summary>
    /// <param name="request">The request that is navigated.</param>
    /// <param name="navigationData">The data that the route of the request carries.</param>
    /// <returns>The result with the view model, or the errors if it could not be created.</returns>
    public NavigationResult CreateViewModel(NavigationRequest request, INavigationData? navigationData)
    {
        var navData = navigationData ?? NavigationData.Empty;

        var viewModelType = request.View?.ViewModel
            ?? throw new InvalidOperationException($"The segment '{request.NameSegment.Name}' has no view model.");

        {
            if (navData.GetData(viewModelType) is { } viewModel)
                return new NavigationResult(request, viewModel);
        }

        INavigationData AddOrUpdateValue(INavigationData data, string key, object value)
        {
            var exists = data.TryGetValue(key, out var oldValue);
            if (!exists)
                return data.Add(key, value);
            if (oldValue == value)
                return data;
            return data.SetItem(key, value);
        }

        switch (request)
        {
            case PrimitiveDataSegmentNavigationRequest dataRequest:
                if (dataRequest.Segment.DataMap is { } dataMapType)
                {
                    // The handle that the pages above already share is reused for the same value; the load starts
                    // here, when the first page that needs the entity is created
                    var dataMap = (INavigationDataMap)ServiceProvider.GetRequiredService(dataMapType);
                    var entity = dataMap.GetOrCreateEntity(navData, dataRequest.Segment.Name, dataRequest.RouteDataPrimitive);
                    navData = AddOrUpdateValue(navData, dataRequest.Segment.Name, entity);
                    request = dataRequest.WithDataEntity(entity.LoadTask, navData);
                }
                else
                {
                    navData = AddOrUpdateValue(navData, dataRequest.Segment.Name, dataRequest.RouteDataPrimitive);
                }
                break;
            case TaskDataSegmentNavigationRequest dataRequest:
                if (navData.TryGetValue(dataRequest.Segment.Name, out var held) && held is INavigationEntity)
                {
                    // The route already holds the handle of the entity
                    break;
                }
                if (dataRequest.Segment.DataMap is { } taskMapType)
                {
                    var dataMap = (INavigationDataMap)ServiceProvider.GetRequiredService(taskMapType);
                    try
                    {
                        var entity = dataMap.CreateEntity(dataRequest.RouteDataTask, dataRequest.RouteDataPrimitive, navData);
                        navData = AddOrUpdateValue(navData, dataRequest.Segment.Name, entity);
                        break;
                    }
                    catch (Exception ex) when (ex is NotSupportedException or ArgumentException)
                    {
                        // The task is not a task of the entity of the map, so it stays as it is
                    }
                }
                navData = AddOrUpdateValue(navData, dataRequest.Segment.Name, dataRequest.RouteDataTask);
                break;
        }
        ServiceProvider.AddScopedInstance<INavigationData>(navData);

        try
        {
            var viewModel = ServiceProvider.GetRequiredService(viewModelType);
            return new NavigationResult(request, viewModel);
        }
        catch (InvalidOperationException)
        {
            ServiceProvider.GetRequiredService<ILogger<NavigationScope>>().LogInformation("Failed to create view model of type {ViewModelType} using route data and service provider", viewModelType);
            return new NavigationResult($"Failed to create view model of type {viewModelType} using route data and service provider");
        }
    }
}
