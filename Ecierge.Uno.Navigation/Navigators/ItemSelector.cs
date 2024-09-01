namespace Ecierge.Uno.Navigation.Navigators;

using System;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

public abstract class ItemSelector<TTarget> where TTarget : FrameworkElement
{
    public Navigator Navigator { get; set; }

    protected TTarget Target => (TTarget)Navigator.Target;
    protected ILogger Logger => Navigator.ServiceProvider.GetRequiredService<ILogger<ItemSelector<TTarget>>>();

    public abstract NavigationResult SelectItem(NavigationRequest request);
}
