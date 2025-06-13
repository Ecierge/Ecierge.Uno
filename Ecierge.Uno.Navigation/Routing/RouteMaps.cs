using System.Collections.Immutable;

using Ecierge.Uno.Navigation.Routing;

namespace Ecierge.Uno.Navigation
{
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
        public abstract ImmutableArray<NameSegment> NestedAfterData { get; }

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
        public bool HasData => DataSegment is not null;
        public bool HasMandatoryData => DataSegment is not null && DataSegment.IsMandatory;
        public ViewMapBase? ViewMap { get; set; }
        public DataSegment? DataSegment { get; private init; }
        public override ImmutableArray<NameSegment> Nested { get; protected init; } = ImmutableArray<NameSegment>.Empty;
        public override ImmutableArray<NameSegment> NestedAfterData => DataSegment is not null ? DataSegment.Nested : Nested;

        public NameSegment(string name, ViewMapBase? view, DataSegment dataSegment) : base(name)
        {
            ViewMap = view;
            dataSegment = dataSegment ?? throw new ArgumentNullException(nameof(dataSegment));
            IsDefault = false;
            if (dataSegment is not null)
            {
                DataSegment = dataSegment;
                dataSegment.ParentSegment = this;
            }
        }

        public NameSegment(string name, ViewMapBase? view = null, bool isDefault = false, ImmutableArray<NameSegment> nested = default) : base(name)
        {
            ViewMap = view;
            IsDefault = isDefault;
            if (!nested.IsDefaultOrEmpty)
            {
                Nested = nested;
                foreach (var segment in nested) segment.ParentSegment = this;
            }
        }

        public Routing.Route BuildDefaultRoute(object? data = null)
        {
            var list = new List<RouteSegmentInstance> {
                this switch
                {
                    DialogSegment dialog => new DialogSegmentInstance(dialog),
                    _ =>new NameSegmentInstance(this)
                }
            };
            INavigationData navigationData = (data as INavigationData)!;
            if (DataSegment is not null)
            {
                if (data is null && DataSegment.IsMandatory)
                    throw new ArgumentNullException(nameof(data), "Data is mandatory.");
                if (navigationData is not null)
                {
                    // TODO: Do something with primitive value that is null now
                    list.Add(new DataSegmentInstance(DataSegment, null, navigationData[DataSegment.Name]));
                }
                else
                {
                    navigationData = NavigationData.Empty.Add(DataSegment.Name, data!);
                    // TODO: Do something with primitive value that is null now
                    list.Add(new DataSegmentInstance(DataSegment, null, data));
                }
                AddDefaultSegments(DataSegment);
            }
            else
            {
                AddDefaultSegments(this);
            }

            void AddDefaultSegments(RouteSegment segment)
            {
                if (segment.Nested.SingleOrDefault(s => s.IsDefault) is NameSegment defaultSegment)
                {
                    list.Add(new NameSegmentInstance(defaultSegment));
                    AddDefaultSegments(defaultSegment);
                }
            }
            return new Routing.Route(list.ToImmutableArray(), navigationData);
        }
    }

    public record DataSegment : RouteSegment
    {
        public bool IsMandatory { get; init; } = true;
        public Type? DataMap { get; private init; }
        public override ImmutableArray<NameSegment> Nested { get; protected init; }
        public override ImmutableArray<NameSegment> NestedAfterData => Nested;
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
        public DialogSegment(string name, ViewMapBase view, DataSegment data) : base(name, view, data) { }
        public DialogSegment(string name, ViewMapBase? view = null, ImmutableArray<NameSegment> nested = default) : base(name, view, false, nested) { }
    }
}

namespace Ecierge.Uno.Navigation.Helpers
{
    public static class Segments
    {
        private static Type DialogSegmentType = typeof(DialogSegment);

        public static ImmutableArray<NameSegment> Merge(IEnumerable<NameSegment> segments)
        {
            var orderedSegments = segments.OrderBy(s => s.Name).ToList();
            for (var i = 0; i < orderedSegments.Count - 1; i++)
            {
                var currentSegment = orderedSegments[i];
                var nextSegment = orderedSegments[i + 1];
                if (currentSegment.Name == nextSegment.Name)
                {
                    var segment = Merge(currentSegment, nextSegment);
                    orderedSegments[i] = segment;
                    orderedSegments.RemoveAt(i + 1);
                    i--;
                }
            }
            return orderedSegments.ToImmutableArray();
        }

        public static NameSegment Merge(NameSegment a, NameSegment b)
        {
            if (a.GetType() == DialogSegmentType || b.GetType() == DialogSegmentType)
                throw new InvalidOperationException("Dialog segments cannot be merged.");
            if (a.Name != b.Name)
                throw new InvalidOperationException("Segments must have the same name to be merged.");
            if (a.HasData != b.HasData)
                throw new InvalidOperationException("Segments must both have data or nested to be merged.");
            if (a.ViewMap is not null && b.ViewMap is not null && a.ViewMap != b.ViewMap)
                throw new InvalidOperationException("Segments must have the same view map to be merged.");

            var viewMap = a.ViewMap ?? b.ViewMap;
            if (a.HasData)
            {
                var dataSegment = Merge(a.DataSegment!, b.DataSegment!);
                return new NameSegment(a.Name, viewMap!, dataSegment);
            }
            else
            {
                var nested = a.Nested.Concat(b.Nested).ToImmutableArray();
                return new NameSegment(a.Name, viewMap, a.IsDefault, nested);
            }

        }

        public static DataSegment Merge(DataSegment a, DataSegment b)
        {
            if (a.DataMap != b.DataMap)
                throw new InvalidOperationException("Segments must have the same data map to be merged.");

            var nested = a.Nested.Concat(b.Nested).ToArray();
            return new DataSegment(a.Name, a.DataMap, a.IsMandatory, nested);
        }
    }
}
