namespace Ecierge.Uno.Navigation;

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;

using Microsoft.Extensions.DependencyInjection;

public interface INavigationDataRegistry : IRegistry<Type>
{
    ImmutableDictionary<Type, Type> EntityMap { get; }
    ImmutableHashSet<Type> Primitives { get; }
    Type this[Type entity] { get; }
    bool HasAssignablePrimitive(Type primitive);
    bool TryGetForAssignableEntity(Type entity, [NotNullWhen(true)] out Type? data);
}

public sealed class NavigationDataMapNotFoundException : KeyNotFoundException
{
    private NavigationDataMapNotFoundException() { }
    public NavigationDataMapNotFoundException(Type data) : base($"NavigationData '{data}' not found in the registry.") { }
    private NavigationDataMapNotFoundException(string message) : base(message) { }
    private NavigationDataMapNotFoundException(string message, Exception innerException) : base(message, innerException) { }
}

public class NavigationDataRegistry(IReadOnlyDictionary<Type, Type> entityMap, IReadOnlySet<Type> primitives) : INavigationDataRegistry
{
    public ImmutableDictionary<Type, Type> EntityMap { get; } = entityMap.ToImmutableDictionary();
    public ImmutableHashSet<Type> Primitives { get; } = primitives.ToImmutableHashSet();
#pragma warning disable CA1033 // Interface methods should be callable by child types
    ImmutableArray<Type> IRegistry<Type>.Items => entityMap.Values.ToImmutableArray();
#pragma warning restore CA1033 // Interface methods should be callable by child types
#pragma warning disable CA1043 // Use Integral Or String Argument For Indexers
    public Type this[Type entity]
    {
        get
        {
            if (EntityMap.TryGetValue(entity, out var dataMap))
                return dataMap;
            else
                throw new NavigationDataMapNotFoundException(entity);
        }
    }
#pragma warning restore CA1043 // Use Integral Or String Argument For Indexers

    // TODO: Improve lookup performance
    public bool HasAssignablePrimitive(Type primitive)
    {
        if (Primitives.Contains(primitive))
            return true;
        foreach (var p in Primitives)
        {
            if (p.IsAssignableTo(primitive))
                return true;
        }
        return false;
    }

    public bool TryGetForAssignableEntity(Type entity, [NotNullWhen(true)] out Type? data)
    {
        foreach (var kvp in EntityMap)
        {
            if (kvp.Key.IsAssignableTo(entity))
            {
                data = kvp.Value;
                return true;
            }
        }
        data = default;
        return false;
    }
}

public interface INavigationDataRegistryBuilder
{
    IReadOnlyDictionary<Type, Type> EntityMap { get; }
    IReadOnlySet<Type> Primitives { get; }
    INavigationDataRegistryBuilder Register<T>() where T : class, INavigationDataMap;
    public INavigationDataRegistry Build();
}

public class NavigationDataRegistryBuilder : INavigationDataRegistryBuilder
{
    private readonly IServiceCollection services;
    private readonly Dictionary<Type, Type> entityMap = new();
    private readonly HashSet<Type> primitives = new();
    public IReadOnlyDictionary<Type, Type> EntityMap => entityMap;
    public IReadOnlySet<Type> Primitives => primitives;

    public NavigationDataRegistryBuilder(IServiceCollection services)
    {
        this.services = services ?? throw new ArgumentNullException(nameof(services));
    }

    public INavigationDataRegistryBuilder Register<T>() where T : class, INavigationDataMap
    {
        services.AddScoped<T>();
        entityMap.Add(T.EntityType, typeof(T));
        primitives.Add(T.PrimitiveType);
        return this;
    }

    public INavigationDataRegistryBuilder Register([NotNull] params INavigationDataMap[] navigationDataMaps)
    {
        foreach (var map in navigationDataMaps) Register(map);
        return this;
    }

    public INavigationDataRegistry Build() => new NavigationDataRegistry(entityMap, primitives);
}
