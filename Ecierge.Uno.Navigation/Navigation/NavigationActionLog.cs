namespace Ecierge.Uno.Navigation;

using System;
using System.Threading.Tasks;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

/// <summary>
/// Reports the failures of the navigations that the actions of a page start. Nobody awaits what an action returns and
/// the user interface swallows what it throws, so a button whose navigation fails would do nothing and say nothing.
/// </summary>
internal static class NavigationActionLog
{
    /// <summary>
    /// Starts the navigation of an action and logs it when it fails, whether the start throws or the navigation that
    /// it returns fails. The failure still reaches the caller.
    /// </summary>
    /// <param name="action">The name of the action.</param>
    /// <param name="navigator">The navigator that the action navigates through, which gives the logger.</param>
    /// <param name="target">What the action navigates to: the text of a route or the name of a segment.</param>
    /// <param name="navigate">Starts the navigation.</param>
    /// <returns>The navigation that <paramref name="navigate"/> started.</returns>
    internal static ValueTask<NavigationResponse> Run(string action, Navigator navigator, string? target, Func<ValueTask<NavigationResponse>> navigate)
    {
        try
        {
            return Observe(action, navigator, target, navigate());
        }
        catch (Exception ex)
        {
            Log(navigator, action, target, ex);
            throw;
        }
    }

    private static async ValueTask<NavigationResponse> Observe(string action, Navigator navigator, string? target, ValueTask<NavigationResponse> navigation)
    {
        try
        {
            return await navigation;
        }
        catch (Exception ex)
        {
            Log(navigator, action, target, ex);
            throw;
        }
    }

    private static void Log(Navigator navigator, string action, string? target, Exception exception)
     => navigator.ServiceProvider.GetService<ILoggerFactory>()
            ?.CreateLogger(typeof(NavigationActionLog).FullName!)
            .LogError(exception, "The action {Action} could not navigate to '{Target}'", action, target);
}
