namespace Ecierge.Uno.Navigation;

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.UI.Dispatching;

using global::Uno.Extensions;

using Ecierge.Uno.Navigation.Navigators;

public static class ServiceCollectionExtensions
{
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
        INavigationDataRegistryBuilder navigationDataRegistryBuilder = new NavigationDataRegistryBuilder();
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

                .AddScopedInstance<Window>()
                .AddScopedInstance<FrameworkElement>()
                .AddScopedInstance<DispatcherQueue>()
                .AddScopedInstance<NavigationScope>()
                .AddScopedInstance<Navigator>()
                .AddScopedInstance<NameSegment>()

                .AddScoped<NavigationData>()
                .AddTransient<INavigationData>(sp => sp.GetRequiredService<NavigationData>())

                //.AddSingleton<IResponseNavigatorFactory, ResponseNavigatorFactory>()

                .AddSingleton<RouteNotifier>()
                .AddSingleton<IRouteNotifier>(sp => sp.GetRequiredService<RouteNotifier>())
                .AddSingleton<IRouteUpdater>(sp => sp.GetRequiredService<RouteNotifier>())
                //.AddHostedService<BrowserAddressBarService>()
                //.AddHostedService<BackButtonService>()
                //.AddScoped<Navigator>()


                // Register the region for each control type
                //.AddNavigator<Frame, FrameNavigator>()
                .AddNavigator<ContentControl, ContentControlNavigator>()
                //.AddNavigator<Panel, PanelVisiblityNavigator>(name: PanelVisiblityNavigator.NavigatorName)
                //.AddNavigator<Microsoft.UI.Xaml.Controls.NavigationView, NavigationViewNavigator>()
                //.AddNavigator<ContentDialog, ContentDialogNavigator>(true)
                //.AddNavigator<MessageDialog, MessageDialogNavigator>(true)
                //.AddNavigator<Flyout, FlyoutNavigator>(true)
                //.AddNavigator<Popup, PopupNavigator>(true)
                .AddNavigator<SelectorBar, SelectorBarNavigator>()

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
                //.AddSingleton<RouteResolver>()
                //.AddSingleton<RouteResolverDefault>()
                //.AddSingleton<IRouteResolver>(sp =>
                //{
                //    var config = sp.GetRequiredService<NavigationOptions>();
                //    return (sp.GetRequiredService(config.RouteResolver!) as IRouteResolver)!;
                //})


                //.AddScoped<NavigationDataProvider>()
                //.AddScoped<RegionControlProvider>()
                //.AddTransient<IDictionary<string, object>>(services => services.GetRequiredService<NavigationDataProvider>().Parameters)
    }

    public static IServiceCollection AddScopedInstance<T>(this IServiceCollection services)
        where T : class
    {
#pragma warning disable CS8603 // Possible null reference return.
        return services.AddTransient<T>(sp => sp.GetInstance<T>());
#pragma warning restore CS8603 // Possible null reference return.
    }


#pragma warning disable IDE0022 // Use expression body for method
    public static IServiceCollection AddNavigator<TControl, TNavigator>(
          [NotNull] this IServiceCollection services
        , string? name = null
        , bool isTransient = false
        )
        where TControl : Control
        where TNavigator : Navigator
    {
        return services
            .AddScoped<TNavigator>()
            .AddSingleton(new Tuple<Type, Type>(typeof(TControl), typeof(TNavigator)));
    }
#pragma warning restore IDE0022 // Use expression body for method
}
