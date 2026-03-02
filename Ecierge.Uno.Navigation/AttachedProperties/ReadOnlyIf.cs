namespace Ecierge.Uno.Navigation;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

/// <summary>
/// Provides attached properties for conditionally setting read-only state based on permissions or editability.
/// </summary>
public static class ReadOnlyIf
{
    /// <summary>
    /// Marker type used for logging category.
    /// </summary>
    private sealed class ReadOnlyIfLogger;

    /// <summary>
    /// Identifies the CanEdit attached dependency property.
    /// When set, the target element will be read-only if CanEdit is false.
    /// </summary>
    public static readonly DependencyProperty CanEditProperty =
        DependencyProperty.RegisterAttached(
            "CanEdit",
            typeof(bool),
            typeof(ReadOnlyIf),
            new PropertyMetadata(true, OnCanEditChanged));

    /// <summary>
    /// Identifies the HasPermissions attached dependency property.
    /// When set, the target element will be read-only if the current user does not have all specified permissions.
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
    /// Internal attached property used to store current permissions result.
    /// </summary>
    private static readonly DependencyProperty PermissionsAllowedProperty =
        DependencyProperty.RegisterAttached(
            "PermissionsAllowed",
            typeof(bool),
            typeof(ReadOnlyIf),
            new PropertyMetadata(true));

    /// <summary>
    /// Gets the CanEdit value used to control read-only state.
    /// </summary>
    public static bool GetCanEdit(DependencyObject d) =>
        (bool)d.GetValue(CanEditProperty);

    /// <summary>
    /// Sets the CanEdit value used to control read-only state.
    /// </summary>
    public static void SetCanEdit(DependencyObject d, bool value) =>
        d.SetValue(CanEditProperty, value);

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

    private static int GetRequestVersion(DependencyObject d) =>
        (int)d.GetValue(RequestVersionProperty);

    private static void SetRequestVersion(DependencyObject d, int value) =>
        d.SetValue(RequestVersionProperty, value);

    private static bool GetPermissionsAllowed(DependencyObject d) =>
        (bool)d.GetValue(PermissionsAllowedProperty);

    private static void SetPermissionsAllowed(DependencyObject d, bool value) =>
        d.SetValue(PermissionsAllowedProperty, value);

    private static void OnCanEditChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is FrameworkElement element)
        {
            ApplyReadOnly(element);
        }
    }

    private static void OnHasPermissionsChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is FrameworkElement element)
        {
            element.Loaded -= OnElementLoaded;
            element.Loaded += OnElementLoaded;

            TriggerUpdate(element);
        }
    }

    private static void OnElementLoaded(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement element)
        {
            TriggerUpdate(element);
        }
    }

    private static void TriggerUpdate(FrameworkElement element)
    {
        var version = GetRequestVersion(element) + 1;
        SetRequestVersion(element, version);

        _ = UpdateReadOnlyAsync(element, version);
    }

    private static async Task UpdateReadOnlyAsync(FrameworkElement element, int version)
    {
        var permissions = GetHasPermissions(element)?.ToArray();
        if (permissions is null || permissions.Length == 0)
        {
            if (GetRequestVersion(element) == version)
            {
                SetPermissionsAllowed(element, true);
                ApplyReadOnly(element);
            }

            return;
        }

        if (GetRequestVersion(element) == version)
        {
            SetPermissionsAllowed(element, false);
            ApplyReadOnly(element);
        }

        Regions.NavigationRegion region;
        try
        {
            region = Navigation.FindNavigationRegion(element);
        }
        catch (RootNavigationRegionMissingException)
        {
            return;
        }

        try
        {
            var ruleCheckers = region.Scope.ServiceProvider.GetServices<IAuthorizationService>();
            if (!ruleCheckers.Any())
            {
                return;
            }

            foreach (var checker in ruleCheckers)
            {
                foreach (var permission in permissions)
                {
                    if (!await checker.HasPermissionAsync(permission))
                    {
                        return;
                    }
                }
            }

            if (GetRequestVersion(element) != version)
            {
                return;
            }

            SetPermissionsAllowed(element, true);
            ApplyReadOnly(element);
        }
        catch (Exception ex)
        {
            var logger = region.Scope.ServiceProvider.GetService<ILogger<ReadOnlyIfLogger>>();
            logger?.LogError(
                ex,
                "Failed to evaluate permissions for element {ElementType}. Element will remain read-only.",
                element.GetType());
        }
    }

    private static void ApplyReadOnly(FrameworkElement element)
    {
        var canEdit = GetCanEdit(element);
        var permissionsAllowed = GetPermissionsAllowed(element);
        var isReadOnly = !canEdit || !permissionsAllowed;

        SetIsReadOnly(element, isReadOnly);
    }

    private static void SetIsReadOnly(FrameworkElement element, bool isReadOnly)
    {
        element.DispatcherQueue.TryEnqueue(() =>
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
    }
}
