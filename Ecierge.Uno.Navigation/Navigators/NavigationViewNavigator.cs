namespace Ecierge.Uno.Navigation.Navigators;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Ecierge.Uno.Navigation;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.UI.Dispatching;

using RouteProperties = Ecierge.Uno.Navigation.Route;

public class ByNameNavigationViewItemSelector : ItemSelector<NavigationView>
{
    public override NavigationResult SelectItem(NavigationRequest request)
    {
        // TODO: Make lazy
        List<NavigationViewItem> allContainers = new();
        void AddItems(object item)
        {
            var container = (NavigationViewItem)Target.ContainerFromMenuItem(item);
            if (container is null) return;
            allContainers.Add(container);
            foreach (var childItem in container.MenuItems)
            {
                AddItems(childItem);
            }
        }

        foreach (object menuItem in Target.MenuItems)
        {
            AddItems(menuItem);
        }
        foreach (object footerMenuItem in Target.FooterMenuItems)
        {
            AddItems(footerMenuItem);
        }

        //var menuItemContainers = Target.MenuItems.Select(i => (NavigationViewItem)Target.ContainerFromMenuItem(i));
        //var footerItemsContainers = Target.FooterMenuItems.Select(i => (NavigationViewItem)Target.ContainerFromMenuItem(i));
        //var allContainers = menuItemContainers.Concat(footerItemsContainers);
        NameSegment segment = request.NameSegment;
        var segmentName = segment.Name;
        var containerToSelect = allContainers.FirstOrDefault(c => RouteProperties.GetSegmentName(c) == segmentName);
        if (containerToSelect != default)
        {
            var itemToSelect = Target.MenuItemFromContainer(containerToSelect);
            Target.SelectedItem = itemToSelect;
            return new NavigationResult(request.RouteSegment);
        }
        else
        {
            Target.SelectedItem = null;
            Logger.LogWarning("No NavigationViewItem item found with name or text '{segmentName}'", segmentName);
            return new NavigationResult($"No NavigationViewItem item found with name or text '{segmentName}'");
        }
    }
}

public class NavigationViewNavigator : SelectorNavigator<NavigationView>
{
    protected string navigatedName = string.Empty;
    protected object? navigatedItem = null;

    protected override string SelectedName => navigatedName;

    private ItemSelector<NavigationView> itemSelector;

    public NavigationViewNavigator(IServiceProvider serviceProvider) : base(serviceProvider)
    {
        Type itemSelectorType = NavigationRegion.GetItemSelectorType(Target) ?? typeof(ByNameNavigationViewItemSelector);
        itemSelector = (ItemSelector<NavigationView>)ServiceProvider.GetRequiredService(itemSelectorType);
        itemSelector.Navigator = this;
        Target.SelectionChanged += async (s, e) =>
        {
            var selectedItem = Target.SelectedItem;
            if (selectedItem == navigatedItem) return;
            navigatedItem = selectedItem;
            var selectedContainer = (NavigationViewItem)Target.ContainerFromMenuItem(selectedItem);
            var segmentName = RouteProperties.GetSegmentName(selectedContainer);
            if (segmentName is null) return;

            var segment = Region.Segment.Nested.FirstOrDefault(s => s.Name == segmentName);
            if (segment is null)
            {
                Logger.LogWarning($"No segment found with name '{segmentName}'");
                return;
            }
            navigatedName = segmentName;
            navigatedItem = selectedItem;

            var request = new NameSegmentNavigationRequest(s, segment, segment.BuildDefaultRoute());
            await NavigateAsync(request);
        };
    }

    protected override NavigationResult SelectItem(NavigationRequest request)
    {
        var result = itemSelector.SelectItem(request);
        if (!result.Success) return result;
        navigatedItem = Target.SelectedItem;
        navigatedName = request.NameSegment.Name;
        return result;
    }
}

public class NavigationViewContentNavigator : ContentControlNavigatorBase<NavigationView>
{
    protected string navigatedName = string.Empty;
    protected object? navigatedItem = null;

    protected string SelectedName => navigatedName;

    private ItemSelector<NavigationView> itemSelector;

    public NavigationViewContentNavigator(IServiceProvider serviceProvider) : base(serviceProvider)
    {
        Type itemSelectorType = NavigationRegion.GetItemSelectorType(Target) ?? typeof(ByNameNavigationViewItemSelector);
        itemSelector = (ItemSelector<NavigationView>)ServiceProvider.GetRequiredService(itemSelectorType);
        itemSelector.Navigator = this;
    }

    protected override async ValueTask<NavigationResult> NavigateCoreAsync(NavigationRequest request)
    {
        if (navigatedName == request.NameSegment.Name) return new NavigationResult(request.RouteSegment, isSkipped: true);

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
            itemSelector.SelectItem(request);
        }
        navigationStatus.NavigationFinished += NavigationFinished;

        return new NavigationResult(request.RouteSegment, view);
    }
}
