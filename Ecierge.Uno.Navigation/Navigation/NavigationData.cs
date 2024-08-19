namespace Ecierge.Uno.Navigation;

using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;

public interface INavigationData : IImmutableDictionary<string, object>
{
    TData? GetData<TData>()
        where TData : class;

    object? GetData(Type dataType);
}

internal class NavigationData : INavigationData
{
    private readonly ImmutableDictionary<string, object> data;

    private NavigationData() => data = ImmutableDictionary<string, object>.Empty;
    public static NavigationData Empty { get; } = new NavigationData();

    public NavigationData(IEnumerable<KeyValuePair<string, object>> data) => this.data = data.ToImmutableDictionary();

    public object this[string key] => ((IReadOnlyDictionary<string, object>)data)[key];

    public IEnumerable<string> Keys => data.Keys;

    public IEnumerable<object> Values => data.Values;

    public int Count => data.Count;

    public IImmutableDictionary<string, object> Add(string key, object value)
    {
        return ((IImmutableDictionary<string, object>)data).Add(key, value);
    }

    public IImmutableDictionary<string, object> AddRange(IEnumerable<KeyValuePair<string, object>> pairs)
    {
        return ((IImmutableDictionary<string, object>)data).AddRange(pairs);
    }

    public IImmutableDictionary<string, object> Clear()
    {
        return ((IImmutableDictionary<string, object>)data).Clear();
    }

    public bool Contains(KeyValuePair<string, object> pair) => data.Contains(pair);

    public bool ContainsKey(string key) => data.ContainsKey(key);

    public TData? GetData<TData>()
        where TData : class
    {
        return TryGetValue(string.Empty, out var data) ? data as TData : default;
    }

    public object? GetData(Type dataType)
    {
        return TryGetValue(string.Empty, out var data) &&
            (data.GetType() == dataType || data.GetType().IsSubclassOf(dataType)) ? data : default;
    }

    public IEnumerator<KeyValuePair<string, object>> GetEnumerator()
    {
        return ((IEnumerable<KeyValuePair<string, object>>)data).GetEnumerator();
    }

    public IImmutableDictionary<string, object> Remove(string key)
    {
        return ((IImmutableDictionary<string, object>)data).Remove(key);
    }

    public IImmutableDictionary<string, object> RemoveRange(IEnumerable<string> keys)
    {
        return ((IImmutableDictionary<string, object>)data).RemoveRange(keys);
    }

    public IImmutableDictionary<string, object> SetItem(string key, object value)
    {
        return ((IImmutableDictionary<string, object>)data).SetItem(key, value);
    }

    public IImmutableDictionary<string, object> SetItems(IEnumerable<KeyValuePair<string, object>> items)
    {
        return ((IImmutableDictionary<string, object>)data).SetItems(items);
    }

    public bool TryGetKey(string equalKey, out string actualKey) => data.TryGetKey(equalKey, out actualKey);

    public bool TryGetValue(string key, [MaybeNullWhen(false)] out object value) => data.TryGetValue(key, out value);

    IEnumerator IEnumerable.GetEnumerator() => ((IEnumerable)data).GetEnumerator();
}
