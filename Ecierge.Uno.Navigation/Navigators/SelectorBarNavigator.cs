namespace Ecierge.Uno.Navigation.Navigators;

using System;
using System.Linq;

using Ecierge.Uno.Navigation;

using Microsoft.Extensions.Logging;

using RouteProperties = Ecierge.Uno.Navigation.Route;

/// <summary>
/// Navigates to the segment of the item that the user selects in a <see cref="SelectorBar"/>, and selects the item of
/// the segment that is navigated to.
/// </summary>
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
            if (selectedItem is null || selectedItem == navigatedItem) return;

            var segmentName = RouteProperties.GetSegmentName(selectedItem);
            var segment = Region.Segment.NestedAfterData.FirstOrDefault(s => s.Name == segmentName);
            if (segment is null)
            {
                segmentName = selectedItem.Text;
                segment = Region.Segment.Nested.FirstOrDefault(s => s.Name == segmentName);
                if (segment is null)
                {
                    Logger.LogWarning("No segment found with name '{SegmentName}'", segmentName);
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
                // A route of one segment, so that the selection is announced and recorded like any other navigation
                // and the segments below it keep what they show
                await this.NavigateRouteAsync(s, Routing.Route.Empty.Add(segment));
            }
        };
    }

    protected override NavigationResult SelectItem(NavigationRequest request)
    {
        if (navigatedName == request.NameSegment.Name) return new NavigationResult(request, isSkipped: true);

        var selectorBar = Target;
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
        return new NavigationResult(request);
    }
}
