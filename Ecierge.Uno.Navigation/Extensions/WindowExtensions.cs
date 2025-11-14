namespace Ecierge.Uno.Navigation;

using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;

using Ecierge.Uno.Navigation;
using Ecierge.Uno.Navigation.UI;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.UI.Xaml.Controls;

public static partial class WindowExtensions
{
    public static async Task NavigateAsync<TShell>(
        [NotNull] this Window window,
        IServiceProvider serviceProvider,
        Func<IServiceProvider, Navigator, Task>? initialNavigate = null,
        Func<TShell, ContentControl>? getNavigationRoot = null)
    where TShell : Control
    {
        serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));

        var appRoot = serviceProvider.GetRequiredService<TShell>();
        window.Content = appRoot;

        bool requiresDelayedActivation = false;
        // TODO: Implement schema activation check
#if WINDOWS10_0_17763_0_OR_GREATER
        requiresDelayedActivation = Activation.IsAppInstanceActivationSupported() && Activation.GetProtocolActivationUriWithWindowsRuntime() is Uri;
#elif DESKTOP
        requiresDelayedActivation = Activation.GetProtocolActivationUriFromCommandLineArguments(Environment.GetCommandLineArgs()) is Uri;
#else
#endif
        var hostLifetime = serviceProvider.GetRequiredService<IHostApplicationLifetime>();
        if (!requiresDelayedActivation) window.Activate();
        else await Task.Run(() => hostLifetime.ApplicationStarted.WaitHandle.WaitOne());

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
        var navigationScope = new NavigationScope(serviceProvider.CreateScope(), window, rootSegment, navRoot);
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
            await initialNavigate.Invoke(navigationScope.ServiceProvider, navigator).ConfigureAwait(true);
        }
        else
        {
            await navigator.NavigateDefaultAsync(Application.Current, rootSegment);
        }
    }

    public static Navigator GetRootNavigator([NotNull] this Window window)
    {
        return Navigation.GetRootNavigator((FrameworkElement)window.Content!);
    }

    public static XamlRoot? GetXamlRoot([NotNull] this Window window) => window.Content?.XamlRoot;
    public static void SetXamlRootTo([NotNull] this Window window, [NotNull] UIElement element) => element.XamlRoot = window.Content?.XamlRoot;
}
