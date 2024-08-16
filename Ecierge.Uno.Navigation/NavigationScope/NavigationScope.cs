namespace Ecierge.Uno.Navigation;

using System;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.UI.Dispatching;

public sealed class NavigationScope : IServiceScope
{
    private static readonly Type WindowType = typeof(Window);
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
        serviceProvider.AddScopedInstance(DispatcherType, window.DispatcherQueue);
        serviceProvider.AddScopedInstance(NavigationScopeType, this);
        serviceProvider.AddScopedInstance(NameSegmentType, segment);
        serviceProvider.AddScopedInstance(NavigatorType, GetNavigator(element.GetType(), parentNavigator));
    }

    public void Dispose() => serviceScope.Dispose();

    public NavigationScope CreateScope(NameSegment segment, FrameworkElement element, Navigator parentNavigator)
     => new NavigationScope(
         this.ServiceProvider.CreateScope(),
         this.ServiceProvider.GetRequiredService<Window>(),
         segment,
         element,
         parentNavigator);

    private Navigator GetNavigator(Type controlType, Navigator? parent)
    {
        controlType = controlType ?? throw new ArgumentNullException(nameof(controlType));
        var options = this.ServiceProvider.GetRequiredService<IOptions<NavigationOptions>>().Value;
        if (options.TryGetNavigatorType(controlType, out Type? navigatorType))
        {
            var navigator = (Navigator)this.ServiceProvider.GetRequiredService(navigatorType);
            navigator.Parent = parent;
            return navigator;
        }
        throw new InvalidOperationException($"No navigator found for {controlType.Name}");
    }
}
