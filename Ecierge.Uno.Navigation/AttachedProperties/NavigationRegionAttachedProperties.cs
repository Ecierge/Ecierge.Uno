namespace Ecierge.Uno.Navigation;

using System.Diagnostics.CodeAnalysis;

public static class NavigationRegion
{
    #region ForSegment

    /// <summary>
    /// ForSegment Attached Dependency Property
    /// </summary>
    public static readonly DependencyProperty ForSegmentProperty =
        DependencyProperty.RegisterAttached("ForSegment", typeof(string), typeof(NavigationRegion), new(null, OnForSegmentChanged));

    /// <summary>
    /// Gets the ForSegment property. This dependency property
    /// indicates the name of segment for navigation within it.
    /// </summary>
    public static string? GetForSegment(FrameworkElement element) => (string?)element.GetValue(ForSegmentProperty);

    /// <summary>
    /// Sets the ForSegment property. This dependency property
    /// indicates the name of segment for navigation within it.
    /// </summary>
    public static void SetForSegment(FrameworkElement element, string value) => element.SetValue(ForSegmentProperty, value);

    /// <summary>
    /// Handles changes to the ForSegment property.
    /// </summary>
    private static void OnForSegmentChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        string oldSegmentName = (string)e.OldValue;
        string newSegmentName = (string)d.GetValue(ForSegmentProperty);

        if (d is FrameworkElement element)
        {
            if (!element.IsLoaded)
            {
                element.Loaded += OnLoaded;
            }
            else
            {
                if (shouldAttach && !isAttached)
                {
                    element.AttachRegion();
                    element.Unloaded += OnUnloaded;
                }
                if (!shouldAttach && isAttached)
                {
                    element.DetachRegion();
                }
            }

            void OnLoaded(object e, RoutedEventArgs args)
            {
                element.Loaded -= OnLoaded;
                element.AttachRegion();
                element.Unloaded += OnUnloaded;
            }

            void OnUnloaded(object e, RoutedEventArgs args)
            {
                element.Unloaded -= OnUnloaded;
                element.GetNavigationRegion()!.Scope!.Dispose();
                element.SetValue(Uno.Navigation.Navigation.InfoProperty, null);
            }
        }

    }

    #endregion ForSegment

    #region AttachForSegmentName

    /// <summary>
    /// AttachForSegmentName Attach Dependency Property
    /// </summary>
    public static readonly DependencyProperty AttachForSegmentNameProperty =
        DependencyProperty.RegisterAttached("AttachForSegmentName", typeof(string), typeof(NavigationRegion), new((string?)null, OnAttachForSegmentNameChanged));

    /// <summary>
    /// Gets the AttachForSegmentName property. This dependency property
    /// indicates the name of segment for navigation within it.
    /// </summary>
    public static string? GetAttachForSegmentName(FrameworkElement element) => (string?)element.GetValue(AttachForSegmentNameProperty);

    /// <summary>
    /// Sets the AttachForSegmentName property. This dependency property
    /// indicates the name of segment for navigation within it.
    /// </summary>
    public static void SetAttachForSegmentName(FrameworkElement element, string? value) => element.SetValue(AttachForSegmentNameProperty, value);

    /// <summary>
    /// Handles changes to the AttachForSegmentName property.
    /// </summary>
    private static void OnAttachForSegmentNameChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (GetAttach((d as FrameworkElement)!))
            throw new InvalidOperationException("Cannot set AttachForSegmentName and Attach properties at the same time.");

        string oldValue = (string)e.OldValue;
        string newValue = (string)d.GetValue(AttachForSegmentNameProperty);

        if (d is FrameworkElement element)
        {
            if (!element.IsLoaded)
            {
                element.Loaded += OnLoaded;
            }
            else
            {
                element.AttachRegion(newValue);
                element.Unloaded += OnUnloaded;
            }

            void OnLoaded(object e, RoutedEventArgs args)
            {
                element.Loaded -= OnLoaded;
                element.AttachRegion(newValue);
                element.Unloaded += OnUnloaded;
            }

            void OnUnloaded(object e, RoutedEventArgs args)
            {
                element.Unloaded -= OnUnloaded;
                element.DetachRegion();
            }
        }

    }

    #endregion AttachForSegmentName

    #region IsBoundary

    /// <summary>
    /// IsBoundary Attached Dependency Property
    /// </summary>
    public static readonly DependencyProperty IsBoundaryProperty =
        DependencyProperty.RegisterAttached("IsBoundary", typeof(bool), typeof(NavigationRegion), new ((bool)false));

    /// <summary>
    /// Gets the IsBoundary property. This dependency property
    /// indicates if current element is navigation boundary.
    /// </summary>
    public static bool GetIsBoundary(DependencyObject d) => (bool)d.GetValue(IsBoundaryProperty);

    /// <summary>
    /// Sets the IsBoundary property. This dependency property
    /// indicates if current element is navigation boundary.
    /// </summary>
    public static void SetIsBoundary(DependencyObject d, bool value) => d.SetValue(IsBoundaryProperty, value);

    #endregion

    #region NestedSegmentName

    /// <summary>
    /// NestedSegmentName Attached Dependency Property
    /// </summary>
    public static readonly DependencyProperty NestedSegmentNameProperty =
        DependencyProperty.RegisterAttached("NestedSegmentName", typeof(string), typeof(NavigationRegion), new(null));

    /// <summary>
    /// Gets the NestedSegmentName property. This dependency property
    /// indicates the name of navigated nested route segment.
    /// </summary>
    public static string? GetNestedSegmentName([NotNull] this FrameworkElement element) => (string?)element.GetValue(NestedSegmentNameProperty);

    /// <summary>
    /// Sets the NestedSegmentName property. This dependency property
    /// indicates the name of navigated nested route segment.
    /// </summary>
    public static void SetNestedSegmentName([NotNull] this FrameworkElement element, string value) => element.SetValue(NestedSegmentNameProperty, value);

    /// <summary>
    /// Clears the NestedSegmentName property. This dependency property
    /// indicates the name of navigated nested route segment.
    /// </summary>
    public static void ClearNestedSegmentName([NotNull] this FrameworkElement element) => element.ClearValue(NestedSegmentNameProperty);

    #endregion NestedSegmentName

    #region NavigatorType

    /// <summary>
    /// NavigatorType Attached Dependency Property
    /// </summary>
    public static readonly DependencyProperty NavigatorTypeProperty =
        DependencyProperty.RegisterAttached("NavigatorType", typeof(Type), typeof(NavigationRegion), new(null));

    /// <summary>
    /// Gets the NavigatorType property. This dependency property
    /// indicates the navigator type used.
    /// </summary>
    public static Type? GetNavigatorType([NotNull] this FrameworkElement element) => (Type)element.GetValue(NavigatorTypeProperty);

    /// <summary>
    /// Sets the NavigatorType property. This dependency property
    /// indicates the navigator type used.
    /// </summary>
    public static void SetNavigatorType([NotNull] this FrameworkElement element, Type value) => element.SetValue(NavigatorTypeProperty, value);

    #endregion NavigatorType

    #region ItemSelectorType

    /// <summary>
    /// ItemSelectorType Attached Dependency Property
    /// </summary>
    public static readonly DependencyProperty ItemSelectorTypeProperty =
        DependencyProperty.RegisterAttached("ItemSelectorType", typeof(Type), typeof(NavigationRegion), new((Type?)null));

    /// <summary>
    /// Gets the ItemSelectorType property. This dependency property
    /// indicates the item selector type used to select an item if navigated from code.
    /// </summary>
    public static Type? GetItemSelectorType(DependencyObject d) => (Type?)d.GetValue(ItemSelectorTypeProperty);

    /// <summary>
    /// Sets the ItemSelectorType property. This dependency property
    /// indicates the item selector type used to select an item if navigated from code.
    /// </summary>
    public static void SetItemSelectorType(DependencyObject d, Type value) => d.SetValue(ItemSelectorTypeProperty, value);

    #endregion ItemSelectorType
}
