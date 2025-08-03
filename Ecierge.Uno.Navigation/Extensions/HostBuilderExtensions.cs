#pragma warning disable IDE0022 // Use expression body for method
namespace Ecierge.Uno;

using Ecierge.Uno.Navigation;

using global::Uno.Extensions;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

/// <summary>
/// Extensions for configuring navigation.
/// </summary>
public static class HostBuilderExtensions
{
    /// <summary>
    /// Configures navigation services
    /// </summary>
    /// <param name="hostBuilder">The host builder to configure</param>
    /// <param name="viewRouteBuilder">Callback to define view and route maps</param>
    /// <param name="createViewRegistryBuilder">Callback to create IViewRegistryBuilder implementation</param>
    /// <param name="createRouteRegistryBuilder">Callback to create IRouteRegistryBuilder implementation</param>
    /// <param name="configure">Callback to adjust navigation configuration (default should be to use appsettings.json)</param>
    /// <param name="configureServices">Callback to register other services related to navigation</param>
    /// <returns>The host builder</returns>
    public static IHostBuilder UseNavigation(
        this IHostBuilder hostBuilder,
        Action<IViewRegistryBuilder, INavigationDataRegistryBuilder, IRouteRegistryBuilder>? viewRouteBuilder,
        Func<IServiceCollection, IViewRegistryBuilder>? createViewRegistryBuilder,
        Func<IServiceCollection, IRouteRegistryBuilder>? createRouteRegistryBuilder,
        Func<NavigationOptions, NavigationOptions>? configure,
        Action<IServiceCollection> configureServices)
    {
        return hostBuilder.UseNavigation(
            viewRouteBuilder,
            createViewRegistryBuilder,
            createRouteRegistryBuilder,
            configure,
            (context, builder) => configureServices.Invoke(builder));
    }

    /// <summary>
    /// Configures navigation services
    /// </summary>
    /// <param name="hostBuilder">The host builder to configure</param>
    /// <param name="viewRouteBuilder">Callback to define view and route maps</param>
    /// <param name="createViewRegistryBuilder">Callback to create IViewRegistryBuilder implementation</param>
    /// <param name="createRouteRegistryBuilder">Callback to create IRouteRegistryBuilder implementation</param>
    /// <param name="configure">Callback to adjust navigation configuration (default should be to use appsettings.json)</param>
    /// <param name="configureServices">Callback to register other services related to navigation</param>
    /// <returns>The host builder</returns>
    public static IHostBuilder UseNavigation(
        this IHostBuilder hostBuilder,
        Action<IViewRegistryBuilder, INavigationDataRegistryBuilder, IRouteRegistryBuilder>? viewRouteBuilder = null,
        Func<IServiceCollection, IViewRegistryBuilder>? createViewRegistryBuilder = null,
        Func<IServiceCollection, IRouteRegistryBuilder>? createRouteRegistryBuilder = null,
        Func<NavigationOptions, NavigationOptions>? configure = null,
        Action<HostBuilderContext, IServiceCollection>? configureServices = default)
    {
        if (hostBuilder.IsRegistered(nameof(UseNavigation)))
        {
            return hostBuilder;
        }
        return hostBuilder
            .UseConfiguration(
                configure: configBuilder =>
                    configBuilder
                        .Section<NavigationOptions>(nameof(NavigationOptions)))
            .ConfigureServices((ctx, services) =>
            {
                _ = services.AddNavigation(configure, viewRouteBuilder, createViewRegistryBuilder, createRouteRegistryBuilder);
                configureServices?.Invoke(ctx, services);
            });
    }
}
