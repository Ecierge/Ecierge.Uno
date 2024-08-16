namespace Ecierge.Uno.Navigation;

using System.Collections.Immutable;

public abstract record RouteSegment(
      string Name
    , ViewMap? View = null
    )
{
    internal RouteSegment ParentSegment { get; set; } = null!;
    internal NameSegment ParentNameSegment
    {
        get
        {
            while (ParentSegment is not NameSegment)
            {
                ParentSegment = ParentSegment.ParentSegment;
            }
            return (NameSegment)ParentSegment;
        }
    }

    public abstract ImmutableArray<NameSegment> Nested { get; protected init; }
}

public record NameSegment : RouteSegment
{
    public bool IsDefault { get; init; } = false;
    public DataSegment? Data { get; private init; }
    public override ImmutableArray<NameSegment> Nested { get; protected init; } = ImmutableArray<NameSegment>.Empty;

    public NameSegment(string name, ViewMap? view, DataSegment data) : base(name, view)
    {
        data = data ?? throw new ArgumentNullException(nameof(data));
        IsDefault = false;
        if (data is not null)
        {
            Data = data;
            data.ParentSegment = this;
        }
    }

    public NameSegment(string name, ViewMap? view, bool isDefault = false, ImmutableArray<NameSegment> nested = default) : base(name, view)
    {
        IsDefault = isDefault;
        if (!nested.IsDefaultOrEmpty)
        {
            Nested = nested;
            foreach (var segment in nested) segment.ParentSegment = this;
        }
    }
}

public record DataSegment : RouteSegment
{
    public override ImmutableArray<NameSegment> Nested { get; protected init; }
    public DataSegment(string name, ViewMap? view, params NameSegment[] nested) : base(name, view)
    {
        if (nested is not null)
        {
            Nested = nested.ToImmutableArray();
            foreach (var segment in nested) segment.ParentSegment = this;
        }
    }
}
