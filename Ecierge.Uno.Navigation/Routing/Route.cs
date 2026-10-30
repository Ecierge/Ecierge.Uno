namespace Ecierge.Uno.Navigation.Routing;

using System.Collections.Immutable;
using System.Diagnostics;

using MoreLinq;

/// <summary>
/// One step of a route that was navigated or is about to be navigated.
/// </summary>
public abstract record RouteSegmentInstance()
{
    /// <summary>
    /// Gets the segment of the route tree that the instance stands for.
    /// </summary>
    public abstract RouteSegment Segment { get; }

    /// <summary>
    /// Tells whether the other instance is the same step of a route: the same named or dialog segment, or the same
    /// data segment with the same value. The data that a step carries is not compared, and the value of a data
    /// segment is compared without regard to case, as the data of a route is.
    /// </summary>
    /// <param name="other">The instance to compare with.</param>
    /// <returns><see langword="true"/> if both instances are the same step.</returns>
    public bool IsSameSegmentAs(RouteSegmentInstance? other)
     => (this, other) switch
        {
            (DataSegmentInstance a, DataSegmentInstance b) =>
                a.DataSegment.Name == b.DataSegment.Name
                && string.Equals(a.Primitive, b.Primitive, StringComparison.OrdinalIgnoreCase),
            (DataSegmentInstance, _) or (_, DataSegmentInstance) => false,
            _ => other is not null && IsDialog == other.IsDialog && Segment.Name == other.Segment.Name
        };

    /// <summary>
    /// Gets a value that tells whether the instance stands for a dialog.
    /// </summary>
    public bool IsDialog => Segment is DialogSegment;
}

/// <summary>
/// A step of a route to a named segment.
/// </summary>
/// <param name="NameSegment">The segment.</param>
[DebuggerDisplay("{Segment.Name}")]
public record NameSegmentInstance(NameSegment NameSegment) : RouteSegmentInstance
{
    /// <inheritdoc/>
    public override RouteSegment Segment => NameSegment;
}

/// <summary>
/// A step of a route that gives the value of a data segment.
/// </summary>
/// <param name="DataSegment">The segment.</param>
/// <param name="Primitive">The value of the segment as it is written in a route, for example the id of an entity.</param>
/// <param name="Data">The task that loads the entity of the value, when it is known at this step.</param>
[DebuggerDisplay("<{Segment.Name}>")]
public record DataSegmentInstance(DataSegment DataSegment, string Primitive, Task? Data) : RouteSegmentInstance
{
    /// <inheritdoc/>
    public override RouteSegment Segment => DataSegment;
}

/// <summary>
/// A step of a route to a dialog.
/// </summary>
/// <param name="DialogSegment">The segment.</param>
[DebuggerDisplay("#{Segment.Name}")]
public record DialogSegmentInstance(DialogSegment DialogSegment) : RouteSegmentInstance
{
    /// <inheritdoc/>
    public override RouteSegment Segment => DialogSegment;
}

/// <summary>
/// A path through the route tree with the data that the steps of the path carry.
/// </summary>
public record Route
{
    /// <summary>
    /// Gets the steps of the route.
    /// </summary>
    public ImmutableArray<RouteSegmentInstance> Segments { get; init; }

    /// <summary>
    /// Gets the data of the route. It is never <see langword="null"/>; a route without data has
    /// <see cref="NavigationData.Empty"/>.
    /// </summary>
    public INavigationData Data { get; init; }

    /// <summary>
    /// Gets the step at the index.
    /// </summary>
    /// <param name="index">The index of the step.</param>
    public RouteSegmentInstance this[int index] => Segments[index];

    /// <summary>
    /// Gets a route without steps and without data.
    /// </summary>
    public static Route Empty => new Route(ImmutableArray<RouteSegmentInstance>.Empty);

    /// <summary>
    /// Creates a route without steps and without data.
    /// </summary>
    public Route() : this(ImmutableArray<RouteSegmentInstance>.Empty) { }

    /// <summary>
    /// Creates a route without steps.
    /// </summary>
    /// <param name="data">The data of the route.</param>
    public Route(INavigationData? data) : this(ImmutableArray<RouteSegmentInstance>.Empty, data) { }

    /// <summary>
    /// Creates a route.
    /// </summary>
    /// <param name="segments">The steps of the route.</param>
    /// <param name="data">The data of the route.</param>
    public Route(ImmutableArray<RouteSegmentInstance> segments, INavigationData? data = null)
    {
        segments = segments.IsDefault ? ImmutableArray<RouteSegmentInstance>.Empty : segments;
        if (segments.Count(s => s.IsDialog) > 1)
            throw new InvalidOperationException("Dialog segment cannot be nested.");
        Segments = segments;
        Data = data ?? NavigationData.Empty;
    }

    /// <summary>
    /// Writes the route for a log or a debugger. A data segment is shown by its name. Use <see cref="ToPath"/> to
    /// write a route that can be read back.
    /// </summary>
    public override string ToString()
     => string.Join(
            Qualifiers.Separator,
            Segments.Select(s =>
                s switch
                {
                    DialogSegmentInstance d => Qualifiers.Dialog + d.Segment.Name,
                    NameSegmentInstance n => n.Segment.Name,
                    DataSegmentInstance d => d.Segment.Name,
                    _ => null
                })
        );

    /// <summary>
    /// Writes the route as text that can be read back: the name of a named segment, the value of a data segment and
    /// the name of a dialog with the dialog qualifier in front, separated by the segment separator.
    /// </summary>
    /// <example>
    /// <code>
    /// Building/3f2a0c4e-5b1d-4e07-9a52-0a8d43f1c6b9/Details/Floors
    /// </code>
    /// </example>
    /// <returns>The text of the route.</returns>
    public string ToPath()
     => string.Join(
            Qualifiers.Separator,
            Segments.Select(s =>
                s switch
                {
                    DataSegmentInstance data => data.Primitive,
                    { IsDialog: true } dialog => Qualifiers.Dialog + dialog.Segment.Name,
                    _ => s.Segment.Name
                })
        );

    /// <summary>
    /// Tells whether the names of the steps are exactly the given names, each data segment written by its name.
    /// </summary>
    /// <param name="segments">The names of the steps.</param>
    /// <returns><see langword="true"/> if the route consists of exactly these steps.</returns>
    public bool StartsWithSegments(IReadOnlyList<string> segments) =>
        Segments.Select(s =>
            s switch
            {
                DialogSegmentInstance d => Qualifiers.Dialog + d.Segment.Name,
                NameSegmentInstance n => n.Segment.Name,
                DataSegmentInstance d => d.Segment.Name,
                _ => null
            })
        .SequenceEqual(segments);

    /// <summary>
    /// Tells whether the steps of this route are the first steps of the other route.
    /// </summary>
    /// <param name="route">The route to compare with.</param>
    /// <returns><see langword="true"/> if this route is not longer than the other and equal to it as far as it goes.</returns>
    public bool IsSubRouteOf(Route route)
    {
        if (Segments.Length > route.Segments.Length) return false;
        return Segments.Zip(route.Segments).All(pair => pair.First == pair.Second);
    }

    /// <summary>
    /// Gets the number of leading steps that this route and the other have in common. Steps are compared by their
    /// segments and, for data segments, by their values.
    /// </summary>
    /// <param name="route">The route to compare with.</param>
    /// <returns>The number of the common leading steps.</returns>
    public int CommonPrefixLength(Route route)
    {
        route = route ?? throw new ArgumentNullException(nameof(route));
        var count = Math.Min(Segments.Length, route.Segments.Length);
        var length = 0;
        while (length < count && Segments[length].IsSameSegmentAs(route.Segments[length]))
        {
            length++;
        }

        return length;
    }

    /// <summary>
    /// Tells whether the steps of this route are the first steps of the other route, comparing the steps by their
    /// segments and, for data segments, by their values rather than by their data.
    /// </summary>
    /// <param name="route">The route to compare with.</param>
    /// <returns><see langword="true"/> if this route is a prefix of the other.</returns>
    public bool IsPrefixOf(Route route) => CommonPrefixLength(route) == Segments.Length;

    /// <summary>
    /// Gets the route up to, but without, the first dialog. The route of a page and the route of a dialog over it
    /// have the same page route.
    /// </summary>
    /// <returns>The route without the dialog and the steps inside it.</returns>
    public Route WithoutDialog()
    {
        for (var index = 0; index < Segments.Length; index++)
        {
            if (Segments[index].IsDialog) return Take(index);
        }

        return this;
    }

    /// <summary>
    /// Keeps the first steps of the route and drops the others, together with the data of the dropped data segments.
    /// </summary>
    /// <param name="count">The number of the steps to keep.</param>
    /// <returns>A new route with the first <paramref name="count"/> steps, or this route if it is not longer.</returns>
    public Route Take(int count)
    {
        if (count < 0 || count >= Segments.Length) return this;
        var dataToRemove =
            Segments.Skip(count).OfType<DataSegmentInstance>().Select(segmentInstance => segmentInstance.DataSegment.Name);
        return new(Segments.Take(count).ToImmutableArray(), Data.RemoveRange(dataToRemove));
    }

    /// <summary>
    /// Drops the first steps of the route, up to but not including the given step.
    /// </summary>
    /// <param name="segment">The step that the new route starts with.</param>
    /// <returns>The route that starts with the step, or this route if the step is not in it.</returns>
    public Route TrimHead(RouteSegmentInstance segment)
    {
        var index = Segments.IndexOf(segment);
        if (index == -1) return this;
        var segments = Segments.Skip(index).ToImmutableArray();
        return new(segments, Data);
    }

    /// <summary>
    /// Drops the first steps of the route, up to but not including the last step of the other route.
    /// </summary>
    /// <param name="route">The route whose last step the new route starts with.</param>
    /// <returns>The route that starts with the step, or this route if the other route is empty or not a part of it.</returns>
    public Route TrimHead(Route route)
     => route.Segments.IsEmpty ? this : TrimHead(route.Segments[route.Segments.Length - 1]);

    /// <summary>
    /// Removes all steps after the given step, together with the data of the removed data segments.
    /// </summary>
    /// <param name="segment">The last step to keep.</param>
    /// <returns>The route up to the step, or this route if the step is not in it.</returns>
    public Route TrimTill(RouteSegmentInstance? segment)
    {
        if (segment is null) return this;
        var index = Segments.LastIndexOf(segment);
        if (index < 0) return this;
        return this.Take(index + 1);
    }

    /// <summary>
    /// Removes all steps after the last step of the given segment.
    /// </summary>
    /// <param name="segment">The segment whose last step is the last step to keep.</param>
    /// <returns>The route up to the step, or this route if the segment is not in it.</returns>
    public Route TrimTill(RouteSegment segment)
    {
        var instance = Segments.LastOrDefault(s => s.Segment == segment);
        if (instance is null) return this;
        return this.TrimTill(instance);
    }

    private static RouteSegmentInstance CreateInstance(NameSegment segment)
     => segment is DialogSegment dialog ? new DialogSegmentInstance(dialog) : new NameSegmentInstance(segment);

    /// <summary>
    /// Adds a step to a named or dialog segment.
    /// </summary>
    /// <param name="segment">The segment, which must be nested in the segment of the last step.</param>
    /// <param name="navigationData">The data that the step adds to the route.</param>
    /// <returns>A new route with the step.</returns>
    /// <exception cref="InvalidOperationException">The segment is not nested in the last segment of the route.</exception>
    public Route Add(NameSegment segment, INavigationData? navigationData = null)
    {
        bool isNested = (segment is DialogSegment) || (Segments.LastOrDefault()?.Segment.Nested.Contains(segment) ?? true);
        if (!isNested) throw new InvalidOperationException("Segment is not nested.");
        var data = navigationData is null ? Data : Data.Union(navigationData);
        return new(Segments.Add(CreateInstance(segment)), data);
    }

    /// <summary>
    /// Adds the step of a named segment that owns data, followed by the step of its data.
    /// </summary>
    /// <param name="segment">The data segment, whose named segment must be nested in the segment of the last step.</param>
    /// <param name="primitive">The value of the data segment.</param>
    /// <param name="data">The task that loads the entity of the value, if it is known.</param>
    /// <param name="navigationData">The data that the step adds to the route.</param>
    /// <returns>A new route with the two steps.</returns>
    /// <exception cref="InvalidOperationException">The segment is not nested in the last segment of the route.</exception>
    public Route Add(DataSegment segment, string primitive, Task? data = null, INavigationData? navigationData = null)
    {
        bool isNested = Segments.LastOrDefault()?.Segment.Nested.Contains(segment.ParentNameSegment) ?? true;
        if (!isNested) throw new InvalidOperationException("Segment is not nested.");

        var segments = Segments.AddRange(
                CreateInstance(segment.ParentNameSegment),
                new DataSegmentInstance(segment, primitive, data)
            );
        var merged = navigationData is null ? Data : Data.Union(navigationData);
        if (data is not null && !merged.ContainsKey(segment.Name))
        {
            merged = merged.Add(segment.Name, data);
        }

        return new(segments, merged);
    }

    /// <summary>
    /// Appends the steps of another route.
    /// </summary>
    /// <param name="route">The route to append, whose first segment must be nested in the segment of the last step.</param>
    /// <returns>A new route with the steps and the data of both routes.</returns>
    /// <exception cref="InvalidOperationException">The first segment is not nested in the last segment of the route.</exception>
    public Route Join(Route route)
    {
        bool isNested = Segments.LastOrDefault()?.Segment.Nested.Contains(route.Segments.FirstOrDefault()?.Segment) ?? true;
        if (!isNested) throw new InvalidOperationException("Segment is not nested.");
        return new(Segments.AddRange(route.Segments), Data.Union(route.Data));
    }

    /// <summary>
    /// Replaces the last step with a step to another named segment.
    /// </summary>
    /// <param name="segment">The segment, which must be nested in the segment of the step before the last.</param>
    /// <returns>A new route.</returns>
    /// <exception cref="InvalidOperationException">The segment is not nested in the segment of the step before the last.</exception>
    public Route ReplaceLast(NameSegment segment)
    {
        var baseSegments = Segments.SkipLast(1).ToList();
        bool isNested = baseSegments.LastOrDefault()?.Segment.Nested.Contains(segment) ?? true;
        if (!isNested) throw new InvalidOperationException("Segment is not nested.");
        baseSegments.Add(CreateInstance(segment));
        return new(baseSegments.ToImmutableArray(), Data);
    }

    /// <summary>
    /// Replaces the last step with the steps of a named segment that owns data and of its data.
    /// </summary>
    /// <param name="segment">The data segment, whose named segment must be nested in the segment of the step before the last.</param>
    /// <param name="primitive">The value of the data segment.</param>
    /// <param name="data">The task that loads the entity of the value, if it is known.</param>
    /// <returns>A new route.</returns>
    /// <exception cref="InvalidOperationException">The segment is not nested in the segment of the step before the last.</exception>
    public Route ReplaceLast(DataSegment segment, string primitive, Task? data)
    {
        var baseSegments = Segments.SkipLast(1).ToList();
        bool isNested = baseSegments.LastOrDefault()?.Segment.Nested.Contains(segment.ParentNameSegment) ?? true;
        if (!isNested) throw new InvalidOperationException("Segment is not nested.");

        baseSegments.Add(CreateInstance(segment.ParentNameSegment));
        baseSegments.Add(new DataSegmentInstance(segment, primitive, data));
        return new(baseSegments.ToImmutableArray(), Data);
    }

    /// <summary>
    /// Gets the last step that is a named or dialog segment, or the named segment of the data segment that the route
    /// ends with.
    /// </summary>
    public NameSegmentInstance? LastNamedSegment =>
        this.Segments.LastOrDefault() switch
        {
            NameSegmentInstance n => n,
            DialogSegmentInstance d => new NameSegmentInstance(d.DialogSegment),
            DataSegmentInstance when Segments.Length > 1 => Segments[Segments.Length - 2] as NameSegmentInstance,
            _ => null
        };

    /// <summary>
    /// Gets the steps that a navigator is asked to navigate to: a named segment followed by its data segment is one step.
    /// </summary>
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
}
