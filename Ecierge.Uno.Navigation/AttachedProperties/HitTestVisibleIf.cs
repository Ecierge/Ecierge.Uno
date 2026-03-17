namespace Ecierge.Uno.Navigation;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.UI.Xaml;

/// <summary>
/// Provides attached properties for conditionally enabling hit testing based on user permissions.
/// </summary>
public abstract class HitTestVisibleIf
{
    /// <summary>
    /// Identifies the HasPermissions attached dependency property.
    /// When set, the target element is hit-test visible only if the current user has at least one of the specified permissions.
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

    private static int GetRequestVersion(DependencyObject d) =>
        (int)d.GetValue(RequestVersionProperty);

    private static void SetRequestVersion(DependencyObject d, int value) =>
        d.SetValue(RequestVersionProperty, value);

    private static void OnHasPermissionsChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is FrameworkElement element)
        {
            element.Loaded += OnElementLoaded;
            TriggerUpdate(element);
        }
    }

    private static void OnElementLoaded(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement element)
        {
            element.Loaded -= OnElementLoaded;
            TriggerUpdate(element);
        }
    }

    private static void TriggerUpdate(FrameworkElement element)
    {
        var version = GetRequestVersion(element) + 1;
        SetRequestVersion(element, version);

        _ = UpdateHitTestVisibilityAsync(element, version);
    }

    private static async Task UpdateHitTestVisibilityAsync(FrameworkElement element, int version)
    {
        var permissions = GetHasPermissions(element)?.ToArray();
        if (permissions is null || permissions.Length == 0)
        {
            if (GetRequestVersion(element) == version)
            {
                SetIsHitTestVisible(element, true);
            }

            return;
        }

        if (GetRequestVersion(element) == version)
        {
            SetIsHitTestVisible(element, false);
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

            var hasAnyPermission = false;

            foreach (var checker in ruleCheckers)
            {
                foreach (var permission in permissions)
                {
                    if (await checker.HasPermissionAsync(permission))
                    {
                        hasAnyPermission = true;
                        break;
                    }
                }

                if (hasAnyPermission)
                {
                    break;
                }
            }

            if (!hasAnyPermission)
            {
                return;
            }

            if (GetRequestVersion(element) != version)
            {
                return;
            }

            SetIsHitTestVisible(element, true);
        }
        catch (Exception ex)
        {
            var loggerFactory = region.Scope.ServiceProvider.GetService<ILoggerFactory>();
            var logger = loggerFactory?.CreateLogger(typeof(HitTestVisibleIf).FullName!);

            logger?.LogError(
                ex,
                "Failed to evaluate permissions for element {ElementType}. Element will remain hit test disabled.",
                element.GetType());
        }
    }

    private static void SetIsHitTestVisible(FrameworkElement element, bool value)
    {
        var dispatcher = element.DispatcherQueue;
        if (dispatcher == null)
        {
            return;
        }

        dispatcher.TryEnqueue(() =>
        {
            element.IsHitTestVisible = value;
        });
    }
}
