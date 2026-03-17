namespace Ecierge.Uno.Navigation;

using System.Collections.Generic;

using Microsoft.UI.Xaml;

/// <summary>
/// Provides attached properties for conditionally showing UI elements based on user permissions.
/// </summary>
public abstract class VisibleIf
{
    /// <summary>
    /// Identifies the HasPermissions attached dependency property.
    /// When set, the target element will be visible if the current user has at least one of the specified permissions.
    /// </summary>
    public static readonly DependencyProperty HasPermissionsProperty =
        DependencyProperty.RegisterAttached(
            "HasPermissions",
            typeof(IEnumerable<string>),
            typeof(VisibleIf),
            new PropertyMetadata(null, OnHasPermissionsChanged));

    /// <summary>
    /// Internal attached property used to prevent out-of-order async updates from overriding newer results.
    /// </summary>
    private static readonly DependencyProperty RequestVersionProperty =
        DependencyProperty.RegisterAttached(
            "RequestVersion",
            typeof(int),
            typeof(VisibleIf),
            new PropertyMetadata(0));

    /// <summary>
    /// Gets the permissions required for the element to be visible.
    /// </summary>
    /// <param name="d">The target object.</param>
    /// <returns>The required permissions, or null if not set.</returns>
    public static IEnumerable<string>? GetHasPermissions(DependencyObject d) =>
        (IEnumerable<string>?)d.GetValue(HasPermissionsProperty);

    /// <summary>
    /// Sets the permissions required for the element to be visible.
    /// </summary>
    /// <param name="d">The target object.</param>
    /// <param name="value">The required permissions.</param>
    public static void SetHasPermissions(DependencyObject d, IEnumerable<string>? value) =>
        d.SetValue(HasPermissionsProperty, value);

    private static void OnHasPermissionsChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) =>
        PermissionHelper.OnPermissionsPropertyChanged(
            d,
            HasPermissionsProperty,
            RequestVersionProperty,
            static (element, granted) =>
            {
                element.DispatcherQueue?.TryEnqueue(() =>
                    element.Visibility = granted ? Visibility.Visible : Visibility.Collapsed);
            },
            typeof(VisibleIf),
            "Failed to evaluate permissions for element {ElementType}. Element will remain collapsed.");
}
