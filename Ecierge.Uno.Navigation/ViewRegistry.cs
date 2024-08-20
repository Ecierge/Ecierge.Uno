namespace Ecierge.Uno.Navigation;

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;

using Microsoft.Extensions.DependencyInjection;

public interface IViewRegistry : IRegistry<ViewMap>
{
    ViewMap this[Type view] { get; }
}

public sealed class ViewMapNotFoundException : KeyNotFoundException
{
    private ViewMapNotFoundException() { }
    public ViewMapNotFoundException(Type view) : base($"View '{view}' not found in the registry.") { }
    private ViewMapNotFoundException(string message) : base(message) { }
    private ViewMapNotFoundException(string message, Exception innerException) : base(message, innerException) { }
}

public class ViewRegistry(IEnumerable<ViewMap> items) : IViewRegistry
{
    private readonly ImmutableArray<ViewMap> items = items.ToImmutableArray();
    public ImmutableDictionary<Type, ViewMap> Views { get; } = items.ToImmutableDictionary(x => x.View, x => x);
#pragma warning disable CA1033 // Interface methods should be callable by child types
    ImmutableArray<ViewMap> IRegistry<ViewMap>.Items => items;
#pragma warning restore CA1033 // Interface methods should be callable by child types
#pragma warning disable CA1043 // Use Integral Or String Argument For Indexers
    public ViewMap this[Type view]
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

public interface IViewRegistryBuilder : IRegistryBuilder<ViewMap>
{
    public new IViewRegistry Build();
}

public class ViewRegistryBuilder(IServiceCollection services) : RegistryBuilder<ViewMap>(services), IViewRegistryBuilder
{
#pragma warning disable CA1725 // Parameter names should match base declaration
    protected override void AddItem(ViewMap view) => Register(view);
#pragma warning restore CA1725 // Parameter names should match base declaration

    public new ViewRegistryBuilder Register(ViewMap view)
    {
        view = view ?? throw new ArgumentNullException(nameof(view));
        items.Add(view);
        Services.AddTransient(view.View);
        if (view.ViewModel is not null)
            Services.AddTransient(view.ViewModel);
        return this;
    }

    public new ViewRegistryBuilder Register([NotNull] params ViewMap[] views)
    {
        foreach (var view in views) Register(view);
        return this;
    }

    public override IRegistry<ViewMap> Build() => new ViewRegistry(items);

    IViewRegistry IViewRegistryBuilder.Build() => new ViewRegistry(items);
}
