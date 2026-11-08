using System.Collections.Immutable;
using Ecierge.Uno.Navigation.Routing;

using Microsoft.Extensions.DependencyInjection;

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

        /// <summary>Gets the segment that is nested in this one by its name.</summary>
        /// <exception cref="NestedSegmentNotFoundException">
        /// No segment nested in this one has the name <paramref name="name"/>.
        /// </exception>
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
        public ImmutableArray<string> Permissions { get; init; } = ImmutableArray<string>.Empty;
        public ViewMapBase? ViewMap { get; set; }
        public DataSegment? DataSegment { get; private init; }
        public override ImmutableArray<NameSegment> Nested { get; protected init; } = ImmutableArray<NameSegment>.Empty;
        public override ImmutableArray<NameSegment> NestedAfterData => DataSegment is not null ? DataSegment.Nested : Nested;

        /// <summary>Creates a segment that carries data.</summary>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="dataSegment"/> is <see langword="null"/>.
        /// </exception>
        public NameSegment(string name, ViewMapBase? view, DataSegment dataSegment, params ImmutableArray<string> permissions) : base(name)
        {
            ViewMap = view;
            dataSegment = dataSegment ?? throw new ArgumentNullException(nameof(dataSegment));
            IsDefault = false;
            if (dataSegment is not null)
            {
                DataSegment = dataSegment;
                dataSegment.ParentSegment = this;
            }
            if (!permissions.IsDefaultOrEmpty)
            {
                Permissions = permissions;
            }
        }

        public NameSegment(string name, ViewMapBase? view = null, bool isDefault = false, ImmutableArray<string> permissions = default, ImmutableArray<NameSegment> nested = default) : base(name)
        {
            ViewMap = view;
            IsDefault = isDefault;
            if (!permissions.IsDefaultOrEmpty)
            {
                Permissions = permissions;
            }
            if (!nested.IsDefaultOrEmpty)
            {
                Nested = nested;
                foreach (var segment in nested) segment.ParentSegment = this;
            }

        }

        /// <summary>
        /// Builds the route that navigates to this segment, and from it through the default nested segments down to a
        /// page.
        /// </summary>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="data"/> is <see langword="null"/>, but the data of the segment is mandatory.
        /// </exception>
        /// <exception cref="InvalidOperationException">The data segment has no data map.</exception>
        public Routing.Route BuildDefaultRoute(IServiceProvider serviceProvider, object? data = null)
        {
            var list = new List<RouteSegmentInstance> {
                this switch
                {
                    DialogSegment dialog => new DialogSegmentInstance(dialog),
                    _ =>new NameSegmentInstance(this)
                }
            };
            INavigationData navigationData = data as INavigationData ?? NavigationData.Empty;
            if (DataSegment is { } dataSegment)
            {
                if (data is null && dataSegment.IsMandatory)
                    throw new ArgumentNullException(nameof(data), "Data is mandatory.");

                if (data is not null && (data is not INavigationData given || given.ContainsKey(dataSegment.Name)))
                {
                    // The route holds the handle of the entity, which is loaded when somebody needs the entity
                    var dataMap = (INavigationDataMap)serviceProvider.GetRequiredService(
                        dataSegment.DataMap ?? throw new InvalidOperationException($"The data segment '{dataSegment.Name}' has no data map."));
                    var entity = data is INavigationData navData
                        ? dataMap.GetEntity(navData, dataSegment.Name)
                        // The route that the entity is part of is the one of the navigator that builds it
                        : dataMap.CreateEntity(data, data: serviceProvider.GetInstance<INavigationData>());
                    navigationData = navigationData.SetItem(dataSegment.Name, entity);
                    list.Add(new DataSegmentInstance(dataSegment, entity.Primitive, null));
                    AddDefaultSegments(dataSegment);
                }
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
            DataMap = dataMap;
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
        public DialogSegment(string name, ViewMapBase? view = null, ImmutableArray<NameSegment> nested = default) : base(name, view, false, default, nested) { }
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

        /// <summary>Merges two segments of the same name into one that has the nested segments of both.</summary>
        /// <exception cref="InvalidOperationException">
        /// One of the segments is a dialog, the names differ, only one of the segments has data, or the view maps
        /// differ.
        /// </exception>
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
                return new NameSegment(a.Name, viewMap, a.IsDefault, default, nested);
            }

        }

        /// <summary>
        /// Merges two data segments of the same data map into one that has the nested segments of both.
        /// </summary>
        /// <exception cref="InvalidOperationException">The data maps of the segments differ.</exception>
        public static DataSegment Merge(DataSegment a, DataSegment b)
        {
            if (a.DataMap != b.DataMap)
                throw new InvalidOperationException("Segments must have the same data map to be merged.");

            var nested = a.Nested.Concat(b.Nested).ToArray();
            return new DataSegment(a.Name, a.DataMap, a.IsMandatory, nested);
        }
    }
}
