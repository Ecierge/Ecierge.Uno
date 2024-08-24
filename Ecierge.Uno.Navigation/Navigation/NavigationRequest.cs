namespace Ecierge.Uno.Navigation;

[ImplicitKeys(IsEnabled = false)]
public abstract partial record NavigationRequest(object Sender, INavigationData? NavigationData)
{
    public Guid Id { get; } = Guid.NewGuid();

    public abstract NameSegment NameSegment { get; }
    public abstract RouteSegment RouteSegment { get; }
}

public record NameSegmentNavigationRequest(
      object Sender
    , NameSegment Segment
    , INavigationData? NavigationData = null)
    : NavigationRequest(Sender, NavigationData)
{
    public override NameSegment NameSegment => Segment;
    public override RouteSegment RouteSegment => Segment;
}

public record DataSegmentNavigationRequest(
      object Sender
    , DataSegment Segment
    , object? RouteData
    , INavigationData? NavigationData = null)
    : NavigationRequest(Sender, NavigationData)
{
    public override NameSegment NameSegment => Segment.ParentNameSegment;
    public override RouteSegment RouteSegment => Segment;
}
