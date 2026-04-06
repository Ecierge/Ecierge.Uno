namespace Ecierge.Uno.Navigation;

using System.Collections.Generic;

using Microsoft.UI.Xaml;

/// <summary>
/// Provides attached properties for conditionally enabling hit testing based on user permissions.
/// </summary>
public abstract class HitTestVisibleIf
{
    /// <summary>
    /// Identifies the HasPermissions attached dependency property.
    /// When set, the target element is hit-test visible only if the current user
    /// has at least one of the specified permissions.
    /// </summary>
    public static readonly DependencyProperty HasPermissionsProperty =
        DependencyProperty.RegisterAttached(
            "HasPermissions",
            typeof(IEnumerable<string>),
            typeof(HitTestVisibleIf),
            new PropertyMetadata(null, OnHasPermissionsChanged));

    /// <summary>
    /// Internal attached property used to prevent out-of-order async updates from overriding newer results.
    /// </summary>
    private static readonly DependencyProperty RequestVersionProperty =
        DependencyProperty.RegisterAttached(
            "RequestVersion",
            typeof(int),
            typeof(HitTestVisibleIf),
            new PropertyMetadata(0));

    /// <summary>
    /// Gets the permissions required for the element to be hit-test visible.
    /// </summary>
    public static IEnumerable<string>? GetHasPermissions(DependencyObject d) =>
        (IEnumerable<string>?)d.GetValue(HasPermissionsProperty);

    /// <summary>
    /// Sets the permissions required for the element to be hit-test visible.
    /// </summary>
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
                    element.IsHitTestVisible = granted);
            },
            typeof(HitTestVisibleIf),
            "Failed to evaluate permissions for element {ElementType}. Element will remain hit test disabled.");
}
