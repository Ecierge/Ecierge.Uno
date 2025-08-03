namespace Ecierge.Uno.Navigation;

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;

using Microsoft.Extensions.DependencyInjection;

public interface INavigationDataRegistry : IRegistry<Type>
{
    ImmutableDictionary<Type, Type> EntityMap { get; }
    Type this[Type entity] { get; }
    bool TryGetForAssignableEntity(Type entity, [NotNullWhen(true)] out Type? data);
}

public sealed class NavigationDataMapNotFoundException : KeyNotFoundException
{
    private NavigationDataMapNotFoundException() { }
    public NavigationDataMapNotFoundException(Type data) : base($"NavigationData '{data}' not found in the registry.") { }
    private NavigationDataMapNotFoundException(string message) : base(message) { }
    private NavigationDataMapNotFoundException(string message, Exception innerException) : base(message, innerException) { }
}

public class NavigationDataRegistry(IReadOnlyDictionary<Type, Type> entityMap) : INavigationDataRegistry
{
    public ImmutableDictionary<Type, Type> EntityMap { get; } = entityMap.ToImmutableDictionary();
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
    INavigationDataRegistryBuilder Register<T>() where T : class, INavigationDataMap;
    public INavigationDataRegistry Build();
}

public class NavigationDataRegistryBuilder : INavigationDataRegistryBuilder
{
    private readonly IServiceCollection services;
    private readonly Dictionary<Type, Type> entityMap = new();
    public IReadOnlyDictionary<Type, Type> EntityMap => entityMap;

    public NavigationDataRegistryBuilder(IServiceCollection services)
    {
        this.services = services ?? throw new ArgumentNullException(nameof(services));
    }

    public INavigationDataRegistryBuilder Register<T>() where T : class, INavigationDataMap
    {
        services.AddSingleton<T>();
        services.AddInheritedScopedInstance(T.EntityType);
        entityMap.Add(T.EntityType, typeof(T));
        return this;
    }

    public INavigationDataRegistry Build() => new NavigationDataRegistry(entityMap);
}
