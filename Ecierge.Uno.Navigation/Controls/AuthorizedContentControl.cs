namespace Ecierge.Uno.Navigation.Controls;

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;
using Ecierge.Uno.Navigation.Regions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Markup;

/// <summary>
/// A content control that displays different content based on authorization status.
/// Checks user permissions to determine if the current user has access.
/// </summary>
[ContentProperty(Name = nameof(Content))]
public sealed class AuthorizedContentControl : Control
{
    private NavigationRegion? _cachedRegion;
    private ILogger<AuthorizedContentControl>? _logger;

    public AuthorizedContentControl()
    {
        DefaultStyleKey = typeof(AuthorizedContentControl);
        Loaded += OnLoaded;
        Unloaded += OnUnloaded;
    }

    #region Content Dependency Property

    public static readonly DependencyProperty ContentProperty =
        DependencyProperty.Register(
            nameof(Content),
            typeof(object),
            typeof(AuthorizedContentControl),
            new PropertyMetadata(null));

    /// <summary>
    /// Gets or sets the content to display when authorized.
    /// </summary>
    public object? Content
    {
        get => GetValue(ContentProperty);
        set => SetValue(ContentProperty, value);
    }

    #endregion

    #region ContentTemplate Dependency Property

    public static readonly DependencyProperty ContentTemplateProperty =
        DependencyProperty.Register(
            nameof(ContentTemplate),
            typeof(DataTemplate),
            typeof(AuthorizedContentControl),
            new PropertyMetadata(null));

    public DataTemplate? ContentTemplate
    {
        get => (DataTemplate?)GetValue(ContentTemplateProperty);
        set => SetValue(ContentTemplateProperty, value);
    }

    #endregion

    #region UnauthorizedContent Dependency Property

    public static readonly DependencyProperty UnauthorizedContentProperty =
        DependencyProperty.Register(
            nameof(UnauthorizedContent),
            typeof(object),
            typeof(AuthorizedContentControl),
            new PropertyMetadata(null));

    /// <summary>
    /// Gets or sets the content to display when not authorized.
    /// </summary>
    public object? UnauthorizedContent
    {
        get => GetValue(UnauthorizedContentProperty);
        set => SetValue(UnauthorizedContentProperty, value);
    }

    #endregion

    #region UnauthorizedContentTemplate Dependency Property

    public static readonly DependencyProperty UnauthorizedContentTemplateProperty =
        DependencyProperty.Register(
            nameof(UnauthorizedContentTemplate),
            typeof(DataTemplate),
            typeof(AuthorizedContentControl),
            new PropertyMetadata(null));

    public DataTemplate? UnauthorizedContentTemplate
    {
        get => (DataTemplate?)GetValue(UnauthorizedContentTemplateProperty);
        set => SetValue(UnauthorizedContentTemplateProperty, value);
    }

    #endregion

    #region UnauthenticatedContent Dependency Property

    public static readonly DependencyProperty UnauthenticatedContentProperty =
        DependencyProperty.Register(
            nameof(UnauthenticatedContent),
            typeof(object),
            typeof(AuthorizedContentControl),
            new PropertyMetadata(null));

    /// <summary>
    /// Gets or sets the content to display when not authenticated.
    /// </summary>
    public object? UnauthenticatedContent
    {
        get => GetValue(UnauthenticatedContentProperty);
        set => SetValue(UnauthenticatedContentProperty, value);
    }

    #endregion

    #region UnauthenticatedContentTemplate Dependency Property

    public static readonly DependencyProperty UnauthenticatedContentTemplateProperty =
        DependencyProperty.Register(
            nameof(UnauthenticatedContentTemplate),
            typeof(DataTemplate),
            typeof(AuthorizedContentControl),
            new PropertyMetadata(null));

    public DataTemplate? UnauthenticatedContentTemplate
    {
        get => (DataTemplate?)GetValue(UnauthenticatedContentTemplateProperty);
        set => SetValue(UnauthenticatedContentTemplateProperty, value);
    }

    #endregion

    #region Permissions Dependency Property

    public static readonly DependencyProperty PermissionsProperty =
        DependencyProperty.Register(
            nameof(Permissions),
            typeof(IEnumerable<string>),
            typeof(AuthorizedContentControl),
            new PropertyMetadata(null, OnPermissionsChanged));

    /// <summary>
    /// Gets or sets the permissions required to access the content.
    /// </summary>
    public IEnumerable<string>? Permissions
    {
        get => (IEnumerable<string>?)GetValue(PermissionsProperty);
        set => SetValue(PermissionsProperty, value);
    }

    private static void OnPermissionsChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is AuthorizedContentControl control && control.IsLoaded)
        {
            _ = control.PerformAuthorizationCheckAsync().ConfigureAwait(false);
        }
    }

    #endregion

    #region AuthorizationStatus Dependency Property

    public static readonly DependencyProperty AuthorizationStatusProperty =
        DependencyProperty.Register(
            nameof(AuthorizationStatus),
            typeof(AuthorizationStatus),
            typeof(AuthorizedContentControl),
            new PropertyMetadata(AuthorizationStatus.Unknown));

    /// <summary>
    /// Gets the current authorization status (read-only).
    /// </summary>
    public AuthorizationStatus AuthorizationStatus
    {
        get => (AuthorizationStatus)GetValue(AuthorizationStatusProperty);
        private set => SetValue(AuthorizationStatusProperty, value);
    }

    #endregion

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        _ = PerformAuthorizationCheckAsync().ConfigureAwait(false);
    }

    private void OnUnloaded(object sender, RoutedEventArgs e)
    {
        _cachedRegion = null;
    }

    private async Task PerformAuthorizationCheckAsync()
    {
        try
        {
            var result = await CheckAuthorizationAsync();
            var status = MapResultToStatus(result);
            
            DispatcherQueue.TryEnqueue(() =>
            {
                AuthorizationStatus = status;
            });
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Authorization check failed for AuthorizedContent");
            DispatcherQueue.TryEnqueue(() =>
            {
                AuthorizationStatus = AuthorizationStatus.Error;
            });
        }
    }

    private async ValueTask<NavigationRuleResult> CheckAuthorizationAsync()
    {
        var region = FindNavigationRegion();
        if (region is null)
        {
            _logger?.LogWarning("No NavigationRegion found for AuthorizedContent");
            return NavigationRuleResult.Deny("No NavigationRegion found");
        }

        var navigator = region.Navigator;
        
        if (Permissions is not null && Permissions.Any())
        {
            return await CheckPermissionsDirectlyAsync(navigator);
        }
        
        _logger?.LogWarning("No Permissions specified for AuthorizedContent");
        return NavigationRuleResult.Deny("No authorization criteria specified");
    }

    private async ValueTask<NavigationRuleResult> CheckPermissionsDirectlyAsync(Navigator navigator)
    {
        try
        {
            var ruleCheckers = navigator.ServiceProvider.GetServices<IAuthorizationService>();
            if (!ruleCheckers.Any())
            {
                _logger?.LogWarning("No INavigationRuleChecker services registered");
                return NavigationRuleResult.Deny("Permission checker not configured");
            }

            var permissionList = Permissions!.ToArray();
            
            // Create a temporary route with these permissions to reuse existing logic
            var tempSegment = new NameSegment(
                name: "AuthorizedContent",
                view: null,
                isDefault: false,
                permissions: ImmutableArray.Create(permissionList),
                nested: ImmutableArray<NameSegment>.Empty
            );
            
            var tempRoute = new Routing.Route(
                ImmutableArray.Create<Routing.RouteSegmentInstance>(
                    new Routing.NameSegmentInstance(tempSegment)
                ),
                data: navigator.ActualRoute.Data
            );

            // Use existing navigation rule checkers
            foreach (var checker in ruleCheckers)
            {
                var result = await checker.CanNavigateAsync(tempRoute);
                if (!result.IsAllowed)
                {
                    return result;
                }
            }

            return NavigationRuleResult.Allow();
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to check permissions");
            return NavigationRuleResult.Deny($"Permission check failed: {ex.Message}");
        }
    }

    private NavigationRegion? FindNavigationRegion()
    {
        if (_cachedRegion is not null)
            return _cachedRegion;

        DependencyObject? current = this;
        while (current is not null)
        {
            if (current is FrameworkElement element)
            {
                var region = Navigation.GetNavigationRegion(element);
                if (region is not null)
                {
                    _cachedRegion = region;
                    
                    if (_logger is null)
                    {
                        _logger = region.Navigator.ServiceProvider.GetService<ILogger<AuthorizedContentControl>>();
                    }
                    
                    return region;
                }
            }
            current = Microsoft.UI.Xaml.Media.VisualTreeHelper.GetParent(current);
        }
        return null;
    }

    private AuthorizationStatus MapResultToStatus(NavigationRuleResult result)
    {
        if (result.IsAllowed)
            return AuthorizationStatus.Authorized;
        
        if (result.Reasons.Any(r => r.Contains("No user", StringComparison.OrdinalIgnoreCase)))
            return AuthorizationStatus.Unauthenticated;
        
        return AuthorizationStatus.Unauthorized;
    }
}

/// <summary>
/// Represents the authorization status of the content.
/// </summary>
public enum AuthorizationStatus
{
    /// <summary>Authorization check not yet performed.</summary>
    Unknown,
    /// <summary>User is authorized to view content.</summary>
    Authorized,
    /// <summary>User is not authenticated.</summary>
    Unauthenticated,
    /// <summary>User is authenticated but lacks required permissions.</summary>
    Unauthorized,
    /// <summary>Authorization check failed (e.g., no NavigationRegion found).</summary>
    Error
}
