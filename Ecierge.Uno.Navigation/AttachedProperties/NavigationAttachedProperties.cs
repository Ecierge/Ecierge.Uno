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
        element.DispatcherQueue.TryEnqueue(DispatcherQueuePriority.Low, async () => navigationRegion.Scope.Dispose());
        element.SetValue(InfoProperty, null);
    }

    internal static Regions.NavigationRegion? GetNavigationRegion([NotNull] this FrameworkElement element) => (Regions.NavigationRegion?)element.GetValue(InfoProperty);

    internal static Regions.NavigationRegion FindNavigationRegion([NotNull] this FrameworkElement element)
    {
        var navigationRegion = element.GetNavigationRegion();
        if (navigationRegion is not null) return navigationRegion;

        foreach (FrameworkElement parent in element.FindAscendants().OfType<FrameworkElement>())
        {
            navigationRegion = parent.GetNavigationRegion();
            if (navigationRegion is not null)
            {
                element.SetNavigationRegion(navigationRegion);
                break;
            }
        }
        if (navigationRegion is null)
        {
            throw new RootNavigationRegionMissingException();
        }
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
            FrameworkElement? parent = element;
            while ((parent = parent!.Parent as FrameworkElement) is not null && parent != navigationBoundary)
            {
                parent.SetNavigationRegion(navigationRegion);
            }
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
