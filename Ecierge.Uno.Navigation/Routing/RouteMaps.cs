namespace Ecierge.Uno.Navigation;

using System.Collections.Immutable;

using Ecierge.Uno.Navigation.Routing;

public abstract record RouteSegment(string Name)
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

    public NameSegment this[string name]
    {
        get
        {
            var segment = Nested.FirstOrDefault(s => s.Name == name);
            if (segment is not null) return segment;
            throw new NestedSegmentNotFoundException(this, name);
        }
    }
}

public record NameSegment : RouteSegment
{
    public bool IsDefault { get; init; } = false;
    public bool HasData => Data is not null;
    public bool HasMandatoryData => Data is not null && Data.IsMandatory;
    public ViewMap? ViewMap { get; set; }
    public DataSegment? Data { get; private init; }
    public override ImmutableArray<NameSegment> Nested { get; protected init; } = ImmutableArray<NameSegment>.Empty;

    public NameSegment(string name, ViewMap view, DataSegment data) : base(name)
    {
        ViewMap = view ?? throw new ArgumentNullException(nameof(view));
        data = data ?? throw new ArgumentNullException(nameof(data));
        IsDefault = false;
        if (data is not null)
        {
            Data = data;
            data.ParentSegment = this;
        }
    }

    public NameSegment(string name, ViewMap? view = null, bool isDefault = false, ImmutableArray<NameSegment> nested = default) : base(name)
    {
        ViewMap = view;
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
    public bool IsMandatory { get; init; } = true;
    public Type? DataMap { get; private init; }
    public override ImmutableArray<NameSegment> Nested { get; protected init; }
    public DataSegment(string name, Type? dataMap, bool isMandatory = true, params NameSegment[] nested) : base(name)
    {
        IsMandatory = isMandatory;
        if (nested is not null)
        {
            Nested = nested.ToImmutableArray();
            foreach (var segment in nested) segment.ParentSegment = this;
        }
    }
}

public record DialogSegment : NameSegment
{
    public DialogSegment(string name, ViewMap view, DataSegment data) : base(name, view, data) { }
    public DialogSegment(string name, ViewMap? view = null, ImmutableArray<NameSegment> nested = default) : base(name, view, false, nested) { }
}
