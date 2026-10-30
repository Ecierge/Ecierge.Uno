namespace Ecierge.Uno.Navigation;

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

using Ecierge.Uno.Navigation.Helpers;

public interface IRouteRegistry : IRegistry<NameSegment>
{
    public NameSegment RootSegment { get; }
}

public class RouteRegistry : IRouteRegistry
{
    public ImmutableArray<NameSegment> Items { get; }
    public NameSegment RootSegment { get; }

    /// <summary>Creates the registry of the segments of the routes.</summary>
    /// <exception cref="ArgumentNullException"><paramref name="items"/> is <see langword="null"/>.</exception>
    public RouteRegistry(IEnumerable<NameSegment> items)
    {
        items = items ?? throw new ArgumentNullException(nameof(items));
        this.Items = items.ToImmutableArray();
        this.RootSegment = new NameSegment(Qualifiers.Root, null, isDefault: true, nested: Items);
    }
}

public interface IRouteRegistryBuilder
{
    IEnumerable<Func<IViewRegistry, INavigationDataRegistry, NameSegment[]>> Factories { get; }
    IRouteRegistryBuilder Register(Func<IViewRegistry, INavigationDataRegistry, NameSegment[]> createSegments);
    public IRouteRegistry Build(IViewRegistry viewRegistry, INavigationDataRegistry navigationDataRegistry);
}

public class RouteRegistryBuilder : IRouteRegistryBuilder
{
    private readonly List<Func<IViewRegistry, INavigationDataRegistry, NameSegment[]>> factories = new();

    public IEnumerable<Func<IViewRegistry, INavigationDataRegistry, NameSegment[]>> Factories => factories;

    /// <summary>Adds a factory of segments, which is called once the views and the data maps are known.</summary>
    /// <exception cref="ArgumentNullException"><paramref name="createSegments"/> is <see langword="null"/>.</exception>
    public IRouteRegistryBuilder Register(Func<IViewRegistry, INavigationDataRegistry, NameSegment[]> createSegments)
    {
        createSegments = createSegments ?? throw new ArgumentNullException(nameof(createSegments));
        factories.Add(createSegments);
        return this;
    }

    /// <summary>
    /// Builds the registry from the segments that the factories give, merging the segments that have the same name.
    /// </summary>
    /// <exception cref="InvalidOperationException">
    /// A segment has data but no view map, or the segments of the same name cannot be merged.
    /// </exception>
    public IRouteRegistry Build(IViewRegistry viewRegistry, INavigationDataRegistry navigationDataRegistry)
    {
        var segments =
            Segments.Merge(
                factories.SelectMany(
                    factory => factory(viewRegistry, navigationDataRegistry)
                )
            );
        foreach (var segment in segments)
        {
            if (segment is NameSegment nameSegment && nameSegment.HasData && nameSegment.ViewMap is null)
                throw new InvalidOperationException($"Name segment '{nameSegment.Name}' has data but no view map.");
        }
        return new RouteRegistry(segments);
    }
}
