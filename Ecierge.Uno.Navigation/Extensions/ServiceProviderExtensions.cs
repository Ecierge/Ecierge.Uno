namespace Ecierge.Uno;

using System;

using Microsoft.Extensions.DependencyInjection;

public static class ServiceProviderExtensions
{
    #region Clone scoped instance

    internal static IServiceProvider CloneScopedInstance<T>(this IServiceProvider target, IServiceProvider source) where T : notnull
    {
        return target.AddScopedInstance(source.GetRequiredService<T>());
    }

    internal static IServiceProvider CloneScopedInstance(this IServiceProvider target, Type serviceType, IServiceProvider source)
    {
        return target.AddScopedInstance(source.GetRequiredService(serviceType));
    }

    internal static IServiceProvider TryCloneScopedInstance<T>(this IServiceProvider target, IServiceProvider source) where T : notnull
    {
        if (source.GetService<T>() is T service)
            return target.AddScopedInstance(service);
        else
            return target;
    }

    internal static IServiceProvider TryCloneScopedInstance(this IServiceProvider target, Type serviceType, IServiceProvider source)
    {
        var service = source.GetService(serviceType);
        if (service is not null)
            return target.AddScopedInstance(serviceType, service);
        else
            return target;
    }
    #endregion Clone scoped instance

    #region Add scoped instance
    public static IServiceProvider AddScopedInstance<T>(this IServiceProvider provider, Func<T> instanceCreator)
    {
        return provider.AddScopedInstance(typeof(T), instanceCreator);
    }

    public static IServiceProvider AddScopedInstance(this IServiceProvider provider, Type serviceType, Func<object> instanceCreator)
    {
        return provider.AddScopedInstance(serviceType, instanceCreator);
    }

    public static IServiceProvider AddScopedInstance<T>(this IServiceProvider provider, T instance)
    {
        return provider.AddScopedInstance(typeof(T), instance!);
    }

    public static IServiceProvider AddScopedInstance(this IServiceProvider provider, Type serviceType, object instance)
    {
        return provider.AddInstance<IScopedInstanceRepository>(serviceType, instance!);
    }
    #endregion Add scoped instance

    #region Add singleton instance
    public static IServiceProvider AddSingletonInstance<T>(this IServiceProvider provider, Func<T> instanceCreator)
    {
        return provider.AddSingletonInstance(typeof(T), instanceCreator);
    }

    public static IServiceProvider AddSingletonInstance(this IServiceProvider provider, Type serviceType, Func<object> instanceCreator)
    {
        return provider.AddSingletonInstance(serviceType, instanceCreator);
    }

    public static IServiceProvider AddSingletonInstance<T>(this IServiceProvider provider, T instance)
    {
        return provider.AddSingletonInstance(typeof(T), instance!);
    }

    public static IServiceProvider AddSingletonInstance(this IServiceProvider provider, Type serviceType, object instance)
    {
        return provider.AddInstance<ISingletonInstanceRepository>(serviceType, instance!);
    }
    #endregion Add singleton instance

    #region Add instance
    private static IServiceProvider AddInstance<TRepository>(this IServiceProvider provider, Type serviceType, object instance) where TRepository : IInstanceRepository
    {
        provider.GetRequiredService<TRepository>().AddInstance(serviceType, instance);
        return provider;
    }

    private static IInstanceRepository AddInstance(this IInstanceRepository repository, Type serviceType, object instance)
    {
        repository.Instances[serviceType] = instance;
        return repository;
    }
    #endregion Add instance

    #region Remove scoped instance
    public static IServiceProvider RemoveScopedInstance<T>(this IServiceProvider provider)
    {
        return provider.RemoveScopedInstance(typeof(T));
    }

    public static IServiceProvider RemoveScopedInstance(this IServiceProvider provider, Type serviceType)
    {
        return provider.RemoveInstance<IScopedInstanceRepository>(serviceType);
    }
    #endregion Remove scoped instance

    #region Remove singleton instance
    public static IServiceProvider RemoveSingletonInstance<T>(this IServiceProvider provider)
    {
        return provider.RemoveScopedInstance(typeof(T));
    }

    public static IServiceProvider RemoveSingletonInstance(this IServiceProvider provider, Type serviceType)
    {
        return provider.RemoveInstance<ISingletonInstanceRepository>(serviceType);
    }
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
    public static object? GetInstance(this IServiceProvider provider, Type type)
    {
        return provider.GetInstanceFromRepository<IScopedInstanceRepository>(type) ??
                provider.GetInstanceFromRepository<ISingletonInstanceRepository>(type);
    }

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
                repository.AddInstance(serviceType, instance);
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
                repository.AddInstance(typeof(T), instanceOfT);
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
