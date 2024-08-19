namespace Ecierge.Uno.Navigation.Navigators;

using System.Linq;

using Microsoft.Extensions.Logging;

internal class SelectorBarNavigator : SelectorNavigator
{
    protected new SelectorBar Target => (SelectorBar)base.Target!;
    protected string navigatedName = string.Empty;
    protected SelectorBarItem? navigatedItem = null;

    protected override string SelectedName => navigatedName;

    public SelectorBarNavigator(IServiceProvider serviceProvider) : base(serviceProvider)
    {
        Target.SelectionChanged += async (s, e) =>
        {
            var selectedItem = Target.SelectedItem;
            if (selectedItem == navigatedItem) return;

            var segmentName = selectedItem.Name ?? selectedItem.Text;
            var segment = Region.Segment.Nested.FirstOrDefault(s => s.Name == segmentName);
            if (segment is null)
            {
                segmentName = selectedItem.Text;
                segment = Region.Segment.Nested.FirstOrDefault(s => s.Name == segmentName);
                if (segment is null)
                {
                    Logger.LogWarning($"No segment found with name '{segmentName}'");
                    return;
                }
            }

            var request = new NameSegmentNavigationRequest(s, segment);
            await NavigateAsync(request);
        };
    }

    public override NavigationResponse SelectItem(NavigationRequest request)
    {
        // TODO: Change to already navigated
        if (navigatedName == request.NameSegment.Name) return new SuccessfulNavigationResponse(null, this);

        var selectorBar = (SelectorBar)Region!.Target!;
        var item = selectorBar.Items.FirstOrDefault(i => i.Name == request.NameSegment.Name);
        if (item is null)
        {
            item = selectorBar.Items.FirstOrDefault(i => i.Text == request.NameSegment.Name);
        }
        navigatedName = request.NameSegment.Name;
        navigatedItem = item;
        selectorBar.SelectedItem = item;
        return new SuccessfulNavigationResponse(null, this);
    }
}
