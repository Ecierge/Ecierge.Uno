namespace Ecierge.Uno.Navigation;

using System.Linq;

using Microsoft.Xaml.Interactivity;

public abstract partial class NavigateSegmentActionBase : DependencyObject
{
    #region Target

    /// <summary>
    /// Target Dependency Property
    /// </summary>
    public static readonly DependencyProperty TargetProperty =
        DependencyProperty.Register(nameof(Target), typeof(FrameworkElement), typeof(NavigateSegmentActionBase), new((FrameworkElement?)null));

    /// <summary>
    /// Gets or sets the Target property. This dependency property
    /// indicates the FrameworkElement that has a navigation region.
    /// </summary>
    public FrameworkElement? Target
    {
        get { return (FrameworkElement?)GetValue(TargetProperty); }
        set { SetValue(TargetProperty, value); }
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
    public object? Execute(object sender, object parameter)
    {
        var target = Target ?? sender;

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
    public object? Execute(object sender, object parameter)
    {
        var target = Target ?? sender;
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
    public object? Execute(object sender, object parameter)
    {
        var target = Target ?? sender;
        if (target is FrameworkElement element)
        {
            var navigationRegion = element.FindNavigationRegion();
            if (navigationRegion is null) throw new NavigationRegionMissingException(element);
            return navigationRegion.Navigator.NavigateDialogSegmentAsync(sender, SegmentName!, SegmentData);
        }
        return null;
    }
}
