namespace Ecierge.Uno.Navigation;

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.UI.Dispatching;

using global::Uno.Extensions;

using Ecierge.Uno.Navigation.Navigators;

public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds navigation services to the service collection.
    /// </summary>
    /// <param name="services">The service collection to add navigation services to.</param>
    /// <param name="configure">Optional configuration action for <see cref="NavigationOptions"/>.</param>
    /// <param name="routeBuilder">Optional action to configure the view and route registries.</param>
    /// <param name="createViewRegistryBuilder">Optional factory to create a custom <see cref="IViewRegistryBuilder"/>.</param>
    /// <param name="createRouteRegistryBuilder">Optional factory to create a custom <see cref="IRouteRegistryBuilder"/>.</param>
    /// <returns></returns>
    public static IServiceCollection AddNavigation(
          this IServiceCollection services
        , Func<NavigationOptions, NavigationOptions>? configure = null
        , Action<IViewRegistryBuilder, INavigationDataRegistryBuilder, IRouteRegistryBuilder>? routeBuilder = null
        , Func<IServiceCollection, IViewRegistryBuilder>? createViewRegistryBuilder = null
        , Func<IServiceCollection, IRouteRegistryBuilder>? createRouteRegistryBuilder = null
        )
    {
        var navConfig = new NavigationOptions();
        navConfig = (configure?.Invoke(navConfig)) ?? navConfig;

        IViewRegistryBuilder viewRegistryBuilder = createViewRegistryBuilder?.Invoke(services) ?? new ViewRegistryBuilder(services);
        INavigationDataRegistryBuilder navigationDataRegistryBuilder = new NavigationDataRegistryBuilder(services);
        IRouteRegistryBuilder routeRegistryBuilder = createRouteRegistryBuilder?.Invoke(services) ?? new RouteRegistryBuilder();
        routeBuilder?.Invoke(viewRegistryBuilder, navigationDataRegistryBuilder, routeRegistryBuilder);
        var views = viewRegistryBuilder.Build();
        var navigationData = navigationDataRegistryBuilder.Build();
        var routes = routeRegistryBuilder.Build(views, navigationData);

        return
            services
                .AddSingleton<IPostConfigureOptions<NavigationOptions>, PostConfigureNavigationOptions>()

                .AddSingleton<IViewRegistry>(views)
                .AddSingleton<INavigationDataRegistry>(navigationData)
                .AddSingleton<IRouteRegistry>(routes)

                .AddSingleton<ISingletonInstanceRepository, Ecierge.Uno.InstanceRepository>()
                .AddScoped<IScopedInstanceRepository, Ecierge.Uno.InstanceRepository>()

                .AddInheritedScopedInstance<Window>()
                .AddInheritedScopedInstance<DispatcherQueue>()
                .AddScopedInstance<FrameworkElement>()
                .AddScopedInstance<NavigationScope>()
                .AddScopedInstance<IServiceScope>()
                .AddScopedInstance<Navigator>()
                .AddScopedInstance<NameSegment>()
                .AddSingleton<INavigationStatus, NavigationStatus>()

                .AddScopedInstance<INavigationData>()

                //.AddSingleton<IResponseNavigatorFactory, ResponseNavigatorFactory>()

                .AddSingleton<RouteNotifier>()
                .AddSingleton<IRouteNotifier>(sp => sp.GetRequiredService<RouteNotifier>())
                .AddSingleton<IRouteUpdater>(sp => sp.GetRequiredService<RouteNotifier>())
                //.AddHostedService<BrowserAddressBarService>()
                //.AddHostedService<BackButtonService>()
                //.AddScoped<Navigator>()


                // Register the region for each control type
                //.AddNavigator<Frame, FrameNavigator>()
                .AddNavigator<ContentDialog, ContentDialogNavigator>()
                .AddNavigator<ContentControl, ContentControlNavigator>()
                .AddNavigator<NavigationView, NavigationViewNavigator>()
                .AddScoped<NavigationViewContentNavigator>()
                //.AddNavigator<Panel, PanelVisibilityNavigator>(name: PanelVisibilityNavigator.NavigatorName)
                //.AddNavigator<Microsoft.UI.Xaml.Controls.NavigationView, NavigationViewNavigator>()
                //.AddNavigator<ContentDialog, ContentDialogNavigator>(true)
                //.AddNavigator<MessageDialog, MessageDialogNavigator>(true)
                //.AddNavigator<Flyout, FlyoutNavigator>(true)
                //.AddNavigator<Popup, PopupNavigator>(true)
                .AddNavigator<SelectorBar, SelectorBarNavigator>()

                .AddScoped<ByNameNavigationViewItemSelector>()

                //.AddSingleton<IRequestHandler, TapRequestHandler>()
                //.AddSingleton<IRequestHandler, ButtonBaseRequestHandler>()
                //.AddSingleton<IRequestHandler, SelectorRequestHandler>()
                //.AddSingleton<IRequestHandler, NavigationViewItemRequestHandler>()
                //.AddSingleton<IRequestHandler, NavigationViewRequestHandler>()
                //.AddSingleton<IRequestHandler, ItemsRepeaterRequestHandler>()

                // Register the navigation mappings repository

                .AddSingleton(views.GetType(), views)
                .AddSingleton<IViewRegistry>(sp => (IViewRegistry)sp.GetRequiredService(views.GetType()))

                .AddSingleton(routes.GetType(), routes)
                .AddSingleton<IRouteRegistry>(sp => (RouteRegistry)sp.GetRequiredService(routes.GetType()));
    }

    /// <summary>
    /// Adds a scoped instance of the specified type to the service collection.
    /// If an instance is present in the current scope it will be copied to child scopes.
    /// </summary>
    /// <param name="services">The service collection to add the scoped instance to.</param>
    /// <param name="type">The type of the instance to add.</param>
    /// <returns>The updated service collection.</returns>
    public static IServiceCollection AddInheritedScopedInstance(this IServiceCollection services, Type type)
     =>
#pragma warning disable CS8603 // Possible null reference return.
        services
            .AddTransient(type, sp => sp.GetInstance(type))
            .Configure<ScopedInstanceRepositoryOptions>(options => options.AddTypeToClone(type));
#pragma warning restore CS8603 // Possible null reference return.

    /// <summary>
    /// Adds a scoped instance of the specified type to the service collection.
    /// If an instance is present in the current scope it will be copied to child scopes.
    /// </summary>
    /// <param name="services">The service collection to add the scoped instance to.</param>
    /// <typeparam name="T">The type of the instance to add.</typeparam>
    /// <returns>The updated service collection.</returns>
    public static IServiceCollection AddInheritedScopedInstance<T>(this IServiceCollection services)
        where T : class
     =>
#pragma warning disable CS8603 // Possible null reference return.
        services
            .AddTransient<T>(sp => sp.GetInstance<T>())
            .Configure<ScopedInstanceRepositoryOptions>(options => options.AddTypeToClone<T>());
#pragma warning restore CS8603 // Possible null reference return.


    /// <summary>
    /// Adds a scoped instance of the specified type to the service collection.
    /// </summary>
    /// <param name="services">The service collection to add the scoped instance to.</param>
    /// <param name="type">The type of the instance to add.</param>
    /// <returns>The updated service collection.</returns>
    public static IServiceCollection AddScopedInstance<T>(this IServiceCollection services)
        where T : class
     =>
#pragma warning disable CS8603 // Possible null reference return.
        services
            .AddTransient<T>(sp => sp.GetInstance<T>());
#pragma warning restore CS8603 // Possible null reference return.

    /// <summary>
    /// Adds a navigator for the specified control type and a mapping to navigator type to the service collection.
    /// </summary>
    /// <typeparam name="TControl">The type of the control that the navigator will manage.</typeparam>
    /// <typeparam name="TNavigator">The type of the navigator that will manage the control.</typeparam>
    /// <param name="services">The service collection to add the navigator to.</param>
    /// <returns>The updated service collection.</returns>
    public static IServiceCollection AddNavigator<TControl, TNavigator>([NotNull] this IServiceCollection services)
        where TControl : Control
        where TNavigator : Navigator
     =>
        services
            .AddScoped<TNavigator>()
            .AddSingleton(new Tuple<Type, Type>(typeof(TControl), typeof(TNavigator)));

    #region Add scoped with navigation parameters

    /// <summary>
    /// Adds a scoped service that can be created with navigation parameters.
    /// </summary>
    /// <typeparam name="TService">The type of the service to add.</typeparam>
    /// <param name="services">The service collection to add the service to.</param>
    /// <returns>The updated service collection.</returns>
    public static IServiceCollection AddScopedWithNavigationParameters<TService>(this IServiceCollection services)
        where TService : class
     =>
        services.AddScoped<TService>(TypeExtensions.CreateWithNavigationParameters<TService>);

    /// <summary>
    /// Adds a scoped service with a specific implementation that can be created with navigation parameters.
    /// </summary>
    /// <typeparam name="TService">The type of the service to add.</typeparam>
    /// <typeparam name="TImplementation">The type of the implementation to add.</typeparam>
    /// <param name="services">The service collection to add the service to.</param>
    /// <returns>The updated service collection.</returns>
    public static IServiceCollection AddScopedWithNavigationParameters<TService, TImplementation>(this IServiceCollection services)
        where TService : class
        where TImplementation : class, TService
     =>
        services.AddScoped<TService, TImplementation>(TypeExtensions.CreateWithNavigationParameters<TImplementation>);

    /// <summary>
    /// Adds a scoped service of the specified type that can be created with navigation parameters.
    /// </summary>
    /// <param name="services">The service collection to add the service to.</param>
    /// <param name="serviceType">The type of the service to add.</param>
    /// <returns>The updated service collection.</returns>
    public static IServiceCollection AddScopedWithNavigationParameters(this IServiceCollection services, Type serviceType)
     =>
        services.AddScoped(serviceType, TypeExtensions.GetFactoryWithNavigationParameters(serviceType));

    /// <summary>
    /// Adds a scoped service of the specified type with a specific implementation that can be created with navigation parameters.
    /// </summary>
    /// <param name="services">The service collection to add the service to.</param>
    /// <param name="serviceType">The type of the service to add.</param>
    /// <param name="implementationType">The type of the implementation to add.</param>
    /// <returns>The updated service collection.</returns>
    public static IServiceCollection AddScopedWithNavigationParameters(this IServiceCollection services, Type serviceType, Type implementationType) =>
        services.AddScoped(serviceType, TypeExtensions.GetFactoryWithNavigationParameters(implementationType));

    #endregion Add scoped with navigation parameters

    #region Add transient with navigation parameters

    /// <summary>
    /// Adds a transient service that can be created with navigation parameters.
    /// </summary>
    /// <typeparam name="TService">The type of the service to add.</typeparam>
    /// <param name="services">The service collection to add the service to.</param>
    /// <returns>The updated service collection.</returns>
    public static IServiceCollection AddTransientWithNavigationParameters<TService>(this IServiceCollection services)
        where TService : class
     =>
        services.AddTransient<TService>(TypeExtensions.CreateWithNavigationParameters<TService>);

    /// <summary>
    /// Adds a transient service with a specific implementation that can be created with navigation parameters.
    /// </summary>
    /// <typeparam name="TService">The type of the service to add.</typeparam>
    /// <typeparam name="TImplementation">The type of the implementation to add.</typeparam>
    /// <param name="services">The service collection to add the service to.</param>
    /// <returns>The updated service collection.</returns>
    public static IServiceCollection AddTransientWithNavigationParameters<TService, TImplementation>(this IServiceCollection services)
        where TService : class
        where TImplementation : class, TService
     =>
        services.AddTransient<TService, TImplementation>(TypeExtensions.CreateWithNavigationParameters<TImplementation>);

    /// <summary>
    /// Adds a transient service of the specified type that can be created with navigation parameters.
    /// </summary>
    /// <param name="services">The service collection to add the service to.</param>
    /// <param name="serviceType">The type of the service to add.</param>
    /// <returns>The updated service collection.</returns>
    public static IServiceCollection AddTransientWithNavigationParameters(this IServiceCollection services, Type serviceType)
     =>
        services.AddTransient(serviceType, TypeExtensions.GetFactoryWithNavigationParameters(serviceType));

    /// <summary>
    /// Adds a transient service of the specified type with a specific implementation that can be created with navigation parameters.
    /// </summary>
    /// <param name="services">The service collection to add the service to.</param>
    /// <param name="serviceType">The type of the service to add.</param>
    /// <param name="implementationType">The type of the implementation to add.</param>
    /// <returns>The updated service collection.</returns>
    public static IServiceCollection AddTransientWithNavigationParameters(this IServiceCollection services, Type serviceType, Type implementationType)
     =>
        services.AddTransient(serviceType, TypeExtensions.GetFactoryWithNavigationParameters(implementationType));

    #endregion Add transient with navigation parameters
}
