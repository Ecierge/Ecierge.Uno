using System.Diagnostics;

namespace Ecierge.Uno.Navigation;

[ImplicitKeys(IsEnabled = false)]
public abstract partial record NavigationRequest(object Sender, Routing.Route Route)
{
    public Guid Id { get; } = Guid.NewGuid();

    /// <summary>
    /// Gets the named segment that the request navigates to. For a data segment it is the segment that owns the data.
    /// </summary>
    public abstract NameSegment NameSegment { get; }

    /// <summary>
    /// Gets the segment that the request navigates to.
    /// </summary>
    public abstract RouteSegment RouteSegment { get; }

    /// <summary>
    /// Gets the view map of the segment, or <see langword="null"/> if the segment has no view.
    /// </summary>
    internal abstract ViewMapBase? View { get; }
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

    public TaskDataSegmentNavigationRequest WithDataEntity(Task routeDataTask, INavigationData navigationData)
    {
        var route = Route with { Data = navigationData };
        return new TaskDataSegmentNavigationRequest(Sender, Segment, RouteDataPrimitive, routeDataTask, route);
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

    /// <summary>Creates a request to open a dialog.</summary>
    /// <exception cref="ArgumentException">
    /// <paramref name="parentSegment"/> is a dialog segment, but dialogs are not nested.
    /// </exception>
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

    /// <inheritdoc />
    /// <exception cref="NotSupportedException">
    /// The parent segment is neither a name segment nor a data segment.
    /// </exception>
    public override NameSegment NameSegment => ParentSegment switch
    {
        NameSegment nameSegment => nameSegment,
        DataSegment dataSegment => dataSegment.ParentNameSegment,
        _ => throw new NotSupportedException("Not supported segment type.")
    };
    public override RouteSegment RouteSegment => Segment;
    internal override ViewMapBase? View => Segment.ViewMap;
}
