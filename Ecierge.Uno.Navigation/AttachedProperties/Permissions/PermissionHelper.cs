namespace Ecierge.Uno.Navigation;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.UI.Xaml;

/// <summary>
/// Shared logic for permission-based attached properties that evaluate <see cref="IAuthorizationService"/>
/// rules and apply a UI state change on the dispatcher queue.
/// </summary>
internal static class PermissionHelper
{
    /// <summary>
    /// Handles the common pattern for permission property changes:
    /// subscribes to <see cref="FrameworkElement.Loaded"/> and triggers the initial permission evaluation.
    /// </summary>
    /// <param name="d">The dependency object whose permission property changed.</param>
    /// <param name="hasPermissionsProperty">The <c>HasPermissions</c> attached property.</param>
    /// <param name="requestVersionProperty">The <c>RequestVersion</c> attached property used to prevent stale async results.</param>
    /// <param name="applyState">
    /// Callback to apply the resolved state on the UI thread.
    /// <c>true</c> means permissions were granted; <c>false</c> means denied.
    /// </param>
    /// <param name="ownerType">The type that owns the attached properties (used for logging).</param>
    /// <param name="failureLogMessage">The log message template used when permission evaluation fails.</param>
    internal static void OnPermissionsPropertyChanged(
        DependencyObject d,
        DependencyProperty hasPermissionsProperty,
        DependencyProperty requestVersionProperty,
        Action<FrameworkElement, bool> applyState,
        Type ownerType,
        string failureLogMessage)
    {
        if (d is not FrameworkElement element) return;

        element.Loaded += OnElementLoaded;
        TriggerUpdate(element);
        return;

        void OnElementLoaded(object sender, RoutedEventArgs e)
        {
            if (sender is FrameworkElement el)
            {
                el.Loaded -= OnElementLoaded;
                TriggerUpdate(el);
            }
        }

        void TriggerUpdate(FrameworkElement el)
        {
            var version = (int)el.GetValue(requestVersionProperty) + 1;
            el.SetValue(requestVersionProperty, version);
            _ = EvaluateAndApplyAsync(el, version, hasPermissionsProperty, requestVersionProperty, applyState, ownerType, failureLogMessage);
        }
    }

    /// <summary>
    /// Evaluates authorization permissions for the specified element and applies state changes based on the result.
    /// </summary>
    /// <remarks>If no permissions are specified or no authorization services are available, the state may be
    /// applied without further checks. Exceptions encountered during evaluation are logged using the provided owner
    /// type and log message.</remarks>
    /// <param name="element">The framework element whose permissions are evaluated and whose state may be updated.</param>
    /// <param name="version">The version number used to determine whether the state should be applied for the current request.</param>
    /// <param name="hasPermissionsProperty">The dependency property containing the collection of permissions to check for the element.</param>
    /// <param name="requestVersionProperty">The dependency property representing the current request version for the element.</param>
    /// <param name="applyState">An action that applies the state to the element based on whether authorization is granted.</param>
    /// <param name="ownerType">The type of the owner used for logging purposes in case of errors.</param>
    /// <param name="failureLogMessage">The log message to use when logging authorization failures or exceptions.</param>
    /// <returns>A task that represents the asynchronous operation. The task completes when the evaluation and state application
    /// are finished.</returns>
    private static async Task EvaluateAndApplyAsync(
        FrameworkElement element,
        int version,
        DependencyProperty hasPermissionsProperty,
        DependencyProperty requestVersionProperty,
        Action<FrameworkElement, bool> applyState,
        Type ownerType,
        string failureLogMessage)
    {
        var permissions = ((IEnumerable<string>?)element.GetValue(hasPermissionsProperty))?.ToArray();
        if (permissions is null || permissions.Length == 0)
        {
            if ((int)element.GetValue(requestVersionProperty) == version)
            {
                applyState(element, true);
            }

            return;
        }

        if ((int)element.GetValue(requestVersionProperty) == version)
        {
            applyState(element, false);
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

            if ((int)element.GetValue(requestVersionProperty) != version)
            {
                return;
            }

            applyState(element, true);
        }
        catch (Exception ex)
        {
            var loggerFactory = region.Scope.ServiceProvider.GetService<ILoggerFactory>();
            var logger = loggerFactory?.CreateLogger(ownerType.FullName!);

            logger?.LogError(
                ex,
                failureLogMessage,
                element.GetType());
        }
    }
}
