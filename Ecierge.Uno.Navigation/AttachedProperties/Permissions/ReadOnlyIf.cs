namespace Ecierge.Uno.Navigation;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Ecierge.Uno.Navigation;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

/// <summary>
/// Provides attached properties for conditionally setting read-only state based on permissions.
/// </summary>
public abstract class ReadOnlyIf
{
    /// <summary>
    /// Identifies the HasPermissions attached dependency property.
    /// When set, the target element will be read-only if the current user does not have any of the specified permissions.
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

        _ = UpdateReadOnlyAsync(element, version);
    }

    private static async Task UpdateReadOnlyAsync(FrameworkElement element, int version)
    {
        var permissions = GetHasPermissions(element)?.ToArray();
        if (permissions is null || permissions.Length == 0)
        {
            if (GetRequestVersion(element) == version)
            {
                SetIsReadOnly(element, false);
            }

            return;
        }

        if (GetRequestVersion(element) == version)
        {
            SetIsReadOnly(element, true);
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

            SetIsReadOnly(element, false);
        }
        catch (Exception ex)
        {
            var loggerFactory = region.Scope.ServiceProvider.GetService<ILoggerFactory>();
            var logger = loggerFactory?.CreateLogger(typeof(ReadOnlyIf).FullName!);

            logger?.LogError(
                ex,
                "Failed to evaluate permissions for element {ElementType}. Element will remain read-only.",
                element.GetType());
        }
    }

    private static void SetIsReadOnly(FrameworkElement element, bool isReadOnly)
    {
        var dispatcher = element.DispatcherQueue;
        if (dispatcher == null)
        {
            return;
        }

        dispatcher.TryEnqueue(() =>
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
