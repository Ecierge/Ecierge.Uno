namespace Ecierge.Uno.Navigation;

using System.Linq;

using Microsoft.Xaml.Interactivity;

public abstract partial class NavigateSegmentActionBase : DependencyObject
{
    #region TargetElement

    /// <summary>
    /// TargetElement Dependency Property
    /// </summary>
    public static readonly DependencyProperty TargetElementProperty =
        DependencyProperty.Register(nameof(TargetElement), typeof(FrameworkElement), typeof(NavigateSegmentActionBase), new((FrameworkElement?)null));

    /// <summary>
    /// Gets or sets the Target property. This dependency property
    /// indicates the FrameworkElement that has a navigation region.
    /// </summary>
    public FrameworkElement? TargetElement
    {
        get { return (FrameworkElement?)GetValue(TargetElementProperty); }
        set { SetValue(TargetElementProperty, value); }
    }

    #endregion

    #region SegmentName

    /// <summary>
    /// SegmentName Dependency Property
    /// </summary>
    public static readonly DependencyProperty SegmentNameProperty =
        DependencyProperty.Register(nameof(SegmentName), typeof(string), typeof(NavigateSegmentActionBase), new((string?)null));

    /// <summary>
    /// Gets or sets the SegmentName property. This dependency property
    /// indicates the route segment name to navigate.
    /// </summary>
    public string? SegmentName
    {
        get { return (string?)GetValue(SegmentNameProperty); }
        set { SetValue(SegmentNameProperty, value); }
    }

    #endregion

    #region SegmentData

    /// <summary>
    /// SegmentData Dependency Property
    /// </summary>
    public static readonly DependencyProperty SegmentDataProperty =
        DependencyProperty.Register(nameof(SegmentData), typeof(object), typeof(NavigateSegmentActionBase), new((object?)null));

    /// <summary>
    /// Gets or sets the SegmentData property. This dependency property
    /// indicates the nested data route segment data.
    /// </summary>
    public object? SegmentData
    {
        get { return GetValue(SegmentDataProperty); }
        set { SetValue(SegmentDataProperty, value); }
    }

    #endregion
}

public partial class NavigateLocalSegmentAction : NavigateSegmentActionBase, IAction
{
    /// <summary>
    /// Navigates to the segment through the navigator of the region of the target element, or of the region above it
    /// when the segment is not nested in that region.
    /// </summary>
    /// <exception cref="NavigationRegionMissingException">
    /// The target element, or the sender, is not in a navigation region.
    /// </exception>
    public object? Execute(object sender, object parameter)
    {
        var target = TargetElement ?? sender;

        if (target is FrameworkElement element)
        {
            var navigationRegion = element.FindNavigationRegion();
            if (navigationRegion is null) throw new NavigationRegionMissingException(element);
            // TODO: Consider not using this condition
            if (!navigationRegion.Segment.NestedAfterData.Any(s => s.Name == SegmentName) && navigationRegion.Parent is not null)
                navigationRegion = navigationRegion.Parent!;
            return navigationRegion.Navigator.NavigateLocalSegmentAsync(sender, SegmentName!, SegmentData);
        }
        return null;
    }
}

public partial class NavigateNestedSegmentAction : NavigateSegmentActionBase, IAction
{
    /// <summary>
    /// Navigates to a segment that is nested in the segment of the region inside the region of the target element, or
    /// of the sender when no target is set.
    /// </summary>
    /// <exception cref="NavigationRegionMissingException">
    /// The target element, or the sender, is not in a navigation region.
    /// </exception>
    public object? Execute(object sender, object parameter)
    {
        var target = TargetElement ?? sender;
        if (target is FrameworkElement element)
        {
            var navigationRegion = element.FindNavigationRegion();
            if (navigationRegion is null) throw new NavigationRegionMissingException(element);
            return navigationRegion.Navigator.NavigateNestedSegmentAsync(sender, SegmentName!, SegmentData);
        }
        return null;
    }
}

public partial class NavigateDialogSegmentAction : NavigateSegmentActionBase, IAction
{
    /// <summary>
    /// Opens the dialog of the segment over the page of the target element, or of the sender when no target is set.
    /// </summary>
    /// <exception cref="NavigationRegionMissingException">
    /// The target element, or the sender, is not in a navigation region.
    /// </exception>
    public object? Execute(object sender, object parameter)
    {
        var target = TargetElement ?? sender;
        if (target is FrameworkElement element)
        {
            var navigationRegion = element.FindNavigationRegion();
            if (navigationRegion is null) throw new NavigationRegionMissingException(element);
            return navigationRegion.Navigator.NavigateDialogSegmentAsync(sender, SegmentName!, SegmentData);
        }
        return null;
    }
}
