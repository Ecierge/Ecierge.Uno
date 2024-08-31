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

public class ByNameNavigationViewItemSelector : ItemSelector<NavigationView>
{
    public override NavigationResult SelectItem()
    {
        var selectedItem = Target.SelectedItem;

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
        NameSegment segment = Navigator.Region.Segment;
        var segmentName = segment.Name;
        var containerToSelect = allContainers.FirstOrDefault(c => c.Name == segmentName);
        if (containerToSelect != default)
        {
            var itemToSelect = Target.MenuItemFromContainer(containerToSelect);
            Target.SelectedItem = itemToSelect;
            return new NavigationResult(segment);
        }
        else
        {
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
        Target.SelectionChanged += async (s, e) =>
        {
            var selectedItem = Target.SelectedItem;
            if (selectedItem == navigatedItem) return;
            navigatedItem = selectedItem;
            var selectedContainer = (NavigationViewItem)Target.ContainerFromMenuItem(selectedItem);
            var itemName = selectedContainer.Name ?? Target.SelectedItem as string;
            if (itemName is null) return;

            var segment = Region.Segment.Nested.FirstOrDefault(s => s.Name == itemName);
            if (segment is null)
            {
                Logger.LogWarning($"No segment found with name '{itemName}'");
                return;
            }

            var request = new NameSegmentNavigationRequest(s, segment);
            await NavigateAsync(request);
        };
    }

    protected override NavigationResult SelectItem(NavigationRequest request) => itemSelector.SelectItem();
}

public class NavigationViewContentNavigator : FactoryNavigator<NavigationView>
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
        if (navigatedName == request.NameSegment.Name) return new NavigationResult(request.RouteSegment);

        var result = CreateView(request);
        if (!result.Success) return result;

        navigatedName = request.NameSegment.Name;
        var view = result.Result;
        Target.Content = view;

        var dispatcher = ServiceProvider.GetRequiredService<DispatcherQueue>();
        dispatcher.TryEnqueue(DispatcherQueuePriority.Low, () =>
        {
            itemSelector.SelectItem();
        });

        return new NavigationResult(request.RouteSegment, view);
    }
}
