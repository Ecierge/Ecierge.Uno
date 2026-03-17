namespace Ecierge.Uno.Navigation;

using System;
using System.Diagnostics.CodeAnalysis;

using Microsoft.Extensions.DependencyInjection;

public static class FrameworkElementExtensions
{
    public static IServiceScope AttachRootNavigationRegion([NotNull] this Control control, NavigationScope scope)
    {
        scope = scope ?? throw new ArgumentNullException(nameof(scope));
        control.SetNavigationRegion(new(scope) { Target = control });
        return scope;
    }

    internal static Regions.NavigationRegion? FindParentNavigationRegion([NotNull] this FrameworkElement element)
    {
        DependencyObject? current = element;

        while (current is not null)
        {
            if (current is FrameworkElement fe)
            {
                var logicalParent = fe.Parent as FrameworkElement;

                if (logicalParent is not null)
                {
                    if (logicalParent.GetNavigationRegion() is Regions.NavigationRegion navRegion)
                        return navRegion;

                    current = logicalParent;
                    continue;
                }
            }

            var visualParent = Microsoft.UI.Xaml.Media.VisualTreeHelper.GetParent(current);

            if (visualParent is FrameworkElement visualFe)
            {
                if (visualFe.GetNavigationRegion() is Regions.NavigationRegion navRegion)
                    return navRegion;
            }

            current = visualParent;
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
