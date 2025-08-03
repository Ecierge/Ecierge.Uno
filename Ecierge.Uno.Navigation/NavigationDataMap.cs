namespace Ecierge.Uno.Navigation;

public class RouteValues : Dictionary<string, object> { }

public interface INavigationDataMapEntityTypeInfo
{
    /// <summary>
    /// Gets the type of the entity that this map handles.
    /// </summary>
    static abstract Type EntityType { get; }
}

public interface INavigationDataMap : INavigationDataMapEntityTypeInfo
{
    /// <summary>
    /// Gets the type of the entity that this map handles.
    /// </summary>
    new abstract Type EntityType { get; }
    /// <summary>
    /// Retrieves the primitive <see cref="string"/> value from the entity.
    /// </summary>
    /// <param name="entity">Entity to get the value from.</param>
    string GetStringValue(object entity);
    /// <summary>
    /// Retrieves the primitive <see cref="string"/> value from the navigation data by name.
    /// If navigation data contains entity, it gets the primitive value from it.
    /// </summary>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="data"/> is null.
    /// </exception>
    string GetStringValue(INavigationData data, string name);
    /// <summary>
    /// If task provided returns itself,
    /// if entity provided returns task from this entity,
    /// if primitive value provided returns task that loads entity for it.
    /// </summary>
    /// <exception cref="NotSupportedException">
    /// Thrown when <paramref name="obj"/> is not a <see cref="string"/>,
    /// neither <see cref="TEntity"/>, nor <see cref="Task{TEntity}"/>.
    /// </exception>
    Task GetEntityTask(object obj);
    /// <summary>
    /// If data contains task returns itself,
    /// if data contains entity returns task from this entity,
    /// if data contains primitive value returns task that loads entity for it.
    /// </summary>
    /// <exception cref="NotSupportedException">
    /// Thrown when <paramref name="obj"/> is not a <see cref="string"/>,
    /// neither <see cref="TEntity"/>, nor <see cref="Task{TEntity}"/>.
    /// </exception>
    Task GetEntityTask(INavigationData data, string name);
    /// <summary>
    /// Retrieves the primitive value from the navigation data by name if present,
    /// or gets it from task if task succeeded.
    /// </summary>
    /// <returns>true if value was found, false otherwise.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="data"/> is null.
    /// </exception>
    bool TryGetEntity(INavigationData data, string name, out object? value);
    /// <summary>
    /// Retrieves the entity task from the navigation data by name if present.
    /// </summary>
    /// <returns>true if value was found, false otherwise.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="data"/> is null.
    /// </exception>
    bool TryGetEntityTask(INavigationData data, string name, out Task? value);
    /// <summary>
    /// Loads the entity for the primitive <see cref="string"/> value from the data store.
    /// </summary>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="data"/> is null.
    /// </exception>
    Task LoadEntityAsync(string primitive);
    /// <summary>
    /// Retrieves the primitive value from the navigation data by name,
    /// and loads the entity for it from the data store.
    /// </summary>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="data"/> is null.
    /// </exception>
    Task LoadEntityAsync(INavigationData data, string name);
}

public abstract class NavigationDataMap<TEntity> : INavigationDataMap
{
    public static Type EntityType { get; } = typeof(TEntity);
    Type INavigationDataMap.EntityType => EntityType;

#pragma warning disable CRR0030 // Redundant 'await'
    Task INavigationDataMap.LoadEntityAsync(INavigationData data, string name) => LoadEntityAsync(GetStringValue(data, name));
    async Task INavigationDataMap.LoadEntityAsync(string primitive) => await LoadEntityAsync(primitive);
#pragma warning restore CRR0030 // Redundant 'await'

    public abstract Task<TEntity> LoadEntityAsync(string primitive);
    protected abstract string ToPrimitive(TEntity entity);

    string INavigationDataMap.GetStringValue(object obj) => obj switch
    {
        null => throw new ArgumentNullException(nameof(obj), "Value cannot be null."),
        TEntity entity => ToPrimitive(entity),
        string primitive => primitive,
        _ => throw new NotSupportedException($"Value of type {obj.GetType()} is not supported.")
    };

    public string GetStringValue(INavigationData data, string name)
    {
        data = data ?? throw new System.ArgumentNullException(nameof(data));
        if (data.TryGetValue(name, out var value))
        {
            return value switch
            {
                TEntity obj => ToPrimitive(obj),
                string primitive => primitive,
                _ => throw new NotSupportedException($"Value of type {value.GetType()} is not supported for {name}.")
            };
        }
        throw new KeyNotFoundException($"No value found for {name} in navigation data.");
    }

    bool INavigationDataMap.TryGetEntity(INavigationData data, string name, out object? value)
    {
        data = data ?? throw new ArgumentNullException(nameof(data));
        if (data.TryGetValue(name, out var obj))
        {
            switch (obj)
            {
                case TEntity entity:
                    value = ToPrimitive(entity);
                    return true;
                case Task<TEntity> task when task.IsCompletedSuccessfully:
                    value = ToPrimitive(task.Result);
                    return true;
                case Task<TEntity> task:
                    value = task;
                    return true;
            }
        }
        value = null;
        return false;
    }

    bool INavigationDataMap.TryGetEntityTask(INavigationData data, string name, out Task? value)
    {
        data = data ?? throw new ArgumentNullException(nameof(data));
        if (data.TryGetValue(name, out var obj))
        {
            switch (obj)
            {
                case TEntity entity:
                    value = Task.FromResult(entity);
                    return true;
                case Task<TEntity> task:
                    value = task;
                    return true;
            }
        }
        value = null;
        return false;
    }

    public bool TryGetEntity(INavigationData data, string name, out TEntity? value)
    {
        data = data ?? throw new ArgumentNullException(nameof(data));
        if (data.TryGetValue(name, out var obj))
        {
            switch (obj)
            {
                case TEntity entity:
                    value = entity;
                    return true;
                case Task<TEntity> task when task.IsCompletedSuccessfully:
                    value = task.Result;
                    return true;
                case string:
                    value = default;
                    return false;
                case null:
                    value = default;
                    return true;
                default:
                    throw new NotSupportedException($"Value of type {obj.GetType()} is not supported for {name}.");
            }
        }
        value = default;
        return false;
    }

    public bool TryGetEntityTask(INavigationData data, string name, out Task<TEntity>? value)
    {
        data = data ?? throw new ArgumentNullException(nameof(data));
        if (data.TryGetValue(name, out var obj))
        {
            switch (obj)
            {
                case TEntity entity:
                    value = Task.FromResult(entity);
                    return true;
                case Task<TEntity> task:
                    value = task;
                    return true;
                case string:
                    value = default;
                    return false;
                case null:
                    value = default;
                    return true;
                default:
                    throw new NotSupportedException($"Value of type {obj.GetType()} is not supported for {name}.");
            }
        }
        value = default;
        return false;
    }

    Task INavigationDataMap.GetEntityTask(object obj) => obj switch
    {
        null => throw new ArgumentNullException(nameof(obj), "Value cannot be null."),
        TEntity entity => Task.FromResult(entity),
        Task<TEntity> task => task,
        string primitive => LoadEntityAsync(primitive),
        _ => throw new NotSupportedException($"Value of type {obj.GetType()} is not supported.")
    };

    public Task GetEntityTask(INavigationData data, string name)
    {
        data = data ?? throw new ArgumentNullException(nameof(data));
        if (data.TryGetValue(name, out var obj))
        {
            return obj switch
            {
                TEntity entity => Task.FromResult(entity),
                Task<TEntity> task => task,
                string primitive => LoadEntityAsync(primitive),
                _ => throw new NotSupportedException($"Value of type {obj.GetType()} is not supported for {name}.")
            };
        }
        throw new KeyNotFoundException($"No value found for {name} in navigation data.");
    }
}
