using Ecierge.Uno.Navigation.Navigation;

namespace Ecierge.Uno.Navigation;

[ImplicitKeys(IsEnabled = false)]
public abstract partial record NavigationRequest(object Sender, INavigationData? NavigationData)
{
    public Guid Id { get; } = Guid.NewGuid();

    public abstract NameSegment NameSegment { get; }
}

public record NameSegmentNavigationRequest(
      object Sender
    , NameSegment Segment
    , INavigationData? NavigationData = null)
    : NavigationRequest(Sender, NavigationData)
{
    public override NameSegment NameSegment => Segment;

}

public record DataSegmentNavigationRequest<TRouteData>(
      object Sender
    , DataSegment Segment
    , TRouteData? RouteData
    , INavigationData? NavigationData = null)
    : NavigationRequest(Sender, NavigationData)
{
    public override NameSegment NameSegment => Segment.ParentNameSegment;
}
