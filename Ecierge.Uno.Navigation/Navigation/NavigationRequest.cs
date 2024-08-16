namespace Ecierge.Uno.Navigation;

[ImplicitKeys(IsEnabled = false)]
public abstract partial record NavigationRequest(object Sender, IReadOnlyDictionary<string, object>? QueryParameters)
{
    public Guid Id { get; } = Guid.NewGuid();

    public abstract NameSegment NameSegment { get; }
}

public record NameSegmentNavigationRequest(
      object Sender
    , NameSegment Segment
    , IReadOnlyDictionary<string, object>? QueryParameters = null)
    : NavigationRequest(Sender, QueryParameters)
{
    public override NameSegment NameSegment => Segment;

}

public record DataSegmentNavigationRequest<TRouteData>(
      object Sender
    , DataSegment Segment
    , TRouteData? RouteData
    , IReadOnlyDictionary<string, object>? QueryParameters = null)
    : NavigationRequest(Sender, QueryParameters)
{
    public override NameSegment NameSegment => Segment.ParentNameSegment;
}
