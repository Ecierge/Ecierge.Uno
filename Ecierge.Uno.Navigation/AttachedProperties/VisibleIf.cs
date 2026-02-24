namespace Ecierge.Uno.Navigation;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.UI.Xaml;

/// <summary>
/// Provides attached properties for conditionally showing UI elements based on user permissions.
/// </summary>
public static class VisibleIf
{
    /// <summary>
    /// Marker type used for logging category.
    /// </summary>
    private sealed class VisibleIfLogger;

    /// <summary>
    /// Identifies the HasPermissions attached dependency property.
    /// When set, the target element will be visible only if the current user has all specified permissions.
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

    private static int GetRequestVersion(DependencyObject d) =>
        (int)d.GetValue(RequestVersionProperty);

    private static void SetRequestVersion(DependencyObject d, int value) =>
        d.SetValue(RequestVersionProperty, value);

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

        _ = UpdateVisibilityAsync(element, version);
    }

    private static async Task UpdateVisibilityAsync(FrameworkElement element, int version)
    {
        var permissions = GetHasPermissions(element)?.ToArray();
        if (permissions is null || permissions.Length == 0)
        {
            if (GetRequestVersion(element) == version)
            {
                SetVisibility(element, Visibility.Visible);
            }

            return;
        }

        if (GetRequestVersion(element) == version)
        {
            SetVisibility(element, Visibility.Collapsed);
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

            SetVisibility(element, Visibility.Visible);
        }
        catch (Exception ex)
        {
            var logger = region.Scope.ServiceProvider.GetService<ILogger<VisibleIfLogger>>();
            logger?.LogError(
                ex,
                "Failed to evaluate permissions for element {ElementType}. Element will remain collapsed.",
                element.GetType());
        }
    }

    private static void SetVisibility(FrameworkElement element, Visibility visibility)
    {
        element.DispatcherQueue.TryEnqueue(() => element.Visibility = visibility);
    }
}
