using System.Diagnostics;

namespace Ecierge.Uno.Navigation;

[ImplicitKeys(IsEnabled = false)]
public abstract partial record NavigationRequest(object Sender, Routing.Route Route)
{
    public Guid Id { get; } = Guid.NewGuid();

    public abstract NameSegment NameSegment { get; }
    public abstract RouteSegment RouteSegment { get; }
    internal abstract ViewMapBase? View { get; }
}

[DebuggerDisplay("🔙")]
public record BackNavigationRequest(object Sender, Routing.Route Route) : NavigationRequest(Sender, Route)
{
    // TODO: Implement correctly
    public override NameSegment NameSegment => throw new NotSupportedException("Back navigation does not have a name segment.");
    public override RouteSegment RouteSegment => throw new NotSupportedException("Back navigation does not have a route segment.");
    internal override ViewMapBase? View => null;
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
    internal override ViewMapBase? View => Segment.ViewMap;
}

[DebuggerDisplay("{NameSegment.Name}/<{Segment.Name}:{RouteDataPrimitive}>")]
public record PrimitiveDataSegmentNavigationRequest(
      object Sender
    , DataSegment Segment
    , string RouteDataPrimitive
    , Routing.Route Route)
    : NavigationRequest(Sender, Route)
{
    public override NameSegment NameSegment => Segment.ParentNameSegment;
    public override RouteSegment RouteSegment => Segment;
    internal override ViewMapBase? View => Segment.ParentNameSegment.ViewMap;

    public TaskDataSegmentNavigationRequest WithDataEntity(Task routeDataTask)
    {
        return new TaskDataSegmentNavigationRequest(Sender, Segment, RouteDataPrimitive, routeDataTask, Route);
    }
}

[DebuggerDisplay("{NameSegment.Name}/<{Segment.Name}:{RouteDataPrimitive}>")]
public record TaskDataSegmentNavigationRequest(
      object Sender
    , DataSegment Segment
    , string RouteDataPrimitive
    , Task RouteDataTask
    , Routing.Route Route)
    : NavigationRequest(Sender, Route)
{
    public override NameSegment NameSegment => Segment.ParentNameSegment;
    public override RouteSegment RouteSegment => Segment;
    internal override ViewMapBase? View => Segment.ParentNameSegment.ViewMap;
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
    internal override ViewMapBase? View => Segment.ViewMap;
}
