namespace Ecierge.Uno.Navigation;

using Microsoft.Xaml.Interactivity;

public abstract partial class NavigateRouteActionBase : DependencyObject
{
    protected Regions.NavigationRegion? navigationRegion;

    #region Route

    /// <summary>
    /// Route Dependency Property
    /// </summary>
    public static readonly DependencyProperty RouteProperty =
        DependencyProperty.Register(nameof(Route), typeof(string), typeof(NavigateRouteActionBase),
            new((string?)null));

    /// <summary>
    /// Gets or sets the Route property. This dependency property
    /// indicates the route to navigate to.
    /// </summary>
    public string? Route
    {
        get { return (string?)GetValue(RouteProperty); }
        set { SetValue(RouteProperty, value); }
    }

    #endregion

    #region NavigationData

    /// <summary>
    /// NavigationData Dependency Property
    /// </summary>
    public static readonly DependencyProperty NavigationDataProperty =
        DependencyProperty.Register(nameof(NavigationData), typeof(INavigationData), typeof(NavigateRouteActionBase),
            new((INavigationData?)null));

    /// <summary>
    /// Gets or sets the NavigationData property. This dependency property
    /// indicates the route navigation data.
    /// </summary>
    public INavigationData? NavigationData
    {
        get { return (INavigationData?)GetValue(NavigationDataProperty); }
        set { SetValue(NavigationDataProperty, value); }
    }

    #endregion
}

public partial class NavigateRootRouteAction : NavigateRouteActionBase, IAction
{
    public object? Execute(object sender, object parameter)
    {
        if (navigationRegion is null && sender is FrameworkElement element)
        {
            navigationRegion = element.FindNavigationRegion();
            if (navigationRegion is null) throw new NavigationRegionMissingException(element);
        }
        if (navigationRegion is not null)
        {
            return navigationRegion.Navigator.RootNavigator.NavigateRouteAsync(sender, Route!, NavigationData);
        }
        return null;
    }
}

public abstract partial class NavigateTargetRouteActionBase : NavigateRouteActionBase
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
}

public partial class NavigateLocalRouteAction : NavigateTargetRouteActionBase, IAction
{
    public object? Execute(object sender, object parameter)
    {
        var target = Target ?? sender;

        if (navigationRegion is null && target is FrameworkElement element)
        {
            navigationRegion = element.FindNavigationRegion();
            if (navigationRegion is null) throw new NavigationRegionMissingException(element);
        }
        if (navigationRegion is not null)
        {
            return navigationRegion.Navigator.NavigateRouteAsync(sender, Route!, NavigationData);
        }
        return null;
    }
}

public partial class NavigateNestedRouteAction : NavigateTargetRouteActionBase, IAction
{
    public object? Execute(object sender, object parameter)
    {
        var target = Target ?? sender;
        if (navigationRegion is null && target is FrameworkElement element)
        {
            navigationRegion = element.FindNavigationRegion();
            if (navigationRegion is null) throw new NavigationRegionMissingException(element);
        }
        if (navigationRegion is not null)
        {
            return navigationRegion.Navigator.ChildNavigator!.NavigateRouteAsync(sender, Route!, NavigationData);
        }
        return null;
    }
}
