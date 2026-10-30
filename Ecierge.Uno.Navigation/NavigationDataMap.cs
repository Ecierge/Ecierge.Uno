namespace Ecierge.Uno.Navigation;

using System;
using System.Collections.Generic;
using System.Threading.Tasks;

/// <summary>
/// The values that a route is navigated with, by name.
/// </summary>
public class RouteValues : Dictionary<string, object> { }

/// <summary>
/// Tells the type of the entity that a data map handles.
/// </summary>
public interface INavigationDataMapEntityTypeInfo
{
    /// <summary>
    /// Gets the type of the entity that this map handles.
    /// </summary>
    static abstract Type EntityType { get; }
}

/// <summary>
/// Turns the value of a data segment of a route into an entity and back: an entity is written in a route as a short
/// value such as its id, and loaded from that value when the route is opened from a link.
/// </summary>
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
    /// neither an entity, nor a task of an entity.
    /// </exception>
    Task GetEntityTask(object obj);
    /// <summary>
    /// If data contains task returns itself,
    /// if data contains entity returns task from this entity,
    /// if data contains primitive value returns task that loads entity for it.
    /// </summary>
    /// <exception cref="NotSupportedException">
    /// Thrown when the value is not a <see cref="string"/>,
    /// neither an entity, nor a task of an entity.
    /// </exception>
    Task GetEntityTask(INavigationData data, string name);
    /// <summary>
    /// Retrieves the entity from the navigation data by name if it is there and loaded: the entity itself, the result
    /// of a task that succeeded, or the entity of a handle that is loaded.
    /// </summary>
    /// <returns>true if the loaded entity was found, false otherwise.</returns>
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

    /// <summary>
    /// Creates the handle of an entity that is loaded from its value when somebody asks for it.
    /// </summary>
    /// <param name="primitive">The value of the entity as it is written in a route.</param>
    /// <returns>The handle.</returns>
    INavigationEntity CreateEntity(string primitive);

    /// <summary>
    /// Creates the handle of an entity from what a caller gave: the entity, the task that loads it, its value as text,
    /// or a handle that is already made.
    /// </summary>
    /// <param name="value">The entity, the task, the text or the handle.</param>
    /// <param name="primitive">The value of the entity, needed only for a task, which does not know it.</param>
    /// <param name="data">The data of the route that the entity is part of, which the loader gets when the handle loads the entity again.</param>
    /// <returns>The handle.</returns>
    /// <exception cref="NotSupportedException">The value is none of the supported kinds.</exception>
    INavigationEntity CreateEntity(object value, string? primitive = null, INavigationData? data = null);

    /// <summary>
    /// Gets the handle that the data holds under the name, if it is the handle of the entity with the given value;
    /// otherwise creates a new handle for the value.
    /// </summary>
    /// <remarks>
    /// Every page below a data segment gets the handle that the segment first made, as long as the value is the same,
    /// so one reload of the entity is seen by all of them.
    /// </remarks>
    /// <param name="data">The data of the route.</param>
    /// <param name="name">The name of the data segment.</param>
    /// <param name="primitive">The value of the data segment.</param>
    /// <returns>The shared handle, or a new one.</returns>
    INavigationEntity GetOrCreateEntity(INavigationData data, string name, string primitive);

    /// <summary>
    /// Gets the handle of the entity that the data holds under the name. The data holds the handle, or the entity,
    /// or the value as text.
    /// </summary>
    /// <param name="data">The data of the route.</param>
    /// <param name="name">The name of the data segment.</param>
    /// <returns>The handle.</returns>
    /// <exception cref="KeyNotFoundException">The data has nothing under the name.</exception>
    /// <exception cref="NotSupportedException">The data holds something else.</exception>
    INavigationEntity GetEntity(INavigationData data, string name);
}

/// <summary>
/// Base class of the data maps. A data map is registered for one entity type, and loads the entity from its value.
/// </summary>
/// <typeparam name="TEntity">The type of the entity.</typeparam>
public abstract class NavigationDataMap<TEntity> : INavigationDataMap
{
    /// <summary>
    /// Gets the type of the entity that this map handles.
    /// </summary>
    public static Type EntityType { get; } = typeof(TEntity);
    Type INavigationDataMap.EntityType => EntityType;

#pragma warning disable CRR0030 // Redundant 'await'
    Task INavigationDataMap.LoadEntityAsync(INavigationData data, string name) => LoadEntityAsync(GetStringValue(data, name), data);
    Task INavigationDataMap.LoadEntityAsync(string primitive) => LoadEntityAsync(primitive);
#pragma warning restore CRR0030 // Redundant 'await'

    /// <summary>
    /// Loads the entity from its value.
    /// </summary>
    /// <param name="primitive">The value of the entity as it is written in a route.</param>
    /// <returns>The task that loads the entity.</returns>
    public abstract Task<TEntity> LoadEntityAsync(string primitive);

    /// <summary>
    /// Loads the entity from its value, knowing the data of the route that the value is part of. A map overrides it
    /// when the value alone does not find the entity, for example an asset is found by its id inside the property
    /// that the route names before it. The data holds the handles of the data segments above the entity, so the
    /// value of the property is read from its handle without waiting for the property to load. The default ignores
    /// the data.
    /// </summary>
    /// <param name="primitive">The value of the entity as it is written in a route.</param>
    /// <param name="data">The data of the route that the value is part of.</param>
    /// <returns>The task that loads the entity.</returns>
    public virtual Task<TEntity> LoadEntityAsync(string primitive, INavigationData data) => LoadEntityAsync(primitive);

    /// <summary>
    /// Writes the entity as a value that <see cref="LoadEntityAsync(string)"/> reads back.
    /// </summary>
    /// <param name="entity">The entity.</param>
    /// <returns>The value of the entity as it is written in a route.</returns>
    protected abstract string ToPrimitive(TEntity entity);

    string INavigationDataMap.GetStringValue(object obj) => obj switch
    {
        null => throw new ArgumentNullException(nameof(obj), "Value cannot be null."),
        NavigationEntity<TEntity> handle => handle.Primitive,
        TEntity entity => ToPrimitive(entity),
        string primitive => primitive,
        _ => throw new NotSupportedException($"Value of type {obj.GetType()} is not supported.")
    };

    /// <inheritdoc cref="INavigationDataMap.GetStringValue(INavigationData, string)"/>
    public string GetStringValue(INavigationData data, string name)
    {
        data = data ?? throw new System.ArgumentNullException(nameof(data));
        if (data.TryGetValue(name, out var value))
        {
            return value switch
            {
                NavigationEntity<TEntity> handle => handle.Primitive,
                TEntity obj => ToPrimitive(obj),
                string primitive => primitive,
                _ => throw new NotSupportedException($"Value of type {value.GetType()} is not supported for {name}.")
            };
        }
        throw new KeyNotFoundException($"No value found for {name} in navigation data.");
    }

    bool INavigationDataMap.TryGetEntity(INavigationData data, string name, out object? value)
    {
        if (TryGetEntity(data, name, out TEntity? entity) && entity is not null)
        {
            value = entity;
            return true;
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
                case NavigationEntity<TEntity> handle:
                    value = handle.GetAsync();
                    return true;

                case TEntity entity:
                    value = Task.FromResult(entity);
                    return true;

                case Task task when task is Task<TEntity> typedTask:
                    value = typedTask;
                    return true;
            }
        }
        value = null;
        return false;
    }

    /// <summary>
    /// Gets the entity from the navigation data by name if it is there and loaded.
    /// </summary>
    /// <param name="data">The data of the route.</param>
    /// <param name="name">The name of the data segment.</param>
    /// <param name="value">The entity.</param>
    /// <returns><see langword="true"/> if the data holds the loaded entity under the name.</returns>
    public bool TryGetEntity(INavigationData data, string name, out TEntity? value)
    {
        data = data ?? throw new ArgumentNullException(nameof(data));
        if (data.TryGetValue(name, out var obj))
        {
            switch (obj)
            {
                case NavigationEntity<TEntity> handle:
                    return handle.TryGetValue(out value);
                case TEntity entity:
                    value = entity;
                    return true;
                case Task<TEntity> task when task.IsCompletedSuccessfully:
                    value = task.Result;
                    return true;
                case Task<TEntity>:
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

    /// <summary>
    /// Gets the task of the entity from the navigation data by name. For a handle it is the task of the entity, which
    /// is a new attempt if the last one failed.
    /// </summary>
    /// <param name="data">The data of the route.</param>
    /// <param name="name">The name of the data segment.</param>
    /// <param name="value">The task.</param>
    /// <returns><see langword="true"/> if the data holds the entity, its handle or its task under the name.</returns>
    public bool TryGetEntityTask(INavigationData data, string name, out Task<TEntity>? value)
    {
        data = data ?? throw new ArgumentNullException(nameof(data));
        if (data.TryGetValue(name, out var obj))
        {
            switch (obj)
            {
                case NavigationEntity<TEntity> handle:
                    value = handle.GetAsync();
                    return true;
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
        NavigationEntity<TEntity> handle => handle.GetAsync(),
        TEntity entity => Task.FromResult(entity),
        Task<TEntity> task => task,
        string primitive => LoadEntityAsync(primitive),
        _ => throw new NotSupportedException($"Value of type {obj.GetType()} is not supported.")
    };

    /// <inheritdoc cref="INavigationDataMap.GetEntityTask(INavigationData, string)"/>
    public Task GetEntityTask(INavigationData data, string name)
    {
        data = data ?? throw new ArgumentNullException(nameof(data));
        if (data.TryGetValue(name, out var obj))
        {
            return obj switch
            {
                NavigationEntity<TEntity> handle => handle.GetAsync(),
                TEntity entity => Task.FromResult(entity),
                Task<TEntity> task => task,
                string primitive => LoadEntityAsync(primitive, data),
                _ => throw new NotSupportedException($"Value of type {obj.GetType()} is not supported for {name}.")
            };
        }
        throw new KeyNotFoundException($"No value found for {name} in navigation data.");
    }

    /// <summary>
    /// Creates the handle of an entity that is loaded from its value when somebody asks for it.
    /// </summary>
    /// <param name="primitive">The value of the entity as it is written in a route.</param>
    /// <returns>The handle.</returns>
    public NavigationEntity<TEntity> CreateEntity(string primitive) => new(primitive, LoadEntityAsync);

    /// <summary>
    /// Creates the handle of an entity that is loaded.
    /// </summary>
    /// <param name="entity">The entity.</param>
    /// <returns>The handle, which reloads the entity from its value.</returns>
    public NavigationEntity<TEntity> CreateEntity(TEntity entity) => new(ToPrimitive(entity), entity, LoadEntityAsync);

    /// <summary>
    /// Creates the handle of an entity that is being loaded by a task.
    /// </summary>
    /// <param name="task">The task that loads the entity.</param>
    /// <param name="primitive">The value of the entity as it is written in a route.</param>
    /// <returns>The handle, which loads the entity again from its value if the task failed.</returns>
    public NavigationEntity<TEntity> CreateEntity(Task<TEntity> task, string primitive) => new(primitive, task, LoadEntityAsync);

    /// <summary>
    /// Creates the handle of an entity that is loaded from its value when somebody asks for it, by the loader that
    /// gets the data of the route.
    /// </summary>
    /// <param name="primitive">The value of the entity as it is written in a route.</param>
    /// <param name="data">The data of the route that the entity is part of.</param>
    /// <returns>The handle.</returns>
    public NavigationEntity<TEntity> CreateEntity(string primitive, INavigationData data)
     => new(primitive, value => LoadEntityAsync(value, data));

    /// <summary>
    /// Creates the handle of an entity that is loaded, which reloads it by the loader that gets the data of the route.
    /// </summary>
    /// <param name="entity">The entity.</param>
    /// <param name="data">The data of the route that the entity is part of.</param>
    /// <returns>The handle.</returns>
    public NavigationEntity<TEntity> CreateEntity(TEntity entity, INavigationData data)
     => new(ToPrimitive(entity), entity, value => LoadEntityAsync(value, data));

    /// <summary>
    /// Creates the handle of an entity that is being loaded by a task, which loads it again by the loader that gets
    /// the data of the route if the task failed.
    /// </summary>
    /// <param name="task">The task that loads the entity.</param>
    /// <param name="primitive">The value of the entity as it is written in a route.</param>
    /// <param name="data">The data of the route that the entity is part of.</param>
    /// <returns>The handle.</returns>
    public NavigationEntity<TEntity> CreateEntity(Task<TEntity> task, string primitive, INavigationData data)
     => new(primitive, task, value => LoadEntityAsync(value, data));

    INavigationEntity INavigationDataMap.CreateEntity(string primitive) => CreateEntity(primitive);

    INavigationEntity INavigationDataMap.CreateEntity(object value, string? primitive, INavigationData? data) => value switch
    {
        null => throw new ArgumentNullException(nameof(value)),
        NavigationEntity<TEntity> handle => handle,
        TEntity entity => data is null ? CreateEntity(entity) : CreateEntity(entity, data),
        Task<TEntity> task => CreateTaskEntity(
            task,
            primitive ?? throw new ArgumentException("A task does not know the value of its entity, so the value is required.", nameof(primitive)),
            data),
        string text => data is null ? CreateEntity(text) : CreateEntity(text, data),
        _ => throw new NotSupportedException($"Value of type {value.GetType()} is not supported.")
    };

    private NavigationEntity<TEntity> CreateTaskEntity(Task<TEntity> task, string primitive, INavigationData? data)
     => data is null ? CreateEntity(task, primitive) : CreateEntity(task, primitive, data);

    /// <inheritdoc cref="INavigationDataMap.GetOrCreateEntity(INavigationData, string, string)"/>
    public NavigationEntity<TEntity> GetOrCreateEntity(INavigationData data, string name, string primitive)
    {
        data = data ?? throw new ArgumentNullException(nameof(data));
        if (data.TryGetValue(name, out var existing))
        {
            switch (existing)
            {
                case NavigationEntity<TEntity> handle when SamePrimitive(handle.Primitive, primitive):
                    return handle;
                case TEntity entity when SamePrimitive(ToPrimitive(entity), primitive):
                    return CreateEntity(entity, data);
                case Task<TEntity> { IsCompletedSuccessfully: true } task when SamePrimitive(ToPrimitive(task.Result), primitive):
                    return CreateEntity(task.Result, data);
            }
        }

        // The handle remembers the data of the route, so that whenever it loads again it finds an entity that lives
        // inside a parent from the value alone
        return CreateEntity(primitive, data);
    }

    INavigationEntity INavigationDataMap.GetOrCreateEntity(INavigationData data, string name, string primitive)
     => GetOrCreateEntity(data, name, primitive);

    INavigationEntity INavigationDataMap.GetEntity(INavigationData data, string name)
    {
        data = data ?? throw new ArgumentNullException(nameof(data));
        if (!data.TryGetValue(name, out var obj))
            throw new KeyNotFoundException($"No value found for {name} in navigation data.");

        return obj switch
        {
            NavigationEntity<TEntity> handle => handle,
            TEntity entity => CreateEntity(entity, data),
            string primitive => CreateEntity(primitive, data),
            Task<TEntity> { IsCompletedSuccessfully: true } task => CreateEntity(task.Result, data),
            _ => throw new NotSupportedException($"Value of type {obj.GetType()} is not supported for {name}.")
        };
    }

    private static bool SamePrimitive(string a, string b) => string.Equals(a, b, StringComparison.OrdinalIgnoreCase);
}
