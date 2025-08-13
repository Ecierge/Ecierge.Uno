namespace Ecierge.Uno.Navigation;

using System;

using Ecierge.Uno;
using Ecierge.Uno.Navigation;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.UI.Dispatching;

public sealed class NavigationScope : IServiceScope, IDisposable
{
    private static readonly Type WindowType = typeof(Window);
    private static readonly Type FrameworkElementType = typeof(FrameworkElement);
    private static readonly Type ContentDialogType = typeof(ContentDialog);
    private static readonly Type DispatcherType = typeof(DispatcherQueue);
    private static readonly Type NavigationScopeType = typeof(NavigationScope);
    private static readonly Type IServiceScopeType = typeof(IServiceScope);
    private static readonly Type NameSegmentType = typeof(NameSegment);
    private static readonly Type NavigatorType = typeof(Navigator);

    private readonly IServiceScope serviceScope;

    internal NameSegment Segment { get; }

    public IServiceProvider ServiceProvider => serviceScope.ServiceProvider;

    public NavigationScope(IServiceScope serviceScope, Window window, NameSegment segment, FrameworkElement element)
    {
        this.serviceScope = serviceScope ?? throw new ArgumentNullException(nameof(serviceScope));
        window = window ?? throw new ArgumentNullException(nameof(window));
        this.Segment = segment ?? throw new ArgumentNullException(nameof(segment));
        element = element ?? throw new ArgumentNullException(nameof(element));

        var serviceProvider = this.ServiceProvider;
        serviceProvider.SetScopedInstance(WindowType, window);
        serviceProvider.SetScopedInstance(DispatcherType, window.DispatcherQueue);
        serviceProvider.SetScopedInstance(NameSegmentType, segment);
        serviceProvider.SetScopedInstance(NavigationScopeType, this);
        serviceProvider.SetScopedInstance(IServiceScopeType, this);
        serviceProvider.SetScopedInstance(FrameworkElementType, element);
        serviceProvider.SetScopedInstance(NavigatorType, GetNavigator(element, null));
    }

    private NavigationScope(Navigator parentNavigator, NameSegment segment)
    {
        parentNavigator = parentNavigator ?? throw new ArgumentNullException(nameof(parentNavigator));
        Segment = segment ?? throw new ArgumentNullException(nameof(segment));

        serviceScope = parentNavigator.ServiceProvider.CreateScope();
        var serviceProvider = this.ServiceProvider;
        serviceProvider.SetScopedInstance(NavigationScopeType, this);
        serviceProvider.SetScopedInstance(IServiceScopeType, this);
        serviceProvider.SetScopedInstance(NameSegmentType, segment);
        var scopedInstanceOptions = serviceProvider.GetService<IOptions<ScopedInstanceRepositoryOptions>>()?.Value;
        if (scopedInstanceOptions is not null)
        {
            foreach (var type in scopedInstanceOptions.TypesToClone)
            {
                serviceProvider.TryCloneScopedInstance(type, parentNavigator.ServiceProvider);
            }
        }
    }

    public NavigationScope(Navigator parentNavigator, NameSegment segment, FrameworkElement element)
        : this(parentNavigator, segment)
    {
        element = element ?? throw new ArgumentNullException(nameof(element));
        var serviceProvider = this.ServiceProvider;
        serviceProvider.SetScopedInstance(FrameworkElementType, element);
        serviceProvider.SetScopedInstance(NavigatorType, GetNavigator(element, parentNavigator));
    }

    public void Dispose() => serviceScope.Dispose();

    public NavigationScope CreateScope(Navigator parentNavigator, NameSegment segment, FrameworkElement element)
     => new NavigationScope(parentNavigator, segment, element);

    public NavigationScope CreateDialogScope(DialogSegment segment, Navigator parentNavigator)
    {
        var lastNavigator = parentNavigator;
        while (lastNavigator.ChildNavigator is not null)
        {
            lastNavigator = lastNavigator.ChildNavigator;
        }

        var serviceProvider = lastNavigator.Region.Scope.ServiceProvider;
        var navigationScope = new NavigationScope(lastNavigator, segment);
        serviceProvider = navigationScope.ServiceProvider;

        var options = serviceProvider.GetRequiredService<IOptions<NavigationOptions>>().Value;
        Type viewType = segment.ViewMap!.View;
        Type navigatorType;
        if (ContentDialogType.IsAssignableFrom(viewType))
        {
            if (!options.TryGetNavigatorType(viewType, out navigatorType!))
                throw new InvalidOperationException($"No navigator found for {viewType.Name}");
        }
        else
        {
            if (!options.TryGetNavigatorType(ContentDialogType, out navigatorType!))
                throw new InvalidOperationException($"No navigator found for {viewType.Name}");
        }
        Navigator navigator = (Navigator)serviceProvider.GetRequiredService(navigatorType);
        AssignNavigators(lastNavigator, navigator);
        serviceProvider.SetScopedInstance(NavigatorType, navigator);
        lastNavigator.ChildNavigator = navigator;
        return navigationScope;
    }

    private Navigator GetNavigator(FrameworkElement element, Navigator? parent)
    {
        Type? navigatorType = element.GetNavigatorType();
        if (navigatorType is null)
        {
            Type elementType = element.GetType();
            var options = this.ServiceProvider.GetRequiredService<IOptions<NavigationOptions>>().Value;
            if (!options.TryGetNavigatorType(elementType, out navigatorType))
                throw new InvalidOperationException($"No navigator found for {elementType.Name}");
        }
        var navigator = (Navigator)this.ServiceProvider.GetRequiredService(navigatorType);
        AssignNavigators(parent, navigator);
        return navigator;
    }

    private void AssignNavigators(Navigator? parent, Navigator navigator)
    {
        navigator.Parent = parent;
        if (parent is not null)
        {
            parent.ChildNavigator = navigator;
            navigator.RootNavigator = parent.RootNavigator;
        }
    }

    public NavigationResult CreateViewModel(NavigationRequest request, INavigationData? navigationData)
    {
        var navData = navigationData ?? NavigationData.Empty;

        var viewModelType = request.View!.ViewModel!;

        {
            if (navData.GetData(viewModelType) is { } viewModel)
                return new NavigationResult(request, viewModel);
        }

        INavigationData AddOrUpdateValue(INavigationData data, string key, object value, Type? dataMapType = null)
        {
            var exists = data.TryGetValue(key, out var oldValue);
            if (!exists)
                return data.Add(key, value);
            if (oldValue == value)
                return data;
            if (value is Task && dataMapType is not null)
            {
                var dataMap = (INavigationDataMap)ServiceProvider.GetRequiredService(dataMapType);
                if (oldValue!.GetType() == dataMap.EntityType)
                    return data;
            }
            return data.SetItem(key, value);
        }

        switch (request)
        {
            case PrimitiveDataSegmentNavigationRequest dataRequest:
                if (dataRequest.Segment.DataMap is { } dataMapType)
                {
                    var dataMap = (INavigationDataMap)ServiceProvider.GetRequiredService(dataMapType);
                    var dataTask = dataMap.LoadEntityAsync(dataRequest.RouteDataPrimitive);
                    navData = AddOrUpdateValue(navData, dataRequest.Segment.Name, dataTask, dataMapType);
                    request = dataRequest.WithDataEntity(dataTask);
                }
                else
                {
                    navData = AddOrUpdateValue(navData, dataRequest.Segment.Name, dataRequest.RouteDataPrimitive);
                }
                break;
            case TaskDataSegmentNavigationRequest dataRequest:
                navData = AddOrUpdateValue(navData, dataRequest.Segment.Name, dataRequest.RouteDataTask, dataRequest.Segment.DataMap);
                break;
        }
        ServiceProvider.SetScopedInstance<INavigationData>(navData);

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
