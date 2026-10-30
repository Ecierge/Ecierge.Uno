namespace Ecierge.Uno.Navigation.Tests.Doubles;

using Ecierge.Uno.Navigation;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml.Controls;

/// <summary>
/// A navigation tree without a user interface: the services of the navigation, an immediate dispatcher and a fake
/// root navigator.
/// </summary>
internal sealed class NavigationTestHost : IDisposable
{
    private readonly ServiceProvider services;

    private NavigationTestHost(ServiceProvider services, NavigationScope rootScope)
    {
        this.services = services;
        RootScope = rootScope;
        Root = (FakeNavigator)rootScope.Region.Navigator;
    }

    public IServiceProvider Services => services;

    public NavigationScope RootScope { get; }

    public FakeNavigator Root { get; }

    public FakeRegionPlan Plan => services.GetRequiredService<FakeRegionPlan>();

    public INavigationStatus Status => services.GetRequiredService<INavigationStatus>();

    /// <summary>
    /// Creates a navigation tree.
    /// </summary>
    /// <param name="registerRoutes">Registers the views, data maps and routes.</param>
    /// <param name="configureServices">Adds or replaces services; runs after the navigation services are added.</param>
    /// <param name="configureOptions">Adjusts the navigation options.</param>
    public static NavigationTestHost Create(
        Action<IViewRegistryBuilder, INavigationDataRegistryBuilder, IRouteRegistryBuilder> registerRoutes,
        Action<IServiceCollection>? configureServices = null,
        Func<NavigationOptions, NavigationOptions>? configureOptions = null)
    {
        var collection = new ServiceCollection();
        collection.AddLogging();
        collection.AddSingleton<FakeRegionPlan>();
        collection.AddSingleton<ImmediateDispatcher>();
        collection.AddSingleton<IAuthorizationService, AllowAllAuthorizationService>();
        collection.AddNavigation(configureOptions, registerRoutes);
        collection.AddScoped<FakeNavigator>();
        collection.AddScoped<FakeDialogNavigator>();
        // Registered after the navigation services, so the fake replaces the navigator of a content dialog
        collection.AddSingleton(new Tuple<Type, Type>(typeof(ContentDialog), typeof(FakeDialogNavigator)));
        configureServices?.Invoke(collection);

        var provider = collection.BuildServiceProvider();
        var rootSegment = provider.GetRequiredService<IRouteRegistry>().RootSegment;
        var rootScope = NavigationScope.CreateRoot(
            provider.CreateScope(),
            provider.GetRequiredService<ImmediateDispatcher>(),
            rootSegment,
            typeof(FakeNavigator));
        return new NavigationTestHost(provider, rootScope);
    }

    public void Dispose() => services.Dispose();
}
