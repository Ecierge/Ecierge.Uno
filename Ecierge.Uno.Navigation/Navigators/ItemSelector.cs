namespace Ecierge.Uno.Navigation.Navigators;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

public abstract class ItemSelector<TTarget> where TTarget : FrameworkElement
{
    Navigator navigator = default!;
    public Navigator Navigator
    {
        get => navigator;
        set
        {
            navigator = value;
            Target = (TTarget)navigator.Target;
            Logger = Navigator.ServiceProvider.GetRequiredService<ILogger<ItemSelector<TTarget>>>();
        }
    }

    protected TTarget Target { get; private set; } = default!;
    protected ILogger Logger { get; private set; } = default!;

    public abstract NavigationResult SelectItem(NavigationRequest request);
}
