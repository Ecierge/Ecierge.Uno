namespace Ecierge.Uno.Navigation;

[ImplicitKeys(IsEnabled = false)]
public abstract partial record NavigationRequest(object Sender, INavigationData? NavigationData)
{
    public Guid Id { get; } = Guid.NewGuid();

    public abstract NameSegment NameSegment { get; }
    public abstract RouteSegment RouteSegment { get; }
    internal abstract ViewMap? View { get; }
}

public record NameSegmentNavigationRequest(
      object Sender
    , NameSegment Segment
    , INavigationData? NavigationData = null)
    : NavigationRequest(Sender, NavigationData)
{
    public override NameSegment NameSegment => Segment;
    public override RouteSegment RouteSegment => Segment;
    internal override ViewMap? View => Segment.View;
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
    internal override ViewMap? View => Segment.ParentNameSegment.View;
}

public record DialogSegmentNavigationRequest : NavigationRequest
{
    internal bool Handle { get; set; } = false;
    public DialogSegment Segment { get; init; }
    public RouteSegment ParentSegment { get; init; }

    public DialogSegmentNavigationRequest(
      object sender
    , DialogSegment segment
    , NameSegment parentSegment
    , INavigationData? NavigationData = null) : base(sender, NavigationData)
    {
        Segment = segment;
        ParentSegment = parentSegment;
    }

    public DialogSegmentNavigationRequest(
      object sender
    , DialogSegment segment
    , DataSegment parentSegments
    , INavigationData? NavigationData = null) : base(sender, NavigationData)
    {
        Segment = segment;
        ParentSegment = parentSegments;
    }

    public override NameSegment NameSegment => ParentSegment switch
    {
        NameSegment nameSegment => nameSegment,
        DataSegment dataSegment => dataSegment.ParentNameSegment,
        _ => throw new NotSupportedException("Not supported segment type.")
    };
    public override RouteSegment RouteSegment => Segment;
    internal override ViewMap? View => Segment.View;
}
