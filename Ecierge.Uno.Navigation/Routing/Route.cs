namespace Ecierge.Uno.Navigation.Routing;

using System.Diagnostics;
using System.Collections.Immutable;

using MoreLinq;

public abstract record RouteSegmentInstance()
{
    public abstract RouteSegment Segment { get; }
}

[DebuggerDisplay("{Segment.Name}")]
public record NameSegmentInstance(NameSegment NameSegment) : RouteSegmentInstance
{
    public override RouteSegment Segment => NameSegment;
}

[DebuggerDisplay("<{Segment.Name}>")]
public record DataSegmentInstance(DataSegment DataSegment, string Primitive, object? Data) : RouteSegmentInstance
{
    public override RouteSegment Segment => DataSegment;
}

[DebuggerDisplay("#{Segment.Name}")]
public record DialogSegmentInstance(DialogSegment DialogSegment) : RouteSegmentInstance
{
    public override RouteSegment Segment => throw new NotImplementedException("Dialog segments not implemented.");
}

public record Route
{
    public ImmutableArray<RouteSegmentInstance> Segments { get; init; }
    public INavigationData? Data { get; init; }
    public bool Refresh { get; init; }

    public static Route Empty => new Route(ImmutableArray<RouteSegmentInstance>.Empty);

    public Route() : this(ImmutableArray<RouteSegmentInstance>.Empty) { }

    public Route(INavigationData? data) : this(ImmutableArray<RouteSegmentInstance>.Empty, data) { }

    public Route(ImmutableArray<RouteSegmentInstance> segments, INavigationData? data = null, bool refresh = false)
    {
        if (segments.Count(s => s is DialogSegmentInstance) > 1)
            throw new InvalidOperationException("Dialog segment cannot be nested.");
        Segments = segments;
        Data = data;
        Refresh = refresh;
    }

    public override string ToString()
     => string.Join('/',
            Segments.Select(s =>
                s switch
                {
                    DialogSegmentInstance d => "!" + d.Segment.Name,
                    NameSegmentInstance n => n.Segment.Name,
                    DataSegmentInstance d => d.Segment.Name,
                    _ => null
                })
        );

    public bool IsSubRouteOf(Route route)
    {
        if (Segments.Length > route.Segments.Length) return false;
        return Segments.Zip(route.Segments).All(pair => pair.First == pair.Second);
    }

    public Route GoBack()
    {
        var lastSegment = Segments.LastOrDefault();
        if (lastSegment is null) return this;
        switch (lastSegment)
        {
            case NameSegmentInstance _:
                return new(Segments.RemoveAt(Segments.Length - 1), Data, Refresh);
            case DataSegmentInstance dataSegment:
                var data = Data!.Remove(dataSegment.DataSegment.Name);
                return new(Segments.RemoveAt(Segments.Length - 2), Data, Refresh);
            case DialogSegmentInstance _:
                throw new NotSupportedException("Dialog segments must not be last.");
            default:
                throw new NotSupportedException("Unknown segment type.");
        }
    }

    public Route Remove(int count)
    {
        var dataToRemove =
            Segments[^count..]
                .Where(x => x is DataSegmentInstance)
                .Select(x => ((DataSegmentInstance)x).DataSegment.Name);
        var data = Data!.RemoveRange(dataToRemove);
        return new(Segments[..^count], Data, Refresh);
    }

    public Route TrimHead(RouteSegmentInstance segment)
    {
        var index = Segments.IndexOf(segment);
        if (index == -1) return this;
        var segments = Segments.Skip(index).ToImmutableArray();
        return new(segments, Data, Refresh);
    }

    public Route TrimHead(Route route) => TrimHead(route.Segments.Last());

    /// <summary>
    /// Takes the specified number of segments from the end of the route.
    /// Also cleans the navigation data from the removed data segments.
    /// </summary>
    /// <param name="count">Segments to take.</param>
    /// <returns>New route with the specified number of segments.</returns>
    public Route Take(int count)
    {
        var dataToRemove =
            Segments.Skip(count).OfType<DataSegmentInstance>().Select(segmentInstance => segmentInstance.DataSegment.Name);
        var data = Data?.RemoveRange(dataToRemove);
        if (count == -1) return this;
        var segments = Segments.Take(count).ToImmutableArray();
        return new(segments, data, Refresh);
    }

    /// <summary>
    /// Removes all segments from the end of the route until the specified segment.
    /// </summary>
    /// <param name="segment"></param>
    /// <returns>New route till the specified segment.</returns>
    public Route TrimTill(RouteSegmentInstance? segment)
    {
        if (segment is null) return this;
        var index = Segments.LastIndexOf(segment);
        return this.Take(index + 1);
    }

    /// <summary>
    /// Removes all segments from the end of the route until the specified segment.
    /// </summary>
    /// <param name="segment"></param>
    /// <returns>New route till the specified segment.</returns>
    public Route TrimTill(RouteSegment segment)
    {
        var instance = Segments.LastOrDefault(s => s.Segment == segment);
        if (instance is null) return this;
        return this.TrimTill(instance);
    }

    public Route Add(NameSegment segment, INavigationData? navigationData = null)
    {
        bool isNested = (segment is DialogSegment) || (Segments.LastOrDefault()?.Segment.Nested.Contains(segment) ?? true);
        if (!isNested) throw new InvalidOperationException("Segment is not nested.");
        var data = Data?.Union(navigationData) ?? navigationData;
        return new(Segments.Add(new NameSegmentInstance(segment)), data, Refresh);
    }

    public Route Add(DataSegment segment, string primitive, object? data, INavigationData? navigationData = null)
    {
        bool isNested = Segments.LastOrDefault()?.Segment.Nested.Contains(segment.ParentNameSegment) ?? true;
        if (!isNested) throw new InvalidOperationException("Segment is not nested.");

        var segments = Segments.AddRange(
                new NameSegmentInstance(segment.ParentNameSegment),
                new DataSegmentInstance(segment, primitive, data)
            );
        navigationData = Data?.Union(navigationData) ?? navigationData;
        if (data is not null)
            navigationData = navigationData?.Add(segment.Name, data) ?? NavigationData.Empty.Add(segment.Name, data);
        return new(segments, navigationData, Refresh);
    }

    public Route Join(Route route)
    {
        bool isNested = Segments.LastOrDefault()?.Segment.Nested.Contains(route.Segments.FirstOrDefault()?.Segment) ?? true;
        if (!isNested) throw new InvalidOperationException("Segment is not nested.");
        return new(Segments.AddRange(route.Segments), Data?.Union(route.Data) ?? route.Data, Refresh);
    }

    public Route ReplaceLast(NameSegment segment)
    {
        var baseSegments = Segments.SkipLast(1).ToList();
        bool isNested = baseSegments.LastOrDefault()?.Segment.Nested.Contains(segment) ?? true;
        if (!isNested) throw new InvalidOperationException("Segment is not nested.");
        baseSegments.Add(new NameSegmentInstance(segment));
        return new(baseSegments.ToImmutableArray(), Data, Refresh);
    }

    public Route ReplaceLast(DataSegment segment, string primitive, object? data)
    {
        var baseSegments = Segments.SkipLast(1).ToList();
        bool isNested = baseSegments.LastOrDefault()?.Segment.Nested.Contains(segment.ParentNameSegment) ?? true;
        if (!isNested) throw new InvalidOperationException("Segment is not nested.");

        baseSegments.Add(new NameSegmentInstance(segment.ParentNameSegment));
        baseSegments.Add(new DataSegmentInstance(segment, primitive, data));
        return new(baseSegments.ToImmutableArray(), Data, Refresh);
    }

    public NameSegmentInstance? LastNamedSegment =>
        this.Segments.LastOrDefault() switch
        {
            NameSegmentInstance n => n,
            DataSegmentInstance d => Segments[^2] as NameSegmentInstance,
            _ => null
        };

    internal ImmutableArray<RouteSegmentInstance> GetNavigatableSegments()
    {
        if (Segments.Length < 2) return Segments;

        var last = Segments.Last();
        return
            Segments.Pairwise((current, next) =>
            {
                if (next is DataSegmentInstance) return default!;
                else return current!;
            })
            .Where(s => s is not null)
            .Append(last)
            .ToImmutableArray();
    }

    public Route ReplaceData(DataSegment segment, string primitive, Task<object>? data)
    {
        var instance = Segments.FirstOrDefault(s => s.Segment == segment);
        if (instance is null) throw new InvalidOperationException("Segment not found.");
        var segments = Segments.Replace(instance, new DataSegmentInstance(segment, primitive, data));
        return new(segments, Data, true);
    }
}
