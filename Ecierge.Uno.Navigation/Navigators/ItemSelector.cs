namespace Ecierge.Uno.Navigation.Navigators;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

/// <summary>
/// Base class for item selectors used in navigators.
/// </summary>
/// <typeparam name="TTarget">Target type to apply navigator to.</typeparam>
public abstract class ItemSelector<TTarget> where TTarget : FrameworkElement
{
    Navigator navigator = default!;
    public Navigator Navigator
    {
        get => navigator;
        set
        {
            navigator = value;
            Target = (TTarget)navigator.Target!;
            Logger = Navigator.ServiceProvider.GetRequiredService<ILogger<ItemSelector<TTarget>>>();
        }
    }

    protected TTarget Target { get; private set; } = default!;
    protected ILogger Logger { get; private set; } = default!;

    /// <summary>
    /// Selects an item based on the provided navigation request.
    /// </summary>
    public abstract NavigationResult SelectItem(NavigationRequest request);
}
