namespace Ecierge.Uno.Navigation;

using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;

using Ecierge.Uno.Navigation;
using Ecierge.Uno.Navigation.UI;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.UI.Xaml.Controls;

public static class WindowExtensions
{
    public static async Task NavigateAsync<TShell>(
        [NotNull] this Window window,
        IServiceProvider serviceProvider,
        Func<IServiceProvider, Navigator, Task>? initialNavigate,
        Func<TShell, ContentControl>? getNavigationRoot = null)
    where TShell : Control
    {
        window = window ?? throw new ArgumentNullException(nameof(window));
        serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));

        var appRoot = serviceProvider.GetRequiredService<TShell>();
        window.Content = appRoot;

        bool requiresDelayedActivation = false;
        // TODO: Implement schema activation check
#if WINDOWS10_0_17763_0_OR_GREATER
#elif DESKTOP
#else
#endif
        if (!requiresDelayedActivation) window.Activate();

        var hostLifetime = serviceProvider.GetRequiredService<IHostApplicationLifetime>();

        Control navRoot = default!;
        if (getNavigationRoot is not null)
            navRoot = getNavigationRoot(appRoot);
        else
            navRoot = appRoot;
        var viewRegistry = serviceProvider.GetRequiredService<IViewRegistry>();
        var viewMap = viewRegistry[typeof(TShell)];
        var rootSegment = serviceProvider.GetRequiredService<IRouteRegistry>().RootSegment;
        rootSegment = new NameSegment(Qualifiers.Root, viewMap, isDefault: true, nested: rootSegment.Nested);
#pragma warning disable CA2000 // Dispose objects before losing scope
        var navigationScope = new NavigationScope(serviceProvider.CreateScope(), window, rootSegment, navRoot, null);
#pragma warning restore CA2000 // Dispose objects before losing scope
        navRoot.AttachRootNavigationRegion(navigationScope);

        if (navRoot is LoadingView loadingView)
        {
            loadingView.Source = new LoadingTask(hostLifetime.ApplicationStarted, navRoot);
        }
        // Activate the window after the application has started
        hostLifetime.ApplicationStarted.Register(() => window.Activate());

        var navigator = navigationScope.ServiceProvider.GetRequiredService<Navigator>();
        Navigation.SetRootNavigator(window.Content!, navigator);
        if (initialNavigate is not null)
        {
            await initialNavigate.Invoke(serviceProvider, navigator).ConfigureAwait(true);
        }
        else
        {
            await navigator.NavigateDefaultAsync(Application.Current, rootSegment);
        }
    }
}
