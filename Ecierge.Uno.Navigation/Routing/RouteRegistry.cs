namespace Ecierge.Uno.Navigation;

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

using Microsoft.Extensions.DependencyInjection;

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
    IEnumerable<Func<IViewRegistry, NameSegment[]>> Factories { get; }
    IRouteRegistryBuilder Register(Func<IViewRegistry, NameSegment[]> createSegments);
    public IRouteRegistry Build(IViewRegistry viewRegistry);
}

public class RouteRegistryBuilder : IRouteRegistryBuilder
{
    private readonly List<Func<IViewRegistry, NameSegment[]>> factories = new();

    public IEnumerable<Func<IViewRegistry, NameSegment[]>> Factories => factories;

    public IRouteRegistryBuilder Register(Func<IViewRegistry, NameSegment[]> createSegments)
    {
        createSegments = createSegments ?? throw new ArgumentNullException(nameof(createSegments));
        factories.Add(createSegments);
        return this;
    }

    public IRouteRegistry Build(IViewRegistry viewRegistry)
    {
        var segments = factories.SelectMany(factory => factory(viewRegistry));
        return new RouteRegistry(segments);
    }
}
