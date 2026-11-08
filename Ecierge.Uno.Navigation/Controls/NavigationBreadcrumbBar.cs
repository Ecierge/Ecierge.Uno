namespace Ecierge.Uno.Navigation.Controls;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Ecierge.Uno.Navigation.Routing;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;

/// <summary>
/// Shows the route of the window as breadcrumbs in a <see cref="BreadcrumbBar"/>, from the root down to the page,
/// and opens the level of the breadcrumb that the user clicks.
/// </summary>
/// <remarks>
/// <list type="bullet">
/// <item><description>
/// A named segment is a breadcrumb with the title of the segment (<see cref="NameSegment.Title"/>), else with its
/// name. A title is first looked up as the key of a resource of the <see cref="IStringLocalizer"/> of the application.
/// </description></item>
/// <item><description>
/// A segment that owns a data segment is one breadcrumb together with its value. It shows the name of the entity
/// that the data map gives (<see cref="NavigationDataMap{TEntity}.GetDisplayName(TEntity)"/>), and the title of
/// the segment until the entity has loaded.
/// </description></item>
/// <item><description>A dialog is not a breadcrumb: the breadcrumbs stand for the page under it.</description></item>
/// <item><description>
/// The bar collapses while the route passes through a segment whose <see cref="NameSegment.HidesBreadcrumbs"/> is
/// set, and while there is no route yet. Space around the breadcrumbs is given with <see cref="Control.Padding"/>,
/// which collapses with them.
/// </description></item>
/// <item><description>
/// A click opens the level of the breadcrumb with its default pages and adds an entry to the history. A click on the
/// last breadcrumb, or on a level whose default page is the page that is shown, asks the page to load its data
/// again (<see cref="IRefreshable"/>).
/// </description></item>
/// </list>
/// <para>
/// The bar may be anywhere in the window, also outside the navigation regions, for example in the shell: it follows
/// the root navigator of the window.
/// </para>
/// </remarks>
[TemplatePart(Name = BreadcrumbBarPartName, Type = typeof(BreadcrumbBar))]
[TemplateVisualState(GroupName = VisibilityStatesGroupName, Name = ShownStateName)]
[TemplateVisualState(GroupName = VisibilityStatesGroupName, Name = HiddenStateName)]
public sealed partial class NavigationBreadcrumbBar : Control
{
    private const string BreadcrumbBarPartName = "PART_BreadcrumbBar";
    private const string VisibilityStatesGroupName = "VisibilityStates";
    private const string ShownStateName = "Shown";
    private const string HiddenStateName = "Hidden";

    // The handles of the entities of the route, with the load whose end shows the name of the entity
    private readonly Dictionary<INavigationEntity, Task?> watched = new();
    private BreadcrumbBar? breadcrumbBar;
    private BreadcrumbTrail trail = BreadcrumbTrail.Empty;
    private Navigator? root;
    private IRouteNotifier? notifier;
    private IStringLocalizer? localizer;
    private BreadcrumbNames? names;
    private ILogger? logger;
    private DependencyObject? rootNavigatorHolder;
    private long rootNavigatorCallback;

    /// <summary>
    /// Creates a breadcrumb bar.
    /// </summary>
    public NavigationBreadcrumbBar()
    {
        DefaultStyleKey = typeof(NavigationBreadcrumbBar);
        Loaded += OnLoaded;
        Unloaded += OnUnloaded;
        // The bar does not let the user click its last item, which stands for the current page; a tap on it is
        // caught here, also when an element below has handled it
        AddHandler(TappedEvent, new TappedEventHandler(OnTapped), handledEventsToo: true);
    }

    /// <inheritdoc/>
    protected override void OnApplyTemplate()
    {
        if (breadcrumbBar is not null) breadcrumbBar.ItemClicked -= OnItemClicked;

        base.OnApplyTemplate();

        breadcrumbBar = GetTemplateChild(BreadcrumbBarPartName) as BreadcrumbBar;
        if (breadcrumbBar is not null) breadcrumbBar.ItemClicked += OnItemClicked;
        Show(trail, replaceItems: true);
    }

    private void OnLoaded(object sender, RoutedEventArgs e) => Attach();

    private void OnUnloaded(object sender, RoutedEventArgs e) => Detach();

    private void Attach()
    {
        Detach();
        if (FindRootNavigator() is { } navigator)
        {
            Follow(navigator);
            return;
        }

        // The window gets its root navigator after its content is shown, so the bar of the shell waits for it
        if (XamlRoot?.Content is { } content)
        {
            rootNavigatorHolder = content;
            rootNavigatorCallback = content.RegisterPropertyChangedCallback(Navigation.RootNavigatorProperty, OnRootNavigatorChanged);
        }
    }

    private void OnRootNavigatorChanged(DependencyObject sender, DependencyProperty property)
    {
        if (sender.GetValue(property) is not Navigator navigator) return;

        // The callback runs inside the call that sets the root navigator, before the first page is opened: the bar
        // must neither unregister the callback while it is being raised nor delay the first navigation
        DispatcherQueue.TryEnqueue(() =>
        {
            if (root is not null || rootNavigatorHolder is null) return;

            StopWaitingForRootNavigator();
            Follow(navigator);
        });
    }

    /// <summary>
    /// Finds the root navigator of the window: on the content of the window, which is an ancestor of the bar, or
    /// through the region that the bar is in.
    /// </summary>
    private Navigator? FindRootNavigator()
    {
        for (DependencyObject? current = this; current is not null; current = VisualTreeHelper.GetParent(current))
        {
            if (current.GetValue(Navigation.RootNavigatorProperty) is Navigator navigator) return navigator;
            if (current is FrameworkElement element && element.GetNavigationRegion() is { } region) return region.RootRegion.Navigator;
        }

        return XamlRoot?.Content?.GetValue(Navigation.RootNavigatorProperty) as Navigator;
    }

    private void Follow(Navigator navigator)
    {
        root = navigator;
        var services = navigator.ServiceProvider;
        logger = services.GetService<ILoggerFactory>()?.CreateLogger<NavigationBreadcrumbBar>();
        localizer = services.GetService<IStringLocalizer>();
        names = new BreadcrumbNames(services);
        notifier = services.GetService<IRouteNotifier>();
        if (notifier is not null) notifier.RouteChanged += OnRouteChanged;
        Rebuild();
    }

    private void Detach()
    {
        StopWaitingForRootNavigator();
        if (notifier is not null) notifier.RouteChanged -= OnRouteChanged;
        notifier = null;
        foreach (var handle in watched.Keys)
        {
            handle.Changed -= OnEntityChanged;
        }

        watched.Clear();
        root = null;
    }

    private void StopWaitingForRootNavigator()
    {
        rootNavigatorHolder?.UnregisterPropertyChangedCallback(Navigation.RootNavigatorProperty, rootNavigatorCallback);
        rootNavigatorHolder = null;
    }

    private void OnRouteChanged(object? sender, RouteChangedEventArgs e)
    {
        // The notifier serves all the windows of the application
        if (root is null || !ReferenceEquals(e.Region, root.Region)) return;

        if (DispatcherQueue.HasThreadAccess) Rebuild();
        else DispatcherQueue.TryEnqueue(Rebuild);
    }

    // A new load of an entity has started; it may give the entity another name
    private void OnEntityChanged(object? sender, EventArgs e) => DispatcherQueue.TryEnqueue(Rebuild);

    private void Rebuild()
    {
        if (root is null || names is null)
        {
            Show(BreadcrumbTrail.Empty);
            return;
        }

#pragma warning disable CA1031 // Do not catch general exceptions: a localizer or a data map that fails must not break the window
        try
        {
            var route = root.ActualRoute.WithoutDialog();
            var built = BreadcrumbTrail.Build(
                route,
                segment => BreadcrumbTrail.TitleOf(segment, localizer),
                step => names.DisplayNameOf(step, route.Data));
            Watch(route);
            Show(built);
        }
        catch (Exception ex)
        {
            logger?.LogError(ex, "The breadcrumbs of the route {Route} could not be built", root.ActualRoute.ToPath());
        }
#pragma warning restore CA1031 // Do not catch general exceptions
    }

    /// <summary>
    /// Follows the entities of the route: a breadcrumb shows the name of its entity once the entity has loaded, and
    /// the new name after the entity was loaded again.
    /// </summary>
    private void Watch(Routing.Route route)
    {
        var handles = route.Segments
            .OfType<DataSegmentInstance>()
            .Select(step => route.Data.TryGetValue(step.DataSegment.Name, out var value) ? value as INavigationEntity : null)
            .OfType<INavigationEntity>()
            .ToHashSet();

        foreach (var gone in watched.Keys.Where(handle => !handles.Contains(handle)).ToList())
        {
            gone.Changed -= OnEntityChanged;
            watched.Remove(gone);
        }

        foreach (var handle in handles)
        {
            if (!watched.TryGetValue(handle, out var observed))
            {
                handle.Changed += OnEntityChanged;
                watched[handle] = observed = null;
            }

            if (handle.IsLoaded) continue;

            // The page of the entity loads it anyway; the bar waits for the same load
            var load = handle.LoadTask;
            if (ReferenceEquals(observed, load)) continue;

            watched[handle] = load;
            _ = load.ContinueWith(
                _ => DispatcherQueue.TryEnqueue(Rebuild),
                CancellationToken.None,
                TaskContinuationOptions.ExecuteSynchronously,
                TaskScheduler.Default);
        }
    }

    private void Show(BreadcrumbTrail next, bool replaceItems = false)
    {
        // The same breadcrumbs keep their items, so that the bar does not flicker; the routes are taken from the trail
        replaceItems |= !HaveSameItems(trail, next);
        trail = next;
        if (breadcrumbBar is not null && replaceItems)
        {
            breadcrumbBar.ItemsSource = next.Items.ToList();
        }

        VisualStateManager.GoToState(this, next.IsShown ? ShownStateName : HiddenStateName, useTransitions: false);
    }

    private static bool HaveSameItems(BreadcrumbTrail current, BreadcrumbTrail next)
     => current.Items.Length == next.Items.Length
        && current.Items.Zip(next.Items).All(pair =>
            pair.First.Title == pair.Second.Title
            && pair.First.IsCurrent == pair.Second.IsCurrent
            && pair.First.Route.ToPath() == pair.Second.Route.ToPath());

    /// <summary>
    /// Asks the page to load its data again when the user taps the breadcrumb of the current page, which the bar shows
    /// as text rather than as a button.
    /// </summary>
    private void OnTapped(object sender, TappedRoutedEventArgs e)
    {
        if (root is not { } navigator || breadcrumbBar is null) return;

        // A breadcrumb that the bar shows as a button reports its click through ItemClicked
        for (var source = e.OriginalSource as DependencyObject; source is not null && !ReferenceEquals(source, this); source = VisualTreeHelper.GetParent(source))
        {
            if (source is ButtonBase { IsEnabled: true }) return;
        }

        if (FindCurrentItem(breadcrumbBar) is not { } item) return;

        var position = e.GetPosition(item);
        if (position.X < 0 || position.Y < 0 || position.X > item.ActualWidth || position.Y > item.ActualHeight) return;

        e.Handled = true;
#pragma warning disable CA1031 // Do not catch general exceptions: a tap must not break the window
        try
        {
            navigator.TryRefreshPage();
        }
        catch (Exception ex)
        {
            logger?.LogError(ex, "The page could not be loaded again");
        }
#pragma warning restore CA1031 // Do not catch general exceptions
    }

    private static BreadcrumbBarItem? FindCurrentItem(DependencyObject parent)
    {
        var count = VisualTreeHelper.GetChildrenCount(parent);
        for (var index = 0; index < count; index++)
        {
            var child = VisualTreeHelper.GetChild(parent, index);
            if (child is BreadcrumbBarItem item
                && (item.Content as NavigationBreadcrumb ?? item.DataContext as NavigationBreadcrumb) is { IsCurrent: true })
            {
                return item;
            }

            if (FindCurrentItem(child) is { } found) return found;
        }

        return null;
    }

    private async void OnItemClicked(BreadcrumbBar sender, BreadcrumbBarItemClickedEventArgs args)
    {
        if (root is not { } navigator) return;

        // The trail holds the routes of the last build, which the items that were kept may not have
        var breadcrumb = args.Index >= 0 && args.Index < trail.Items.Length
            ? trail.Items[args.Index]
            : args.Item as NavigationBreadcrumb;
        if (breadcrumb is null) return;

#pragma warning disable CA1031 // Do not catch general exceptions: a click must not break the window
        try
        {
            await BreadcrumbTrail.OpenAsync(navigator, breadcrumb, this).ConfigureAwait(true);
        }
        catch (Exception ex)
        {
            logger?.LogError(ex, "The breadcrumb {Title} could not be opened", breadcrumb.Title);
        }
#pragma warning restore CA1031 // Do not catch general exceptions
    }
}
