using System.Diagnostics;

namespace Ecierge.Uno.Navigation;

[ImplicitKeys(IsEnabled = false)]
public abstract partial record NavigationRequest(object Sender, Routing.Route Route)
{
    public Guid Id { get; } = Guid.NewGuid();

    public abstract NameSegment NameSegment { get; }
    public abstract RouteSegment RouteSegment { get; }
    internal abstract ViewMap? View { get; }
}

[DebuggerDisplay("{Segment.Name}")]
public record NameSegmentNavigationRequest(
      object Sender
    , NameSegment Segment
    , Routing.Route Route)
    : NavigationRequest(Sender, Route)
{
    public override NameSegment NameSegment => Segment;
    public override RouteSegment RouteSegment => Segment;
    internal override ViewMap? View => Segment.ViewMap;
}

[DebuggerDisplay("{NameSegment.Name}/{Segment.Name}")]
public record DataSegmentNavigationRequest(
      object Sender
    , DataSegment Segment
    , object? RouteData
    , Routing.Route Route)
    : NavigationRequest(Sender, Route)
{
    public override NameSegment NameSegment => Segment.ParentNameSegment;
    public override RouteSegment RouteSegment => Segment;
    internal override ViewMap? View => Segment.ParentNameSegment.ViewMap;
}

[DebuggerDisplay("!{Segment.Name}")]
public record DialogSegmentNavigationRequest : NavigationRequest
{
    internal bool Handle { get; set; } = false;
    public DialogSegment Segment { get; init; }
    public RouteSegment ParentSegment { get; init; }

    public DialogSegmentNavigationRequest(
      object sender
    , DialogSegment segment
    , RouteSegment parentSegment
    , Routing.Route route) : base(sender, route)
    {
        if (parentSegment is DialogSegment)
            throw new ArgumentException("Dialogs must not be nested.", nameof(parentSegment));
        Segment = segment;
        ParentSegment = parentSegment;
    }

    public override NameSegment NameSegment => ParentSegment switch
    {
        NameSegment nameSegment => nameSegment,
        DataSegment dataSegment => dataSegment.ParentNameSegment,
        _ => throw new NotSupportedException("Not supported segment type.")
    };
    public override RouteSegment RouteSegment => Segment;
    internal override ViewMap? View => Segment.ViewMap;
}
