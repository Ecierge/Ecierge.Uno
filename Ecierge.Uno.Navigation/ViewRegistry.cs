namespace Ecierge.Uno.Navigation;

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;

using Microsoft.Extensions.DependencyInjection;

public interface IViewRegistry : IRegistry<ViewMapBase>
{
    ViewMapBase this[Type view] { get; }
}

public sealed class ViewMapNotFoundException : KeyNotFoundException
{
    private ViewMapNotFoundException() { }
    public ViewMapNotFoundException(Type view) : base($"View '{view}' not found in the registry.") { }
    private ViewMapNotFoundException(string message) : base(message) { }
    private ViewMapNotFoundException(string message, Exception innerException) : base(message, innerException) { }
}

public class ViewRegistry(IEnumerable<ViewMapBase> items) : IViewRegistry
{
    private readonly ImmutableArray<ViewMapBase> items = items.ToImmutableArray();
    public ImmutableDictionary<Type, ViewMapBase> Views { get; } = items.ToImmutableDictionary(x => x.View, x => x);
#pragma warning disable CA1033 // Interface methods should be callable by child types
    ImmutableArray<ViewMapBase> IRegistry<ViewMapBase>.Items => items;
#pragma warning restore CA1033 // Interface methods should be callable by child types
#pragma warning disable CA1043 // Use Integral Or String Argument For Indexers
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

public interface IViewRegistryBuilder : IRegistryBuilder<ViewMapBase>
{
    public new IViewRegistry Build();
}

public class ViewRegistryBuilder(IServiceCollection services) : RegistryBuilder<ViewMapBase>(services), IViewRegistryBuilder
{
#pragma warning disable CA1725 // Parameter names should match base declaration
    protected override void AddItem(ViewMapBase view) => Register(view);
#pragma warning restore CA1725 // Parameter names should match base declaration

    public new ViewRegistryBuilder Register(ViewMapBase view)
    {
        view = view ?? throw new ArgumentNullException(nameof(view));
        items.Add(view);
        view.Register(Services);
        
        return this;
    }

    public new ViewRegistryBuilder Register([NotNull] params ViewMapBase[] views)
    {
        foreach (var view in views) Register(view);
        return this;
    }

    public override IRegistry<ViewMapBase> Build() => new ViewRegistry(items);

    IViewRegistry IViewRegistryBuilder.Build() => new ViewRegistry(items);
}
