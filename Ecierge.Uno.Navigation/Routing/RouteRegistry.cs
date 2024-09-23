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

    public IRouteRegistryBuilder Register(Func<IViewRegistry, INavigationDataRegistry, NameSegment[]> createSegments)
    {
        createSegments = createSegments ?? throw new ArgumentNullException(nameof(createSegments));
        factories.Add(createSegments);
        return this;
    }

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
