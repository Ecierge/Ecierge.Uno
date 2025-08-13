namespace Ecierge.Uno;

using System;

using Microsoft.Extensions.DependencyInjection;

public static class ServiceProviderExtensions
{
    #region Clone scoped instance

    internal static IServiceProvider CloneScopedInstance<T>(this IServiceProvider target, IServiceProvider source) where T : notnull
    {
        return target.SetScopedInstance(source.GetRequiredService<T>());
    }

    internal static IServiceProvider CloneScopedInstance(this IServiceProvider target, Type serviceType, IServiceProvider source)
    {
        return target.SetScopedInstance(source.GetRequiredService(serviceType));
    }

    internal static IServiceProvider TryCloneScopedInstance<T>(this IServiceProvider target, IServiceProvider source) where T : notnull
    {
        if (source.GetService<T>() is T service)
            return target.SetScopedInstance(service);
        else
            return target;
    }

    internal static IServiceProvider TryCloneScopedInstance(this IServiceProvider target, Type serviceType, IServiceProvider source)
    {
        var service = source.GetService(serviceType);
        if (service is not null)
            return target.SetScopedInstance(serviceType, service);
        else
            return target;
    }

    #endregion Clone scoped instance

    #region Set scoped instance

    /// <summary>
    /// Sets a scoped instance factory on the service provider (overwrites any existing one).
    /// </summary>
    /// <typeparam name="T">Type of the service to set.</typeparam>
    /// <param name="provider">The service provider to set the scoped instance on.</param>
    /// <param name="instanceCreator">A function that creates an instance of the service.</param>
    /// <returns>The updated service provider with the scoped instance set.</returns>
    public static IServiceProvider SetScopedInstance<T>(this IServiceProvider provider, Func<T> instanceCreator)
     =>
        provider.SetScopedInstance(typeof(T), instanceCreator);

    /// <summary>
    /// Sets a scoped instance factory on the service provider (overwrites any existing one).
    /// </summary>
    /// <param name="provider">The service provider to set the scoped instance on.</param>
    /// <param name="serviceType">The type of the service to set.</param>
    /// <param name="instanceCreator">A function that creates an instance of the service.</param>
    /// <returns>The updated service provider with the scoped instance set.</returns>
    public static IServiceProvider SetScopedInstance(this IServiceProvider provider, Type serviceType, Func<object> instanceCreator)
     =>
        provider.SetScopedInstance(serviceType, instanceCreator);

    /// <summary>
    /// Sets a scoped instance on the service provider (overwrites any existing one).
    /// </summary>
    /// <typeparam name="T">Type of the service to set.</typeparam>
    /// <param name="provider">The service provider to set the scoped instance on.</param>
    /// <param name="instance">The instance of the service to set.</param>
    /// <returns>The updated service provider with the scoped instance set.</returns>
    public static IServiceProvider SetScopedInstance<T>(this IServiceProvider provider, T instance)
     =>
        provider.SetScopedInstance(typeof(T), instance!);

    /// <summary>
    /// Sets a scoped instance on the service provider (overwrites any existing one).
    /// </summary>
    /// <param name="provider">The service provider to set the scoped instance on.</param>
    /// <param name="serviceType">The type of the service to set.</param>
    /// <param name="instance">The instance of the service to set.</param>
    /// <returns>The updated service provider with the scoped instance set.</returns>
    public static IServiceProvider SetScopedInstance(this IServiceProvider provider, Type serviceType, object instance)
     =>
        provider.SetInstance<IScopedInstanceRepository>(serviceType, instance!);

    #endregion Set scoped instance

    #region Set singleton instance

    /// <summary>
    /// Sets a singleton instance factory on the service provider (overwrites any existing one).
    /// </summary>
    /// <typeparam name="T">Type of the service to set.</typeparam>
    /// <param name="provider">The service provider to set the singleton instance on.</param>
    /// <param name="instanceCreator">A function that creates an instance of the service.</param>
    /// <returns>The updated service provider with the singleton instance set.</returns>
    public static IServiceProvider SetSingletonInstance<T>(this IServiceProvider provider, Func<T> instanceCreator)
     =>
        provider.SetSingletonInstance(typeof(T), instanceCreator);

    /// <summary>
    /// Sets a singleton instance factory on the service provider (overwrites any existing one).
    /// </summary>
    /// <param name="provider">The service provider to set the singleton instance on.</param>
    /// <param name="serviceType">The type of the service to set.</param>
    /// <param name="instanceCreator">A function that creates an instance of the service.</param>
    /// <returns>The updated service provider with the singleton instance set.</returns>
    public static IServiceProvider SetSingletonInstance(this IServiceProvider provider, Type serviceType, Func<object> instanceCreator)
     =>
        provider.SetSingletonInstance(serviceType, instanceCreator);

    /// <summary>
    /// Sets a singleton instance on the service provider (overwrites any existing one).
    /// </summary>
    /// <typeparam name="T">Type of the service to set.</typeparam>
    /// <param name="provider">The service provider to set the singleton instance on.</param>
    /// <param name="instance">The instance of the service to set.</param>
    /// <returns>The updated service provider with the singleton instance set.</returns>
    public static IServiceProvider SetSingletonInstance<T>(this IServiceProvider provider, T instance)
     =>
        provider.SetSingletonInstance(typeof(T), instance!);

    /// <summary>
    /// Sets a singleton instance on the service provider (overwrites any existing one).
    /// </summary>
    /// <param name="provider">The service provider to set the singleton instance on.</param>
    /// <param name="serviceType">The type of the service to set.</param>
    /// <param name="instance">The instance of the service to set.</param>
    /// <returns>The updated service provider with the singleton instance set.</returns>
    public static IServiceProvider SetSingletonInstance(this IServiceProvider provider, Type serviceType, object instance)
     =>
        provider.SetInstance<ISingletonInstanceRepository>(serviceType, instance!);

    #endregion Set singleton instance

    #region Set instance

    private static IServiceProvider SetInstance<TRepository>(this IServiceProvider provider, Type serviceType, object instance) where TRepository : IInstanceRepository
    {
        provider.GetRequiredService<TRepository>().SetInstance(serviceType, instance);
        return provider;
    }

    private static IInstanceRepository SetInstance(this IInstanceRepository repository, Type serviceType, object instance)
    {
        repository.Instances[serviceType] = instance;
        return repository;
    }

    #endregion Set instance

    #region Remove scoped instance

    /// <summary>
    /// Removes a scoped instance of the specified type from the service provider.
    /// </summary>
    /// <typeparam name="T">Type of the service to remove.</typeparam>
    /// <param name="provider">The service provider to remove the scoped instance from.</param>
    /// <returns>The updated service provider with the scoped instance removed.</returns>
    public static IServiceProvider RemoveScopedInstance<T>(this IServiceProvider provider)
     =>
        provider.RemoveScopedInstance(typeof(T));

    /// <summary>
    /// Removes a scoped instance of the specified type from the service provider.
    /// </summary>
    /// <param name="provider">The service provider to remove the scoped instance from.</param>
    /// <param name="serviceType">The type of the service to remove.</param>
    /// <returns>The updated service provider with the scoped instance removed.</returns>
    public static IServiceProvider RemoveScopedInstance(this IServiceProvider provider, Type serviceType)
     =>
        provider.RemoveInstance<IScopedInstanceRepository>(serviceType);

    #endregion Remove scoped instance

    #region Remove singleton instance

    /// <summary>
    /// Removes a singleton instance of the specified type from the service provider.
    /// </summary>
    /// <typeparam name="T">Type of the service to remove.</typeparam>
    /// <param name="provider">The service provider to remove the singleton instance from.</param>
    /// <returns>The updated service provider with the singleton instance removed.</returns>
    public static IServiceProvider RemoveSingletonInstance<T>(this IServiceProvider provider)
     =>
        provider.RemoveScopedInstance(typeof(T));

    /// <summary>
    /// Removes a singleton instance of the specified type from the service provider.
    /// </summary>
    /// <param name="provider">The service provider to remove the singleton instance from.</param>
    /// <param name="serviceType">The type of the service to remove.</param>
    /// <returns>The updated service provider with the singleton instance removed.</returns>
    public static IServiceProvider RemoveSingletonInstance(this IServiceProvider provider, Type serviceType)
     =>
        provider.RemoveInstance<ISingletonInstanceRepository>(serviceType);

    #endregion Remove singleton instance

    #region Remove instance

    private static IServiceProvider RemoveInstance<TRepository>(this IServiceProvider provider, Type serviceType) where TRepository : IInstanceRepository
    {
        provider.GetRequiredService<TRepository>().RemoveInstance(serviceType);
        return provider;
    }

    private static IInstanceRepository RemoveInstance(this IInstanceRepository repository, Type serviceType)
    {
        repository.Instances.Remove(serviceType);
        return repository;
    }

    #endregion Remove instance

    #region Get instance

    /// <summary>
    /// Gets an instance of the specified type from the service provider.
    /// </summary>
    /// <param name="provider">The service provider to get the instance from.</param>
    /// <param name="type">The type of the service to get.</param>
    /// <returns>The instance of the specified type, or null if not found.</returns>
    public static object? GetInstance(this IServiceProvider provider, Type type)
    {
        return provider.GetInstanceFromRepository<IScopedInstanceRepository>(type) ??
                provider.GetInstanceFromRepository<ISingletonInstanceRepository>(type);
    }

    /// <summary>
    /// Gets an instance of the specified type from the service provider.
    /// </summary>
    /// <typeparam name="T">Type of the service to get.</typeparam>
    /// <param name="provider">The service provider to get the instance from.</param>
    /// <returns>The instance of the specified type, or null if not found.</returns>
    public static T? GetInstance<T>(this IServiceProvider provider)
    {
        return provider.GetInstanceFromRepository<IScopedInstanceRepository, T>() ??
                provider.GetInstanceFromRepository<ISingletonInstanceRepository, T>();
    }

    #endregion Get instance

    #region Get instance from repository

    private static object? GetInstanceFromRepository<TRepository>(this IServiceProvider provider, Type serviceType)
        where TRepository : IInstanceRepository
    {
        return provider.GetRequiredService<TRepository>().GetRepositoryInstance(serviceType);
    }

    private static T? GetInstanceFromRepository<TRepository, T>(this IServiceProvider provider)
        where TRepository : IInstanceRepository
    {
        var singleton = provider.GetRequiredService<TRepository>().GetRepositoryInstance<T>();
        if (singleton is T singletonOfT)
        {
            return singletonOfT;
        }
        return default;
    }

    #endregion Get instance from repository

    #region Get instance from repository (internal)

    private static object? GetRepositoryInstance(this IInstanceRepository repository, Type serviceType)
    {
        var value = repository.Instances.TryGetValue(serviceType, out var repoValue) ? repoValue : null;
        if (value is Func<object> valueCreator)
        {
            var instance = valueCreator();
            if (instance.GetType().IsAssignableTo(serviceType))
            {
                repository.SetInstance(serviceType, instance);
            }
            return instance;
        }

        if (value?.GetType().IsAssignableTo(serviceType) ?? false)
        {
            return value;
        }

        return default;
    }

    private static T? GetRepositoryInstance<T>(this IInstanceRepository repository)
    {
        var value = repository.Instances.TryGetValue(typeof(T), out var repoValue) ? repoValue : null;
        if (value is Func<T> valueCreator)
        {
            var instance = valueCreator();
            if (instance is T instanceOfT)
            {
                repository.SetInstance(typeof(T), instanceOfT);
            }
            return instance;
        }

        if (value is T typedValue)
        {
            return typedValue;
        }

        return default;
    }

    #endregion Get instance from repository (internal)
}
