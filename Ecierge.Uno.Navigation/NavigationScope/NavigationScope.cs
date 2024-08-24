namespace Ecierge.Uno.Navigation;

using System;

using Ecierge.Uno.Navigation;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.UI.Dispatching;

public sealed class NavigationScope : IServiceScope, IDisposable
{
    private static readonly Type WindowType = typeof(Window);
    private static readonly Type FrameworkElementType = typeof(FrameworkElement);
    private static readonly Type DispatcherType = typeof(DispatcherQueue);
    private static readonly Type NavigationScopeType = typeof(NavigationScope);
    private static readonly Type NameSegmentType = typeof(NameSegment);
    private static readonly Type NavigatorType = typeof(Navigator);

    private readonly IServiceScope serviceScope;

    internal NameSegment Segment { get; }

    public IServiceProvider ServiceProvider => serviceScope.ServiceProvider;

    public NavigationScope(IServiceScope serviceScope, Window window, NameSegment segment, FrameworkElement element, Navigator? parentNavigator)
    {
        this.serviceScope = serviceScope ?? throw new ArgumentNullException(nameof(serviceScope));
        window = window ?? throw new ArgumentNullException(nameof(window));
        this.Segment = segment ?? throw new ArgumentNullException(nameof(segment));
        element = element ?? throw new ArgumentNullException(nameof(element));

        var serviceProvider = this.ServiceProvider;
        serviceProvider.AddScopedInstance(WindowType, window);
        serviceProvider.AddScopedInstance(FrameworkElementType, element);
        serviceProvider.AddScopedInstance(DispatcherType, window.DispatcherQueue);
        serviceProvider.AddScopedInstance(NavigationScopeType, this);
        serviceProvider.AddScopedInstance(NameSegmentType, segment);
        serviceProvider.AddScopedInstance(NavigatorType, GetNavigator(element, parentNavigator));
    }

    ~NavigationScope() => Dispose(false);

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    private void Dispose(bool disposing)
    {
        if (disposing)
        {
            var navigator = ServiceProvider.GetService<Navigator>();
            Navigator? parentNavigator = navigator!.Parent;
            if (parentNavigator is not null && parentNavigator.ChildNavigator == navigator)
            {
                parentNavigator.ChildNavigator = null;
            }

            serviceScope.Dispose();
        }
    }

    public NavigationScope CreateScope(NameSegment segment, FrameworkElement element, Navigator parentNavigator)
     => new NavigationScope(
         this.ServiceProvider.CreateScope(),
         this.ServiceProvider.GetRequiredService<Window>(),
         segment,
         element,
         parentNavigator);

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
        navigator.Parent = parent;
        if (parent is not null)
        {
            parent.ChildNavigator = navigator;
            navigator.RootNavigator = parent.RootNavigator;
        }
        return navigator;
    }

    public NavigationResult CreateViewModel(NavigationRequest request, INavigationData? navigationData)
    {
        var data = navigationData ?? NavigationData.Empty;

        var nameSegment = request.NameSegment;
        var viewModelType = nameSegment.View!.ViewModel!;

        var viewModel = data.GetData(viewModelType);
        if (viewModel is not null) return new NavigationResult(nameSegment, viewModel);

        if (request is not DataSegmentNavigationRequest)
        {
            try
            {
                viewModel = ServiceProvider.GetService(viewModelType);
            }
            catch (InvalidOperationException) { }
            if (viewModel is not null) return new NavigationResult(nameSegment, viewModel);
        }
        if (request is DataSegmentNavigationRequest dataRequest && dataRequest.RouteData is not null)
        {
            navigationData = (navigationData ?? NavigationData.Empty).Add(dataRequest.Segment.Name, dataRequest.RouteData);
        }

        var ctor = viewModelType.GetNavigationConstructor(ServiceProvider, navigationData ?? NavigationData.Empty, out var args);
        if (ctor is not null)
        {
            try
            {
                return new NavigationResult(nameSegment, ctor.Invoke(args));
            }
            catch
            {
                ServiceProvider.GetRequiredService<ILogger<NavigationScope>>().LogInformation("Failed to create view model of type {ViewModelType} using route data and service provider", viewModelType);
                return new NavigationResult($"Failed to create view model of type {viewModelType} using route data and service provider");
            }
        }
        return new NavigationResult($"Constructor for {viewModelType} not found");
    }
}
