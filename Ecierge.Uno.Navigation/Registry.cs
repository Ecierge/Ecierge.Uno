namespace Ecierge.Uno.Navigation;

using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;

using Microsoft.Extensions.DependencyInjection;

public interface IRegistry<T>
{
    ImmutableArray<T> Items { get; }
}

public interface IRegistryBuilder<T>
{
    IEnumerable<T> Items { get; }
    IRegistryBuilder<T> Register(T item);
    IRegistryBuilder<T> Register(params T[] items);

    IRegistry<T> Build();
}

#pragma warning disable CA1708 // Identifiers should differ by more than case
public abstract class RegistryBuilder<T> : IRegistryBuilder<T>
{
#pragma warning disable CA1051 // Do not declare visible instance fields
    protected readonly List<T> items = new();
#pragma warning restore CA1051 // Do not declare visible instance fields

    public IEnumerable<T> Items => items;

    protected IServiceCollection Services { get; private set; }

    protected RegistryBuilder(IServiceCollection services) => Services = services;

    public IRegistryBuilder<T> Register(T item)
    {
        item = item ?? throw new ArgumentNullException(nameof(item));
        AddItem(item);
        return this;
    }

    public IRegistryBuilder<T> Register([NotNull] params T[] items)
    {
        foreach (var item in items) AddItem(item);
        return this;
    }

    protected abstract void AddItem(T item);

    public abstract IRegistry<T> Build();
}
#pragma warning restore CA1708 // Identifiers should differ by more than case
