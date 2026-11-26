namespace Ecierge.Uno.Navigation.Navigators;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

/// <summary>
/// Base class for item selectors used in navigators.
/// </summary>
/// <remarks>
/// A selector keeps no state: the navigator gives it its control and itself with every call, so a selector is never
/// half initialized and one instance can serve a navigator for its whole life.
/// </remarks>
/// <typeparam name="TTarget">Target type to apply navigator to.</typeparam>
public abstract class ItemSelector<TTarget> where TTarget : FrameworkElement
{
    /// <summary>
    /// Selects an item based on the provided navigation request.
    /// </summary>
    /// <param name="target">The control that holds the items.</param>
    /// <param name="navigator">The navigator that selects the item.</param>
    /// <param name="request">The request of the segment whose item is selected.</param>
    /// <returns>The result of the selection.</returns>
    public abstract NavigationResult SelectItem(TTarget target, Navigator navigator, NavigationRequest request);

    /// <summary>
    /// Gets the logger of the selector for the services of the navigator.
    /// </summary>
    /// <param name="navigator">The navigator that selects the item.</param>
    /// <returns>The logger.</returns>
    protected ILogger GetLogger(Navigator navigator)
     => navigator.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger(GetType());
}
