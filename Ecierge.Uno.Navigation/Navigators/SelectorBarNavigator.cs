namespace Ecierge.Uno.Navigation.Navigators;

using System.Linq;

using Ecierge.Uno.Navigation;

using Microsoft.Extensions.Logging;

using RouteProperties = Uno.Navigation.Route;

internal class SelectorBarNavigator : SelectorNavigator<SelectorBar>
{
    private bool isFirstNavigation = true;
    protected string navigatedName = string.Empty;
    protected SelectorBarItem? navigatedItem = null;

    protected override string SelectedName => navigatedName;

    public SelectorBarNavigator(IServiceProvider serviceProvider) : base(serviceProvider)
    {
        Target.SelectionChanged += async (s, e) =>
        {
            var selectedItem = Target.SelectedItem;
            if (selectedItem == navigatedItem) return;

            var segmentName = RouteProperties.GetSegmentName(selectedItem);
            var segment = Region.Segment.NestedAfterData.FirstOrDefault(s => s.Name == segmentName);
            if (segment is null)
            {
                segmentName = selectedItem.Text;
                segment = Region.Segment.Nested.FirstOrDefault(s => s.Name == segmentName);
                if (segment is null)
                {
                    Logger.LogWarning($"No segment found with name '{segmentName}'");
                    return;
                }
                navigatedName = segmentName;
                navigatedItem = selectedItem;
            }

            if (isFirstNavigation)
            {
                isFirstNavigation = false;
                _ = this.NavigateLocalSegmentAsync(s, segment);
            }
            else
            {
                var request = new NameSegmentNavigationRequest(s, segment, segment.BuildDefaultRoute());
                await NavigateAsync(request);
            }
        };
    }

    protected override NavigationResult SelectItem(NavigationRequest request)
    {
        if (navigatedName == request.NameSegment.Name) return new NavigationResult(request.RouteSegment, isSkipped: true);

        var selectorBar = (SelectorBar)Region!.Target!;
        var item =
            selectorBar.Items.FirstOrDefault(
                i => RouteProperties.GetSegmentName(i) == request.NameSegment.Name
            );
        if (item is null)
        {
            item = selectorBar.Items.FirstOrDefault(i => i.Text == request.NameSegment.Name);
        }
        if (item is null)
        {
            Logger.LogWarning("No SelectorBarItem item found with Route.SegmentName or Name '{segmentName}'", request.NameSegment.Name);
            return new NavigationResult($"No SelectorBarItem item found with Route.SegmentName or Name '{request.NameSegment.Name}'");
        }
        navigatedName = request.NameSegment.Name;
        navigatedItem = item;
        // TODO: Rewrite to item selector and set index
        selectorBar.SelectedItem = item;
        return new NavigationResult(request.RouteSegment);
    }
}
