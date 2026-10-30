namespace Ecierge.Uno.Navigation;

using System;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using Windows.UI.Core;

/// <summary>
/// Connects the root navigator of a window with the system around it: the links that open the application, the history
/// of the browser and the back button of the system.
/// </summary>
internal static class WindowIntegration
{
    /// <summary>
    /// Connects the window. It runs before the first navigation, so that the first page is already recorded in the
    /// history of the browser and the link that started the application waits for the code that opens the first page.
    /// </summary>
    /// <param name="root">The root navigator of the window.</param>
    public static void Attach(Navigator root)
    {
        var services = root.ServiceProvider;
        var options = services.GetRequiredService<IOptions<NavigationOptions>>().Value;
        var logger = services.GetRequiredService<ILoggerFactory>().CreateLogger("Ecierge.Uno.Navigation.SystemIntegration");
        AttachBackButton(root, options, logger);
    }

    private static void AttachBackButton(Navigator root, NavigationOptions options, ILogger logger)
    {
        // The browser has its own back button, and the history of the browser is the business of its own service
        if (OperatingSystem.IsBrowser()) return;
        if (!(options.UseNativeBackButton ?? (OperatingSystem.IsAndroid() || OperatingSystem.IsIOS()))) return;

#pragma warning disable CA1031 // Do not catch general exception types
        try
        {
            SystemNavigationManager.GetForCurrentView().BackRequested += async (_, e) =>
            {
                // Without an entry to go back to, the system leaves the application as it does without this handler
                if (e.Handled || !root.Journal.CanGoBack) return;

                e.Handled = true;
                try
                {
                    await root.GoBackAsync(root).ConfigureAwait(true);
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Failed to go back");
                }
            };
        }
        catch (Exception ex)
        {
            // Some platforms have no system back button for the window
            logger.LogWarning(ex, "The back button of the system is not available");
        }
#pragma warning restore CA1031 // Do not catch general exception types
    }
}
