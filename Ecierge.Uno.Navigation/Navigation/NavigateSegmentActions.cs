namespace Ecierge.Uno.Navigation;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Ecierge.Uno.Navigation.Regions;

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
        DependencyProperty.Register(nameof(SegmentName), typeof(string), typeof(NavigateSegmentActionBase), new ((string?)null));

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
        DependencyProperty.Register(nameof(SegmentData), typeof(object), typeof(NavigateSegmentActionBase), new ((object?)null));

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
    public object Execute(object sender, object parameter)
    {
        var target = Target ?? sender;

        if (target is FrameworkElement element)
        {
            var navigationRegion = element.FindNavigationRegion();
            if (navigationRegion is null) return NavigationResponse.Failed;
            if (!navigationRegion.Segment.Nested.Any(s => s.Name == SegmentName) && navigationRegion.Parent is not null)
                navigationRegion = navigationRegion.Parent!;
            return navigationRegion.Navigator.NavigateLocalSegmentAsync(sender, SegmentName!, SegmentData);
        }
        return NavigationResponse.Failed;
    }
}

public partial class NavigateNestedSegmentAction : NavigateSegmentActionBase, IAction
{
    public object Execute(object sender, object parameter)
    {
        var target = Target ?? sender;
        if (target is FrameworkElement element)
        {
            var navigationRegion = element.FindNavigationRegion();
            if (navigationRegion is null) return NavigationResponse.Failed;
            return navigationRegion.Navigator.NavigateNestedSegmentAsync(sender, SegmentName!, SegmentData);
        }
        return NavigationResponse.Failed;
    }
}
