namespace Ecierge.Uno.Navigation.Controls;

using System;
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
/// Uses INavigationRuleChecker to determine if the current user has access.
/// </summary>
[ContentProperty(Name = nameof(Content))]
public sealed class AuthorizedContent : Control
{
    private NavigationRegion? _cachedRegion;
    private ILogger<AuthorizedContent>? _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="AuthorizedContent"/> class.
    /// </summary>
    public AuthorizedContent()
    {
        DefaultStyleKey = typeof(AuthorizedContent);
        Loaded += OnLoaded;
        Unloaded += OnUnloaded;
    }

    #region Content Dependency Property

    /// <summary>
    /// Identifies the Content dependency property.
    /// </summary>
    public static readonly DependencyProperty ContentProperty =
        DependencyProperty.Register(
            nameof(Content),
            typeof(object),
            typeof(AuthorizedContent),
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

    /// <summary>
    /// Identifies the ContentTemplate dependency property.
    /// </summary>
    public static readonly DependencyProperty ContentTemplateProperty =
        DependencyProperty.Register(
            nameof(ContentTemplate),
            typeof(DataTemplate),
            typeof(AuthorizedContent),
            new PropertyMetadata(null));

    /// <summary>
    /// Gets or sets the template for authorized content.
    /// </summary>
    public DataTemplate? ContentTemplate
    {
        get => (DataTemplate?)GetValue(ContentTemplateProperty);
        set => SetValue(ContentTemplateProperty, value);
    }

    #endregion

    #region UnauthorizedContent Dependency Property

    /// <summary>
    /// Identifies the UnauthorizedContent dependency property.
    /// </summary>
    public static readonly DependencyProperty UnauthorizedContentProperty =
        DependencyProperty.Register(
            nameof(UnauthorizedContent),
            typeof(object),
            typeof(AuthorizedContent),
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

    /// <summary>
    /// Identifies the UnauthorizedContentTemplate dependency property.
    /// </summary>
    public static readonly DependencyProperty UnauthorizedContentTemplateProperty =
        DependencyProperty.Register(
            nameof(UnauthorizedContentTemplate),
            typeof(DataTemplate),
            typeof(AuthorizedContent),
            new PropertyMetadata(null));

    /// <summary>
    /// Gets or sets the template for unauthorized content.
    /// </summary>
    public DataTemplate? UnauthorizedContentTemplate
    {
        get => (DataTemplate?)GetValue(UnauthorizedContentTemplateProperty);
        set => SetValue(UnauthorizedContentTemplateProperty, value);
    }

    #endregion

    #region UnauthenticatedContent Dependency Property

    /// <summary>
    /// Identifies the UnauthenticatedContent dependency property.
    /// </summary>
    public static readonly DependencyProperty UnauthenticatedContentProperty =
        DependencyProperty.Register(
            nameof(UnauthenticatedContent),
            typeof(object),
            typeof(AuthorizedContent),
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

    /// <summary>
    /// Identifies the UnauthenticatedContentTemplate dependency property.
    /// </summary>
    public static readonly DependencyProperty UnauthenticatedContentTemplateProperty =
        DependencyProperty.Register(
            nameof(UnauthenticatedContentTemplate),
            typeof(DataTemplate),
            typeof(AuthorizedContent),
            new PropertyMetadata(null));

    /// <summary>
    /// Gets or sets the template for unauthenticated content.
    /// </summary>
    public DataTemplate? UnauthenticatedContentTemplate
    {
        get => (DataTemplate?)GetValue(UnauthenticatedContentTemplateProperty);
        set => SetValue(UnauthenticatedContentTemplateProperty, value);
    }

    #endregion

    #region RequiredRoute Dependency Property

    /// <summary>
    /// Identifies the RequiredRoute dependency property.
    /// </summary>
    public static readonly DependencyProperty RequiredRouteProperty =
        DependencyProperty.Register(
            nameof(RequiredRoute),
            typeof(string),
            typeof(AuthorizedContent),
            new PropertyMetadata(string.Empty, OnRequiredRouteChanged));

    /// <summary>
    /// Gets or sets the route path to check permissions against (e.g., "Administration/Staff").
    /// </summary>
    public string RequiredRoute
    {
        get => (string)GetValue(RequiredRouteProperty);
        set => SetValue(RequiredRouteProperty, value);
    }

    private static void OnRequiredRouteChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is AuthorizedContent control && control.IsLoaded)
        {
            _ = control.PerformAuthorizationCheckAsync();
        }
    }

    #endregion

    #region AuthorizationStatus Dependency Property

    /// <summary>
    /// Identifies the AuthorizationStatus dependency property.
    /// </summary>
    public static readonly DependencyProperty AuthorizationStatusProperty =
        DependencyProperty.Register(
            nameof(AuthorizationStatus),
            typeof(AuthorizationStatus),
            typeof(AuthorizedContent),
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
        _ = PerformAuthorizationCheckAsync();
    }

    private void OnUnloaded(object sender, RoutedEventArgs e)
    {
        _cachedRegion = null;
    }

    /// <summary>
    /// Performs the authorization check asynchronously.
    /// </summary>
    private async Task PerformAuthorizationCheckAsync()
    {
        try
        {
            var result = await CheckAuthorizationAsync();
            var status = MapResultToStatus(result);
            
            // Update on UI thread
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

    /// <summary>
    /// Checks authorization by walking up the visual tree to find a NavigationRegion
    /// and using its Navigator to check permissions.
    /// </summary>
    private async ValueTask<NavigationRuleResult> CheckAuthorizationAsync()
    {
        var region = FindNavigationRegion();
        if (region is null)
        {
            _logger?.LogWarning("No NavigationRegion found for AuthorizedContent");
            return NavigationRuleResult.Deny("No NavigationRegion found");
        }

        var navigator = region.Navigator;
        
        // Parse the RequiredRoute string into a Route object
        Routing.Route route;
        try
        {
            route = string.IsNullOrEmpty(RequiredRoute) 
                ? navigator.Route // Use current route
                : navigator.ParseRoute(RequiredRoute);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to parse route: {Route}", RequiredRoute);
            return NavigationRuleResult.Deny($"Failed to parse route: {ex.Message}");
        }
        
        // Use existing Navigator.IsAllowedToNavigateAsync which calls all INavigationRuleChecker services
        return await navigator.IsAllowedToNavigateAsync(route);
    }

    /// <summary>
    /// Finds the NavigationRegion by walking up the visual tree.
    /// </summary>
    private NavigationRegion? FindNavigationRegion()
    {
        // Return cached region if available
        if (_cachedRegion is not null)
            return _cachedRegion;

        DependencyObject? current = this;
        while (current is not null)
        {
            // Use the existing attached property getter
            if (current is FrameworkElement element)
            {
                var region = Navigation.GetNavigationRegion(element);
                if (region is not null)
                {
                    _cachedRegion = region;
                    
                    // Try to get logger from region's service provider
                    if (_logger is null)
                    {
                        _logger = region.Navigator.ServiceProvider.GetService<ILogger<AuthorizedContent>>();
                    }
                    
                    return region;
                }
            }
            current = Microsoft.UI.Xaml.Media.VisualTreeHelper.GetParent(current);
        }
        return null;
    }

    /// <summary>
    /// Maps NavigationRuleResult to AuthorizationStatus.
    /// </summary>
    private AuthorizationStatus MapResultToStatus(NavigationRuleResult result)
    {
        if (result.IsAllowed)
            return AuthorizationStatus.Authorized;
        
        // Check if denial reason indicates unauthenticated user
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
