namespace Ecierge.Uno.Navigation.Navigators;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Ecierge.Uno.Navigation;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

using RouteProperties = Ecierge.Uno.Navigation.Route;

/// <summary>
/// Selects the item of a <see cref="NavigationView"/> whose route segment name is the name of the segment.
/// </summary>
public class ByNameNavigationViewItemSelector : ItemSelector<NavigationView>
{
    /// <inheritdoc/>
    public override NavigationResult SelectItem(NavigationView target, Navigator navigator, NavigationRequest request)
    {
        // TODO: Make lazy
        List<NavigationViewItem> allContainers = new();
        void AddItems(object item)
        {
            if (target.ContainerFromMenuItem(item) is not NavigationViewItem container) return;
            allContainers.Add(container);
            foreach (var childItem in container.MenuItems)
            {
                AddItems(childItem);
            }
        }

        foreach (object menuItem in target.MenuItems)
        {
            AddItems(menuItem);
        }
        foreach (object footerMenuItem in target.FooterMenuItems)
        {
            AddItems(footerMenuItem);
        }

        NameSegment segment = request.NameSegment;
        var segmentName = segment.Name;
        var containerToSelect = allContainers.FirstOrDefault(c => RouteProperties.GetSegmentName(c) == segmentName);
        if (containerToSelect != default)
        {
            var itemToSelect = target.MenuItemFromContainer(containerToSelect);
            target.SelectedItem = itemToSelect;
            return new NavigationResult(request);
        }
        else
        {
            target.SelectedItem = null;
            GetLogger(navigator).LogWarning("No NavigationViewItem item found with name or text '{segmentName}'", segmentName);
            return new NavigationResult($"No NavigationViewItem item found with name or text '{segmentName}'");
        }
    }
}

/// <summary>
/// Navigates to the segment of the item that the user selects in a <see cref="NavigationView"/>, and selects the
/// item of the segment that is navigated to.
/// </summary>
public class NavigationViewNavigator : SelectorNavigator<NavigationView>
{
    /// <summary>Name of the segment that was navigated to.</summary>
    protected string navigatedName = string.Empty;

    /// <summary>Item that was navigated to. Used to prevent navigation on an item that is already selected.</summary>
    protected object? navigatedItem = null;

    /// <inheritdoc/>
    protected override string SelectedName => navigatedName;

    private readonly ItemSelector<NavigationView> itemSelector;

    /// <summary>
    /// Creates a navigator.
    /// </summary>
    /// <param name="serviceProvider">The services of the scope of the region.</param>
    public NavigationViewNavigator(IServiceProvider serviceProvider) : base(serviceProvider)
    {
        Type itemSelectorType = NavigationRegion.GetItemSelectorType(Target) ?? typeof(ByNameNavigationViewItemSelector);
        itemSelector = (ItemSelector<NavigationView>)ServiceProvider.GetRequiredService(itemSelectorType);
        AttachBackButton();
        Target.SelectionChanged += async (s, e) =>
        {
            var selectedItem = Target.SelectedItem;
            if (selectedItem is null || selectedItem == navigatedItem) return;
            navigatedItem = selectedItem;
            if (Target.ContainerFromMenuItem(selectedItem) is not NavigationViewItem selectedContainer) return;
            var segmentName = RouteProperties.GetSegmentName(selectedContainer);
            if (segmentName is null) return;

            var segment = Region.Segment.Nested.FirstOrDefault(s => s.Name == segmentName);
            if (segment is null)
            {
                Logger.LogWarning("No segment found with name '{SegmentName}'", segmentName);
                return;
            }
            navigatedName = segmentName;
            navigatedItem = selectedItem;

            // A route of one segment, so that the selection is announced and recorded like any other navigation
            // and the segments below it keep what they show
            await this.NavigateRouteAsync(s, Routing.Route.Empty.Add(segment));
        };
    }

    /// <inheritdoc/>
    protected override NavigationResult SelectItem(NavigationRequest request)
    {
        var result = itemSelector.SelectItem(Target, this, request);
        if (!result.Success) return result;
        navigatedItem = Target.SelectedItem;
        navigatedName = request.NameSegment.Name;
        return result;
    }

    // The back button of the view goes back in the history of the window and is enabled while there is a page to go back to
    private void AttachBackButton()
    {
        var journal = Journal;
        var dispatcher = Dispatcher;
        // The journal lives as long as the window, and the view only as long as its shell, so the journal must not keep it
        var view = new WeakReference<NavigationView>(Target);
        Target.IsBackEnabled = journal.CanGoBack;

        void OnJournalChanged(object? sender, JournalChangedEventArgs e)
        {
            if (!view.TryGetTarget(out _))
            {
                journal.Changed -= OnJournalChanged;
                return;
            }

            dispatcher.TryEnqueue(NavigationDispatcherPriority.Normal, () =>
            {
                if (view.TryGetTarget(out var target)) target.IsBackEnabled = journal.CanGoBack;
            });
        }

        journal.Changed += OnJournalChanged;
        Target.BackRequested += async (_, _) =>
        {
            if (journal.CanGoBack) await this.GoBackAsync(Target);
        };
    }
}

/// <summary>
/// Shows the view of the segment in the content of a <see cref="NavigationView"/>, and selects the item of the
/// segment after the navigation has finished.
/// </summary>
public class NavigationViewContentNavigator : ContentControlNavigatorBase<NavigationView>
{
    /// <summary>Name of the segment that was navigated to.</summary>
    protected string navigatedName = string.Empty;

    /// <summary>Item that was navigated to.</summary>
    protected object? navigatedItem = null;

    /// <summary>Gets the name of the segment that was navigated to.</summary>
    protected string SelectedName => navigatedName;

    private readonly ItemSelector<NavigationView> itemSelector;

    /// <summary>
    /// Creates a navigator.
    /// </summary>
    /// <param name="serviceProvider">The services of the scope of the region.</param>
    public NavigationViewContentNavigator(IServiceProvider serviceProvider) : base(serviceProvider)
    {
        Type itemSelectorType = NavigationRegion.GetItemSelectorType(Target) ?? typeof(ByNameNavigationViewItemSelector);
        itemSelector = (ItemSelector<NavigationView>)ServiceProvider.GetRequiredService(itemSelectorType);
    }

#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously
    /// <inheritdoc/>
    protected override async ValueTask<NavigationResult> NavigateCoreAsync(NavigationRequest request)
    {
        if (navigatedName == request.NameSegment.Name) return new NavigationResult(request, isSkipped: true);

        var result = CreateView(request);
        if (!result.Success) return result;

        navigatedName = request.NameSegment.Name;
        navigatedItem = Target.SelectedItem;
        var view = result.Result;
        Target.Content = view;

        var navigationStatus = NavigationStatus;
        void NavigationFinished(object? sender, NavigationResponse e)
        {
            navigationStatus.NavigationFinished -= NavigationFinished;
            itemSelector.SelectItem(Target, this, request);
        }
        navigationStatus.NavigationFinished += NavigationFinished;

        return new NavigationResult(request, view);
    }
#pragma warning restore CS1998 // Async method lacks 'await' operators and will run synchronously
}
