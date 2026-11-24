namespace Ecierge.Uno.Navigation;

using Ecierge.Uno.Navigation.Navigators;
using Ecierge.Uno.Navigation.Routing;

using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;

using CommunityToolkit.WinUI;
using Microsoft.UI.Dispatching;

public static class Navigation
{
    #region NavigationInfo

    public static readonly DependencyProperty InfoProperty =
        DependencyProperty.RegisterAttached("Info", typeof(NavigationRegion), typeof(Uno.Navigation.Navigation), new(null));

    internal static void SetNavigationRegion([NotNull] this FrameworkElement element, Regions.NavigationRegion navigationRegion) => element.SetValue(InfoProperty, navigationRegion);

    internal static void ResetNavigationRegion([NotNull] this FrameworkElement element)
    {
        var navigationRegion = element.GetNavigationRegion();
        if (navigationRegion is null) return;
        element.DispatcherQueue.EnqueueAsync(navigationRegion.Scope.DisposeAsync, DispatcherQueuePriority.Low);
        element.SetValue(InfoProperty, null);

        // The ancestors that AttachRegion marked would keep the disposed region, and an element attached under them later would take it for its parent region.
        // A mark that a later attachment replaced stays.
        foreach (var ancestor in navigationRegion.MarkedAncestors)
        {
            if (ReferenceEquals(ancestor.GetNavigationRegion(), navigationRegion))
            {
                ancestor.SetValue(InfoProperty, null);
            }
        }

        navigationRegion.MarkedAncestors = [];

        // Elements that stored the region keep it, but GetNavigationRegion ignores it from now on, so their next lookup searches again.
        navigationRegion.IsDetached = true;
    }

    /// <summary>
    /// Gets the region stored on the element, or <see langword="null"/> if there is none or it was detached.
    /// </summary>
    internal static Regions.NavigationRegion? GetNavigationRegion([NotNull] this FrameworkElement element) =>
        element.GetValue(InfoProperty) is Regions.NavigationRegion { IsDetached: false } navigationRegion ? navigationRegion : null;

    internal static Regions.NavigationRegion FindNavigationRegion([NotNull] this FrameworkElement element)
    {
        var navigationRegion = element.GetNavigationRegion();
        if (navigationRegion is not null) return navigationRegion;

        // The same walk as AttachRegion, so that the marks it put on the ancestors are found.
        navigationRegion = element.FindParentNavigationRegion() ?? throw new RootNavigationRegionMissingException();
        element.SetNavigationRegion(navigationRegion);
        return navigationRegion;
    }

    public static void AttachRegion([NotNull] this FrameworkElement element, string? segmentName = null)
    {
        var parentNavigationRegion =
            element.FindParentNavigationRegion() ??
            throw new RootNavigationRegionMissingException();

        var navigationBoundary = element.FindNavigationBoundary();

        var parentSegment = parentNavigationRegion.Segment;
        ImmutableArray<NameSegment> nestedSegments;
        string parentSegmentName;
        if (parentSegment.DataSegment is DataSegment dataSegment)
        {
            parentSegmentName = dataSegment.Name;
            nestedSegments = dataSegment.Nested;
        }
        else
        {
            parentSegmentName = parentSegment.Name;
            nestedSegments = parentSegment.Nested;
        }

        NameSegment nestedSegment;
        if (parentSegment is DialogSegment && parentNavigationRegion.Target!.GetType().IsAssignableTo(typeof(ContentDialog)))
        {
            nestedSegment = parentSegment;
        }
        else if (string.IsNullOrEmpty(segmentName))
        {
            nestedSegment = parentNavigationRegion.Navigator.Route.LastNamedSegment!.NameSegment;
        }
        else
        {
            NameSegment? foundNestedSegment = nestedSegments.FirstOrDefault(s => s.Name == segmentName);
            if (foundNestedSegment is null) throw new NestedSegmentMissingException(segmentName, parentSegmentName);
            nestedSegment = foundNestedSegment;
        }

#pragma warning disable CA2000 // Dispose objects before losing scope
        var scope = parentNavigationRegion.Scope.CreateScope(parentNavigationRegion.Navigator, nestedSegment, element);
#pragma warning restore CA2000 // Dispose objects before losing scope
        var navigationRegion = new Regions.NavigationRegion(scope)
        {
            Parent = parentNavigationRegion,
            Target = element,
            Root = navigationBoundary
        };

        element.SetNavigationRegion(navigationRegion);
        if (navigationBoundary is not null)
        {
            // Mark the ancestors up to the boundary, so that FindNavigationRegion resolves the elements around the attached one to its region.
            // The walk is the one FindNavigationBoundary used, so it reaches the boundary.
            List<FrameworkElement> markedAncestors = [];
            for (var parent = element.GetNavigationParent(); parent is not null && parent != navigationBoundary; parent = parent.GetNavigationParent())
            {
                if (parent is FrameworkElement frameworkElement)
                {
                    frameworkElement.SetNavigationRegion(navigationRegion);
                    markedAncestors.Add(frameworkElement);
                }
            }

            // DetachRegion removes these marks again.
            navigationRegion.MarkedAncestors = markedAncestors.ToArray();
        }
    }

    public static void DetachRegion([NotNull] this FrameworkElement element) => element.ResetNavigationRegion();

    #endregion NavigationInfo

    #region RootNavigator

    /// <summary>
    /// RootNavigator Attached Dependency Property
    /// </summary>
    public static readonly DependencyProperty RootNavigatorProperty =
        DependencyProperty.RegisterAttached("RootNavigator", typeof(Navigator), typeof(Navigation), new ((Navigator?)null));

    /// <summary>
    /// Gets the RootNavigator property. This dependency property
    /// indicates the window root navigator.
    /// </summary>
    internal static Navigator GetRootNavigator(FrameworkElement element) => (Navigator)element.GetValue(RootNavigatorProperty);

    /// <summary>
    /// Sets the RootNavigator property. This dependency property
    /// indicates the window root navigator.
    /// </summary>
    internal static void SetRootNavigator(UIElement element, Navigator navigator)
    {
        navigator.RootNavigator = navigator;
        element.SetValue(RootNavigatorProperty, navigator);
    }

    #endregion
}
