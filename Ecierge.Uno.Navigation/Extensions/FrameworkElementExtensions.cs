namespace Ecierge.Uno.Navigation;

using System;
using System.Diagnostics.CodeAnalysis;

using Ecierge.Uno.Navigation.Regions;

using Microsoft.Extensions.DependencyInjection;

public static class FrameworkElementExtensions
{
    public static IServiceScope AttachRootNavigationRegion([NotNull] this Control control, NavigationScope scope)
    {
        scope = scope ?? throw new ArgumentNullException(nameof(scope));
        control.SetNavigationRegion(new NavigationRegion(scope) { Target = control });
        return scope;
    }

    internal static NavigationRegion? FindParentNavigationRegion([NotNull] this FrameworkElement element)
    {
        var parent = element.Parent as FrameworkElement;
        while (parent is not null)
        {
            if (parent.GetNavigationRegion() is NavigationRegion navigationRegion)
            {
                return navigationRegion;
            }
            parent = parent.Parent as FrameworkElement;
        }
        return null;
    }
}
