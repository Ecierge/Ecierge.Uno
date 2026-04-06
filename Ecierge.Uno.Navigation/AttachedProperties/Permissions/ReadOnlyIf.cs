namespace Ecierge.Uno.Navigation;

using System.Collections.Generic;

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

/// <summary>
/// Provides attached properties for conditionally setting read-only state based on permissions.
/// Supported controls: <see cref="TextBox"/> and <see cref="RichEditBox"/>.
/// </summary>
public abstract class ReadOnlyIf
{
    /// <summary>
    /// Identifies the HasPermissions attached dependency property.
    /// When set, the target element will be read-only if the current user
    /// does not have any of the specified permissions.
    /// </summary>
    public static readonly DependencyProperty HasPermissionsProperty =
        DependencyProperty.RegisterAttached(
            "HasPermissions",
            typeof(IEnumerable<string>),
            typeof(ReadOnlyIf),
            new PropertyMetadata(null, OnHasPermissionsChanged));

    /// <summary>
    /// Internal attached property used to prevent out-of-order async updates from overriding newer results.
    /// </summary>
    private static readonly DependencyProperty RequestVersionProperty =
        DependencyProperty.RegisterAttached(
            "RequestVersion",
            typeof(int),
            typeof(ReadOnlyIf),
            new PropertyMetadata(0));

    /// <summary>
    /// Gets the permissions required for the element to remain editable.
    /// </summary>
    public static IEnumerable<string>? GetHasPermissions(DependencyObject d) =>
        (IEnumerable<string>?)d.GetValue(HasPermissionsProperty);

    /// <summary>
    /// Sets the permissions required for the element to remain editable.
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
                var isReadOnly = !granted;
                element.DispatcherQueue?.TryEnqueue(() =>
                {
                    switch (element)
                    {
                        case TextBox textBox:
                            textBox.IsReadOnly = isReadOnly;
                            break;

                        case RichEditBox richEditBox:
                            richEditBox.IsReadOnly = isReadOnly;
                            break;
                    }
                });
            },
            typeof(ReadOnlyIf),
            "Failed to evaluate permissions for element {ElementType}. Element will remain read-only.");
}
