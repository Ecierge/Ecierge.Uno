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
    }

    internal static Regions.NavigationRegion? GetNavigationRegion([NotNull] this FrameworkElement element) => (Regions.NavigationRegion?)element.GetValue(InfoProperty);

    /// <summary>
    /// Finds the navigation region of the element, or else of the nearest ancestor that has one, and remembers it on
    /// the element.
    /// </summary>
    /// <exception cref="RootNavigationRegionMissingException">
    /// Neither the element nor an ancestor of it is in a navigation region.
    /// </exception>
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

    /// <summary>
    /// Makes the element the control of a navigation region inside the region around it. The region shows the nested
    /// segment named <paramref name="segmentName"/>; without a name it shows the segment that the region around it
    /// navigated to last.
    /// </summary>
    /// <exception cref="RootNavigationRegionMissingException">
    /// The element is not inside a navigation region.
    /// </exception>
    /// <exception cref="InvalidOperationException">
    /// <paramref name="segmentName"/> is empty and the region around the element has not navigated to a segment yet.
    /// </exception>
    /// <exception cref="NestedSegmentMissingException">
    /// The segment of the region around the element has no nested segment named <paramref name="segmentName"/>.
    /// </exception>
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
            nestedSegment = parentNavigationRegion.Navigator.Route.LastNamedSegment?.NameSegment
                ?? throw new InvalidOperationException(
                    $"The region '{parentNavigationRegion.Segment.Name}' has not navigated to a segment yet, so the segment of the nested region '{element.Name}' is unknown.");
        }
        else
        {
            NameSegment? foundNestedSegment = nestedSegments.FirstOrDefault(s => s.Name == segmentName);
            if (foundNestedSegment is null) throw new NestedSegmentMissingException(segmentName, parentSegmentName);
            nestedSegment = foundNestedSegment;
        }

#pragma warning disable CA2000 // Dispose objects before losing scope
        var scope = parentNavigationRegion.Scope.CreateScope(parentNavigationRegion.Navigator, nestedSegment, element, navigationBoundary);
#pragma warning restore CA2000 // Dispose objects before losing scope
        var navigationRegion = scope.Region;

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
    /// <exception cref="RootNavigationRegionMissingException">
    /// The window of the element has no root navigator.
    /// </exception>
    internal static Navigator GetRootNavigator(FrameworkElement element)
     => (Navigator?)element.GetValue(RootNavigatorProperty)
        ?? throw new RootNavigationRegionMissingException();

    /// <summary>
    /// Sets the RootNavigator property. This dependency property
    /// indicates the window root navigator.
    /// </summary>
    internal static void SetRootNavigator(UIElement element, Navigator navigator)
     => element.SetValue(RootNavigatorProperty, navigator);

    #endregion

    #region CanGoBack

    /// <summary>
    /// CanGoBack Attached Dependency Property
    /// </summary>
    public static readonly DependencyProperty CanGoBackProperty =
        DependencyProperty.RegisterAttached("CanGoBack", typeof(bool), typeof(Navigation), new(false));

    /// <summary>
    /// Gets the CanGoBack property. This attached property of the content of the window tells whether the history of
    /// the window has an entry before the current one, so that a back button can be enabled by binding to it.
    /// </summary>
    /// <param name="element">The content of the window.</param>
    /// <returns><see langword="true"/> if the window can go back.</returns>
    public static bool GetCanGoBack(DependencyObject element) => (bool)element.GetValue(CanGoBackProperty);

    internal static void SetCanGoBack(DependencyObject element, bool value) => element.SetValue(CanGoBackProperty, value);

    #endregion

    #region CanGoForward

    /// <summary>
    /// CanGoForward Attached Dependency Property
    /// </summary>
    public static readonly DependencyProperty CanGoForwardProperty =
        DependencyProperty.RegisterAttached("CanGoForward", typeof(bool), typeof(Navigation), new(false));

    /// <summary>
    /// Gets the CanGoForward property. This attached property of the content of the window tells whether the history
    /// of the window has an entry after the current one.
    /// </summary>
    /// <param name="element">The content of the window.</param>
    /// <returns><see langword="true"/> if the window can go forward.</returns>
    public static bool GetCanGoForward(DependencyObject element) => (bool)element.GetValue(CanGoForwardProperty);

    internal static void SetCanGoForward(DependencyObject element, bool value) => element.SetValue(CanGoForwardProperty, value);

    /// <summary>
    /// Keeps <see cref="CanGoBackProperty"/> and <see cref="CanGoForwardProperty"/> of the element in step with the
    /// journal. The properties change on the thread of the user interface.
    /// </summary>
    internal static void TrackJournal([NotNull] this FrameworkElement element, NavigationJournal journal)
    {
        void Update()
        {
            SetCanGoBack(element, journal.CanGoBack);
            SetCanGoForward(element, journal.CanGoForward);
        }

        Update();
        journal.Changed += (_, _) =>
        {
            if (element.DispatcherQueue.HasThreadAccess)
                Update();
            else
                element.DispatcherQueue.TryEnqueue(Update);
        };
    }

    #endregion
}
