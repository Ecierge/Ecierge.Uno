namespace Ecierge.Uno.Navigation;

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;

public interface INavigationDataRegistry : IRegistry<INavigationDataMap>
{
    INavigationDataMap this[Type entity] { get; }
}

public sealed class NavigationDataMapNotFoundException : KeyNotFoundException
{
    private NavigationDataMapNotFoundException() { }
    public NavigationDataMapNotFoundException(Type data) : base($"NavigationData '{data}' not found in the registry.") { }
    private NavigationDataMapNotFoundException(string message) : base(message) { }
    private NavigationDataMapNotFoundException(string message, Exception innerException) : base(message, innerException) { }
}

public class NavigationDataRegistry(IEnumerable<INavigationDataMap> items) : INavigationDataRegistry
{
    private readonly ImmutableArray<INavigationDataMap> items = items.ToImmutableArray();
    public ImmutableDictionary<Type, INavigationDataMap> Data { get; } = items.ToImmutableDictionary(x => x.EntityType, x => x);
#pragma warning disable CA1033 // Interface methods should be callable by child types
    ImmutableArray<INavigationDataMap> IRegistry<INavigationDataMap>.Items => items;
#pragma warning restore CA1033 // Interface methods should be callable by child types
#pragma warning disable CA1043 // Use Integral Or String Argument For Indexers
    public INavigationDataMap this[Type entity]
    {
        get
        {
            if (Data.TryGetValue(entity, out var dataMap))
                return dataMap;
            else
                throw new NavigationDataMapNotFoundException(entity);
        }
    }
#pragma warning restore CA1043 // Use Integral Or String Argument For Indexers

}

public interface INavigationDataRegistryBuilder
{
    IEnumerable<INavigationDataMap> Items { get; }
    INavigationDataRegistryBuilder Register(INavigationDataMap navigationdata);
    INavigationDataRegistryBuilder Register([NotNull] params INavigationDataMap[] navigationdatas);
    public INavigationDataRegistry Build();
}

public class NavigationDataRegistryBuilder : INavigationDataRegistryBuilder
{
    private readonly List<INavigationDataMap> items = new();
    public IEnumerable<INavigationDataMap> Items => items;

    public INavigationDataRegistryBuilder Register(INavigationDataMap map)
    {
        map = map ?? throw new ArgumentNullException(nameof(map));
        items.Add(map);
        return this;
    }

    public INavigationDataRegistryBuilder Register([NotNull] params INavigationDataMap[] navigationDataMaps)
    {
        foreach (var map in navigationDataMaps) Register(map);
        return this;
    }

    public INavigationDataRegistry Build() => new NavigationDataRegistry(items);
}
