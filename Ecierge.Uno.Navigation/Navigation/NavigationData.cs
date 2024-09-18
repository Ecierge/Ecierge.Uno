namespace Ecierge.Uno.Navigation;

using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

public interface INavigationData : IImmutableDictionary<string, object>
{
    TData? GetData<TData>()
        where TData : class;

    object? GetData(Type dataType);

    new INavigationData Add(string key, object value);

    new INavigationData SetItem(string key, object value);

    INavigationData Union(INavigationData? other);
}

public class NavigationData : INavigationData
{
    private readonly ImmutableDictionary<string, object> data;

    private NavigationData() => data = ImmutableDictionary.Create<string, object>(StringComparer.InvariantCultureIgnoreCase);
    protected NavigationData(ImmutableDictionary<string, object> data) => this.data = data;
    public static NavigationData Empty { get; } = new NavigationData();

    public NavigationData(IEnumerable<KeyValuePair<string, object>> data) => this.data = data.ToImmutableDictionary(StringComparer.InvariantCultureIgnoreCase);

    public object this[string key] => ((IReadOnlyDictionary<string, object>)data)[key];

    public IEnumerable<string> Keys => data.Keys;

    public IEnumerable<object> Values => data.Values;

    public int Count => data.Count;

    public INavigationData Add(string key, object value) => new NavigationData(data.Add(key, value));
    IImmutableDictionary<string, object> IImmutableDictionary<string, object>.Add(string key, object value) => Add(key, value);

    public INavigationData AddRange(IEnumerable<KeyValuePair<string, object>> pairs)
    {
        return new NavigationData((IImmutableDictionary<string, object>)data.AddRange(pairs));
    }
    IImmutableDictionary<string, object> IImmutableDictionary<string, object>.AddRange(IEnumerable<KeyValuePair<string, object>> pairs) => AddRange(pairs);

    public INavigationData Clear() => new NavigationData(data.Clear());
    IImmutableDictionary<string, object> IImmutableDictionary<string, object>.Clear() => Clear();

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

    public IEnumerator<KeyValuePair<string, object>> GetEnumerator() => data.GetEnumerator();

    public INavigationData Remove(string key) => new NavigationData(data.Remove(key));
    IImmutableDictionary<string, object> IImmutableDictionary<string, object>.Remove(string key) => Remove(key);

    public INavigationData RemoveRange(IEnumerable<string> keys) => new NavigationData(data.RemoveRange(keys));
    IImmutableDictionary<string, object> IImmutableDictionary<string, object>.RemoveRange(IEnumerable<string> keys) => RemoveRange(keys);

    public INavigationData SetItem(string key, object value) => new NavigationData(data.SetItem(key, value));
    IImmutableDictionary<string, object> IImmutableDictionary<string, object>.SetItem(string key, object value) => SetItem(key, value);

    public INavigationData SetItems(IEnumerable<KeyValuePair<string, object>> items) => new NavigationData(data.SetItems(items));
    IImmutableDictionary<string, object> IImmutableDictionary<string, object>.SetItems(IEnumerable<KeyValuePair<string, object>> items) => SetItems(items);

    public bool TryGetKey(string equalKey, out string actualKey) => data.TryGetKey(equalKey, out actualKey);

    public bool TryGetValue(string key, [MaybeNullWhen(false)] out object value) => data.TryGetValue(key, out value);

    public INavigationData Union(INavigationData? other)
    {
        if (other is null) return this;

        var dictionary = new Dictionary<string, object>(data);
        foreach (var pair in other)
        {
            dictionary[pair.Key] = pair.Value;
        }
        return new NavigationData(dictionary);
    }

    IEnumerator IEnumerable.GetEnumerator() => ((IEnumerable)data).GetEnumerator();
}

internal static class NavigationDataExtensions
{
    public static void ApplyScopedInstanceServices(this INavigationData navigationData, IServiceProvider serviceProvider)
    {
        var scopedInstanceOptions = serviceProvider.GetService<IOptions<ScopedInstanceRepositoryOptions>>()?.Value;
        if (scopedInstanceOptions is null)
            return;

        var typesToClone = scopedInstanceOptions.TypesToClone;
        foreach (var value in navigationData.Values)
        {
            Type serviceType = value.GetType();
            if (typesToClone.Contains(serviceType))
            {
                serviceProvider.AddScopedInstance(serviceType, value);
            }
        }
    }
}
