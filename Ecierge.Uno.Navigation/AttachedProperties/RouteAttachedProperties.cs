namespace Ecierge.Uno.Navigation;

using System.Diagnostics.CodeAnalysis;

using Ecierge.Uno.Navigation.Regions;

public static class Route
{
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
