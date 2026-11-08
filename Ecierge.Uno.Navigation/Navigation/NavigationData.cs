namespace Ecierge.Uno.Navigation;

using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;

using Ecierge.Uno.Navigation.Routing;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

public interface INavigationData : IImmutableDictionary<string, object>
{
    TData? GetData<TData>()
        where TData : class;

    object? GetData(Type dataType);

    new INavigationData Add(string key, object value);
    INavigationData Add(KeyValuePair<string, object> kvp) => Add(kvp.Key, kvp.Value);
    new INavigationData Remove(string key);
    new INavigationData RemoveRange(IEnumerable<string> keys);
    new INavigationData Clear();
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

    public bool IsEmpty => data.IsEmpty;

    public INavigationData Add(string key, object value) => new NavigationData(data.Add(key, value));
    public INavigationData Add(KeyValuePair<string, object> kvp) => Add(kvp.Key, kvp.Value);
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

internal static class RouteExtensions
{
    private static readonly Type genericTaskType = typeof(Task<>);

    /// <summary>
    /// Applies scoped instance services from the route's navigation data to the service provider.
    /// </summary>
    /// <remarks>
    /// Only an entity that is already loaded is added. A pending or a failed load is never waited for, because a
    /// wait on the thread of the user interface may never end, and a failed load has no entity.
    /// </remarks>
    /// <param name="route">The route to apply scoped instance services from its navigation data.</param>
    /// <param name="serviceProvider">The services of the scope to add the instances to.</param>
    public static void ApplyScopedInstanceServices(this Routing.Route route, IServiceProvider serviceProvider)
    {
        var scopedInstanceOptions = serviceProvider.GetService<IOptions<ScopedInstanceRepositoryOptions>>()?.Value;
        if (scopedInstanceOptions is null)
            return;

        var typesToClone = scopedInstanceOptions.TypesToClone;
        foreach (var value in route.Data.Values)
        {
            switch (value)
            {
                case INavigationEntity handle:
                    if (typesToClone.Contains(handle.EntityType) && handle.TryGetLoadedEntity(out var entity))
                        serviceProvider.AddScopedInstance(handle.EntityType, entity);
                    break;
                case Task task:
                    if (task.IsCompletedSuccessfully
                        && TryGetTaskResult(task, out var resultType, out var result)
                        && typesToClone.Contains(resultType))
                        serviceProvider.AddScopedInstance(resultType, result);
                    break;
                default:
                    var serviceType = value.GetType();
                    if (typesToClone.Contains(serviceType))
                        serviceProvider.AddScopedInstance(serviceType, value);
                    break;
            }
        }
    }

    /// <summary>
    /// Makes sure that the data of the route holds the handle of the entity of every data segment of the route, shared
    /// with the data that already holds one for the same value.
    /// </summary>
    /// <param name="route">The route.</param>
    /// <param name="serviceProvider">The services that give the data maps.</param>
    /// <returns>The route with the handles in its data.</returns>
    public static Routing.Route WithEntityHandles(this Routing.Route route, IServiceProvider serviceProvider)
    {
        var data = route.Data;
        foreach (var step in route.Segments.OfType<DataSegmentInstance>())
        {
            if (step.DataSegment.DataMap is not { } mapType) continue;
            var map = (INavigationDataMap)serviceProvider.GetRequiredService(mapType);
            data = data.SetItem(step.DataSegment.Name, map.GetOrCreateEntity(data, step.DataSegment.Name, step.Primitive));
        }

        return ReferenceEquals(data, route.Data) ? route : route with { Data = data };
    }

    // Task<T>.Result is a well-known property that the trimmer preserves; the task is completed, so reading it does not wait.
    [UnconditionalSuppressMessage("Trimming", "IL2075",
        Justification = "Task<T>.Result is a well-known property that the trimmer preserves.")]
    private static bool TryGetTaskResult(Task task, [NotNullWhen(true)] out Type? resultType, [NotNullWhen(true)] out object? result)
    {
        // An async method returns a task of a private subclass of Task<T>
        for (var type = task.GetType(); type is not null; type = type.BaseType)
        {
            if (type.IsGenericType && type.GetGenericTypeDefinition() == genericTaskType)
            {
                resultType = type.GetGenericArguments()[0];
                result = type.GetProperty("Result")?.GetValue(task);
                return result is not null;
            }
        }

        resultType = null;
        result = null;
        return false;
    }
}
