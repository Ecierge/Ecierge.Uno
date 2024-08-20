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
}
