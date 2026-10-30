namespace Ecierge.Uno.Navigation;

using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;

/// <summary>
/// The handle of the entity that a data segment of a route stands for. All the pages below the segment share one
/// handle, so what one of them learns about the entity is seen by the others.
/// </summary>
/// <remarks>
/// <para>
/// A bare <see cref="Task{TResult}"/> that is stored in the data of a route keeps its failure for good: when the
/// server is down while the entity loads, every page below the segment gets the faulted task, also after the server is
/// back. A handle starts the load again when the last attempt faulted or was canceled and somebody asks for the entity,
/// and it can be reloaded at any time.
/// </para>
/// <para>
/// The handle does not start loading when it is created, so reading a route does not cost a request. The first
/// attempt starts when the entity is asked for.
/// </para>
/// </remarks>
public interface INavigationEntity
{
    /// <summary>
    /// Gets the type of the entity.
    /// </summary>
    Type EntityType { get; }

    /// <summary>
    /// Gets the value of the entity as it is written in a route, for example its id. It is available without loading
    /// the entity, and the entity can be loaded from it.
    /// </summary>
    string Primitive { get; }

    /// <summary>
    /// Gets a value that tells that the entity is loaded.
    /// </summary>
    bool IsLoaded { get; }

    /// <summary>
    /// Gets the task of the current attempt to load the entity, and starts the first attempt if there is none.
    /// </summary>
    Task LoadTask { get; }

    /// <summary>
    /// Gets the entity if it is loaded, without waiting for it and without starting a load.
    /// </summary>
    /// <param name="entity">The entity.</param>
    /// <returns><see langword="true"/> if the last attempt completed successfully with an entity.</returns>
    bool TryGetLoadedEntity([NotNullWhen(true)] out object? entity);

    /// <summary>
    /// Gets the task of the entity: the task of the current attempt, or of a new attempt if the last one faulted or
    /// was canceled.
    /// </summary>
    /// <returns>The task that completes when the entity is loaded.</returns>
    Task LoadAsync();

    /// <summary>
    /// Loads the entity again, whether or not it is loaded, and replaces the current attempt.
    /// </summary>
    /// <returns>The task of the new attempt.</returns>
    Task ReloadAsync();

    /// <summary>
    /// Raised when a new attempt replaces the current one, so that the pages that show the entity can read it again.
    /// </summary>
    event EventHandler? Changed;
}

/// <summary>
/// The handle of the entity that a data segment of a route stands for.
/// </summary>
/// <typeparam name="TEntity">The type of the entity.</typeparam>
public sealed class NavigationEntity<TEntity> : INavigationEntity
{
    private readonly Func<string, Task<TEntity>> loader;
    private readonly object gate = new();
    private Task<TEntity>? task;

    /// <summary>
    /// Creates a handle of an entity that is not loaded yet.
    /// </summary>
    /// <param name="primitive">The value of the entity as it is written in a route.</param>
    /// <param name="loader">Loads the entity from its value.</param>
    public NavigationEntity(string primitive, Func<string, Task<TEntity>> loader)
    {
        Primitive = primitive ?? throw new ArgumentNullException(nameof(primitive));
        this.loader = loader ?? throw new ArgumentNullException(nameof(loader));
    }

    /// <summary>
    /// Creates a handle of an entity that is loaded.
    /// </summary>
    /// <param name="primitive">The value of the entity as it is written in a route.</param>
    /// <param name="entity">The entity.</param>
    /// <param name="loader">Loads the entity from its value when it is reloaded.</param>
    public NavigationEntity(string primitive, TEntity entity, Func<string, Task<TEntity>> loader)
        : this(primitive, loader)
    {
        task = Task.FromResult(entity);
    }

    /// <summary>
    /// Creates a handle of an entity that is loaded by a task that is already running.
    /// </summary>
    /// <param name="primitive">The value of the entity as it is written in a route.</param>
    /// <param name="task">The task that loads the entity.</param>
    /// <param name="loader">Loads the entity from its value when it is reloaded.</param>
    public NavigationEntity(string primitive, Task<TEntity> task, Func<string, Task<TEntity>> loader)
        : this(primitive, loader)
    {
        this.task = task ?? throw new ArgumentNullException(nameof(task));
    }

    /// <inheritdoc/>
    public string Primitive { get; }

    /// <inheritdoc/>
    public Type EntityType => typeof(TEntity);

    /// <inheritdoc/>
    public event EventHandler? Changed;

    /// <inheritdoc/>
    public bool IsLoaded
    {
        get
        {
            lock (gate)
            {
                return task is { IsCompletedSuccessfully: true };
            }
        }
    }

    /// <inheritdoc cref="INavigationEntity.LoadTask"/>
    public Task<TEntity> LoadTask
    {
        get
        {
            lock (gate)
            {
                return task ??= StartLoad();
            }
        }
    }

    Task INavigationEntity.LoadTask => LoadTask;

    bool INavigationEntity.TryGetLoadedEntity([NotNullWhen(true)] out object? entity)
    {
        if (TryGetValue(out var value) && value is not null)
        {
            entity = value;
            return true;
        }

        entity = null;
        return false;
    }

    /// <summary>
    /// Gets the entity if it is loaded.
    /// </summary>
    /// <param name="value">The entity.</param>
    /// <returns><see langword="true"/> if the last attempt completed successfully.</returns>
    public bool TryGetValue([MaybeNullWhen(false)] out TEntity value)
    {
        lock (gate)
        {
            if (task is { IsCompletedSuccessfully: true } loaded)
            {
                value = loaded.Result;
                return true;
            }
        }

        value = default;
        return false;
    }

    /// <summary>
    /// Gets the task of the entity: the task of the current attempt, or of a new attempt if the last one faulted or
    /// was canceled.
    /// </summary>
    /// <returns>The task that completes when the entity is loaded.</returns>
    public Task<TEntity> GetAsync()
    {
        Task<TEntity> current;
        var restarted = false;
        lock (gate)
        {
            if (task is { IsFaulted: true } or { IsCanceled: true })
            {
                task = StartLoad();
                restarted = true;
            }

            current = task ??= StartLoad();
        }

        if (restarted) Changed?.Invoke(this, EventArgs.Empty);
        return current;
    }

    Task INavigationEntity.LoadAsync() => GetAsync();

    /// <summary>
    /// Loads the entity again, whether or not it is loaded, and replaces the current attempt.
    /// </summary>
    /// <returns>The task of the new attempt.</returns>
    public Task<TEntity> ReloadAsync()
    {
        Task<TEntity> reloaded;
        lock (gate)
        {
            reloaded = task = StartLoad();
        }

        Changed?.Invoke(this, EventArgs.Empty);
        return reloaded;
    }

    Task INavigationEntity.ReloadAsync() => ReloadAsync();

    private Task<TEntity> StartLoad()
    {
        try
        {
            return loader(Primitive);
        }
        catch (Exception ex)
        {
            // A loader that fails before it returns a task fails the attempt like one that fails later
            return Task.FromException<TEntity>(ex);
        }
    }

    /// <inheritdoc/>
    public override string ToString() => $"{typeof(TEntity).Name} {Primitive}";
}
