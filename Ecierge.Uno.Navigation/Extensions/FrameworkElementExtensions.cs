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
        control.SetNavigationRegion(new (scope) { Target = control });
        return scope;
    }

    internal static Regions.NavigationRegion? FindParentNavigationRegion([NotNull] this FrameworkElement element)
    {
        var parent = element as FrameworkElement;
        while ((parent = parent!.Parent as FrameworkElement) is not null)
        {
            if (parent.GetNavigationRegion() is Regions.NavigationRegion navigationRegion)
            {
                return navigationRegion;
            }
        }
        return null;
    }

    internal static FrameworkElement? FindNavigationBoundary([NotNull] this FrameworkElement element)
    {
        var parent = element;
        while ((parent = parent!.Parent as FrameworkElement) is not null)
        {
            if (NavigationRegion.GetIsBoundary(parent) || parent.GetType().IsAssignableTo(typeof(Page)) || parent.GetNavigationRegion() is not null)
            {
                return parent;
            }
        }
        return null;
    }
}
