namespace Ecierge.Uno.Navigation;

using Ecierge.Uno.Navigation.Navigators;
using Ecierge.Uno.Navigation.Routing;

using System.Collections.Immutable;

using System.Diagnostics.CodeAnalysis;

public static class Navigation
{
    #region NavigationInfo

    public static readonly DependencyProperty InfoProperty =
        DependencyProperty.RegisterAttached("Info", typeof(NavigationRegion), typeof(Uno.Navigation.Navigation), new(null));

    internal static void SetNavigationRegion([NotNull] this FrameworkElement element, Regions.NavigationRegion navigationRegion) => element.SetValue(InfoProperty, navigationRegion);

    internal static Regions.NavigationRegion? GetNavigationRegion([NotNull] this FrameworkElement element) => (Regions.NavigationRegion?)element.GetValue(InfoProperty);

    internal static Regions.NavigationRegion FindNavigationRegion([NotNull] this FrameworkElement element)
    {
        var regionElement = element;
        var navigationRegion = element.GetNavigationRegion();
        while (navigationRegion is null && regionElement.Parent is not null)
        {
            regionElement = (FrameworkElement)regionElement.Parent;
            navigationRegion = regionElement.GetNavigationRegion();
        }
        if (navigationRegion is null)
        {
            throw new RootNavigationRegionMissingException();
        }
        return navigationRegion;
    }

    public static void SetSegment([NotNull] this FrameworkElement element, string value, FrameworkElement root)
    {
        var parentNavigationRegion =
            root.GetNavigationRegion() ??
            root.FindParentNavigationRegion() ??
            throw new RootNavigationRegionMissingException();
        var parentSegment = parentNavigationRegion.Segment;
        ImmutableArray<NameSegment> nestedSegments;
        string parentSegmentName;
        if (parentSegment.Data is DataSegment dataSegment)
        {
            parentSegmentName = dataSegment.Name;
            nestedSegments = dataSegment.Nested;
        }
        else
        {
            parentSegmentName = parentSegment.Name;
            nestedSegments = parentSegment.Nested;
        }

        NameSegment? nestedSegment;
        if (parentSegment is DialogSegment && parentNavigationRegion.Target!.GetType().IsAssignableTo(typeof(ContentDialog)))
        {
            nestedSegment = parentSegment;
        }
        else
        {
            nestedSegment = nestedSegments.FirstOrDefault(s => s.Name == value);
        }

        if (nestedSegment is null) throw new NestedSegmentMissingException(value, parentSegmentName);
#pragma warning disable CA2000 // Dispose objects before losing scope
        var scope = parentNavigationRegion.Scope.CreateScope(parentNavigationRegion.Navigator, nestedSegment, element);
#pragma warning restore CA2000 // Dispose objects before losing scope
        var navigationRegion = new Regions.NavigationRegion(scope)
        {
            Parent = parentNavigationRegion,
            Target = element,
            Root = root
        };

        element.SetNavigationRegion(navigationRegion);
    }

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
