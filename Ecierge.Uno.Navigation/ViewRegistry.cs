namespace Ecierge.Uno.Navigation;

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;

using Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Interface for a registry that holds view maps, allowing retrieval by view type.
/// </summary>
public interface IViewRegistry : IRegistry<ViewMapBase>
{
    ViewMapBase this[Type view] { get; }
}

/// <summary>
/// Exception thrown when a view map is not found in the registry.
/// </summary>
public sealed class ViewMapNotFoundException : KeyNotFoundException
{
    private ViewMapNotFoundException() { }
    /// <summary>
    /// Initializes a new instance of the <see cref="ViewMapNotFoundException"/> class with a specified view type.
    /// </summary>
    /// <param name="view">The type of the view that was not found.</param>
    public ViewMapNotFoundException(Type view) : base($"View '{view}' not found in the registry.") { }
    private ViewMapNotFoundException(string message) : base(message) { }
    private ViewMapNotFoundException(string message, Exception innerException) : base(message, innerException) { }
}

/// <summary>
/// A registry that holds view maps, allowing retrieval by view type.
/// </summary>
/// <param name="items">A collection of view maps to initialize the registry with.</param>
public class ViewRegistry(IEnumerable<ViewMapBase> items) : IViewRegistry
{
    private readonly ImmutableArray<ViewMapBase> items = items.ToImmutableArray();
    /// <summary>
    /// Gets a dictionary mapping view types to their corresponding view maps.
    /// </summary>
    public ImmutableDictionary<Type, ViewMapBase> Views { get; } = items.ToImmutableDictionary(x => x.View, x => x);
#pragma warning disable CA1033 // Interface methods should be callable by child types
    ImmutableArray<ViewMapBase> IRegistry<ViewMapBase>.Items => items;
#pragma warning restore CA1033 // Interface methods should be callable by child types
#pragma warning disable CA1043 // Use Integral Or String Argument For Indexers
    /// <summary>
    /// Indexer to retrieve a view map by its view type.
    /// </summary>
    /// <param name="view">The type of the view to retrieve the map for.</param>
    /// <returns>The corresponding view map.</returns>
    /// <exception cref="ViewMapNotFoundException">Thrown when the requested view map is not found.</exception>
    public ViewMapBase this[Type view]
    {
        get
        {
            if (Views.TryGetValue(view, out var viewMap))
                return viewMap;
            else
                throw new ViewMapNotFoundException(view);
        }
    }
#pragma warning restore CA1043 // Use Integral Or String Argument For Indexers
}

/// <summary>
/// Builder interface for constructing a view registry.
/// </summary>
public interface IViewRegistryBuilder : IRegistryBuilder<ViewMapBase>
{
    /// <summary>
    /// Builds the view registry with the registered view maps.
    /// </summary>
    /// <returns>An instance of <see cref="IViewRegistry"/> containing the registered views.</returns>
    public new IViewRegistry Build();
}

/// <summary>
/// Builder for constructing a view registry, allowing registration of view maps.
/// </summary>
/// <param name="services">The service collection to register view maps with.</param>
public class ViewRegistryBuilder(IServiceCollection services) : RegistryBuilder<ViewMapBase>(services), IViewRegistryBuilder
{
#pragma warning disable CA1725 // Parameter names should match base declaration
    protected override void AddItem(ViewMapBase view) => Register(view);
#pragma warning restore CA1725 // Parameter names should match base declaration

    /// <summary>
    /// Registers a view map in the registry.
    /// </summary>
    /// <param name="view">The view map to register.</param>
    /// <returns>This instance for method chaining.</returns>
    /// <exception cref="ArgumentNullException">Thrown when the view is null.</exception>
    public new ViewRegistryBuilder Register(ViewMapBase view)
    {
        view = view ?? throw new ArgumentNullException(nameof(view));
        items.Add(view);
        view.Register(Services);

        return this;
    }

    /// <summary>
    /// Registers multiple view maps in the registry.
    /// </summary>
    /// <param name="views">A collection of view maps to register.</param>
    /// <returns>This instance for method chaining.</returns>
    public ViewRegistryBuilder Register([NotNull] params IEnumerable<ViewMapBase> views)
    {
        foreach (var view in views) Register(view);
        return this;
    }

    /// <summary>
    /// Builds the view registry with the registered view maps.
    /// </summary>
    /// <returns>An instance of <see cref="IRegistry<ViewMapBase>"/> containing the registered views.</returns>
    public override IRegistry<ViewMapBase> Build() => new ViewRegistry(items);

    IViewRegistry IViewRegistryBuilder.Build() => new ViewRegistry(items);
}
