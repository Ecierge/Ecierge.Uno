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

    /// <summary>
    /// Gets the next element up the tree for the navigation lookups: the logical parent when the element has one, otherwise the visual parent.
    /// </summary>
    /// <remarks>
    /// The logical tree has gaps: <see cref="FrameworkElement.Parent"/> is <see langword="null"/> for content that a template or a presenter hosts. The walk crosses such a gap through <see cref="VisualTreeHelper.GetParent(DependencyObject)"/> and goes back to the logical tree as soon as an element has a logical parent again. All the navigation lookups that climb the tree use this one step, so they agree on the ancestors of an element.
    /// </remarks>
    /// <param name="element">The element to start from.</param>
    /// <returns>The parent, or <see langword="null"/> at the root of the tree.</returns>
    internal static DependencyObject? GetNavigationParent(this DependencyObject element) =>
        element is FrameworkElement { Parent: FrameworkElement logicalParent }
            ? logicalParent
            : VisualTreeHelper.GetParent(element);

    /// <summary>
    /// Finds the navigation region of the nearest ancestor that has one.
    /// </summary>
    /// <param name="element">The element whose ancestors are searched; its own region is not considered.</param>
    /// <returns>The region of the nearest ancestor, or <see langword="null"/> when no ancestor has one.</returns>
    internal static Regions.NavigationRegion? FindParentNavigationRegion([NotNull] this FrameworkElement element)
    {
        for (var parent = element.GetNavigationParent(); parent is not null; parent = parent.GetNavigationParent())
        {
            if (parent is FrameworkElement frameworkElement && frameworkElement.GetNavigationRegion() is Regions.NavigationRegion navigationRegion)
            {
                return navigationRegion;
            }
        }

        return null;
    }

    /// <summary>
    /// Finds the nearest ancestor that bounds a navigation region attached to <paramref name="element"/>: an element marked with <see cref="NavigationRegion.IsBoundaryProperty"/>, a <see cref="Page"/> or an element that already has a navigation region.
    /// </summary>
    /// <param name="element">The element whose ancestors are searched.</param>
    /// <returns>The boundary, or <see langword="null"/> when no ancestor qualifies.</returns>
    internal static FrameworkElement? FindNavigationBoundary([NotNull] this FrameworkElement element)
    {
        for (var parent = element.GetNavigationParent(); parent is not null; parent = parent.GetNavigationParent())
        {
            if (parent is FrameworkElement frameworkElement
                && (NavigationRegion.GetIsBoundary(frameworkElement) || frameworkElement is Page || frameworkElement.GetNavigationRegion() is not null))
            {
                return frameworkElement;
            }
        }

        return null;
    }
}
