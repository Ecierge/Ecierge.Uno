namespace Ecierge.Uno.Navigation;

using System.Diagnostics.CodeAnalysis;

using Ecierge.Uno.Navigation.Regions;

public static class Route
{
    #region NavigationTarget

    /// <summary>
    /// NavigationTarget Attached Dependency Property
    /// </summary>
    public static readonly DependencyProperty NavigationTargetProperty =
        DependencyProperty.RegisterAttached("NavigationTarget", typeof(FrameworkElement), typeof(Route), new((FrameworkElement?)null));

    /// <summary>
    /// Gets the NavigationTarget property. This dependency property
    /// indicates the navigation target.
    /// </summary>
    public static FrameworkElement GetNavigationTarget([NotNull] this FrameworkElement element) => (FrameworkElement)element.GetValue(NavigationTargetProperty);

    /// <summary>
    /// Sets the NavigationTarget property. This dependency property
    /// indicates the navigation target.
    /// </summary>
    public static void SetNavigationTarget([NotNull] this FrameworkElement element, FrameworkElement value) => element.SetValue(NavigationTargetProperty, value);

    #endregion NavigationTarget

    #region SegmentName

    /// <summary>
    /// SegmentName Attached Dependency Property
    /// </summary>
    public static readonly DependencyProperty SegmentNameProperty =
        DependencyProperty.RegisterAttached("SegmentName", typeof(string), typeof(Route), new ((string?)null));

    /// <summary>
    /// Gets the SegmentName property. This dependency property
    /// indicates the segment name of selected item.
    /// </summary>
    public static string GetSegmentName(FrameworkElement element) => (string?)element.GetValue(SegmentNameProperty) ?? element.Name;

    /// <summary>
    /// Sets the SegmentName property. This dependency property
    /// indicates the segment name of selected item.
    /// </summary>
    public static void SetSegmentName(FrameworkElement element, string value) => element.SetValue(SegmentNameProperty, value);

    #endregion SegmentName

    #region SelectIf

    /// <summary>
    /// SelectIf Attached Dependency Property
    /// </summary>
    public static readonly DependencyProperty SelectIfProperty =
        DependencyProperty.RegisterAttached("SelectIf", typeof(Routing.Route), typeof(Route), new (default(Routing.Route)));

    /// <summary>
    /// Gets the SelectIf property. This dependency property
    /// indicates if an item must be selected for route.
    /// </summary>
    public static Routing.Route GetSelectIf(DependencyObject d) => (Routing.Route)d.GetValue(SelectIfProperty);

    /// <summary>
    /// Sets the SelectIf property. This dependency property
    /// indicates if an item must be selected for route.
    /// </summary>
    public static void SetSelectIf(DependencyObject d, Routing.Route value) => d.SetValue(SelectIfProperty, value);

    #endregion SelectIf
}
