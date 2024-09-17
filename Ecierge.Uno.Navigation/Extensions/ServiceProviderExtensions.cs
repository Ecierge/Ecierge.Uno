namespace Ecierge.Uno;

using System;

using Microsoft.Extensions.DependencyInjection;

public static class ServiceProviderExtensions
{
    internal static IServiceProvider CloneScopedInstance<T>(this IServiceProvider target, IServiceProvider source) where T : notnull
    {
        return target.AddScopedInstance(source.GetRequiredService<T>());
    }

    internal static IServiceProvider CloneScopedInstance(this IServiceProvider target, Type type, IServiceProvider source)
    {
        return target.AddScopedInstance(source.GetRequiredService(type));
    }

    internal static IServiceProvider TryCloneScopedInstance<T>(this IServiceProvider target, IServiceProvider source) where T : notnull
    {
        if (source.GetService<T>() is T service)
            return target.AddScopedInstance(service);
        else
            return target;
    }

    internal static IServiceProvider TryCloneScopedInstance(this IServiceProvider target, Type type, IServiceProvider source)
    {
        var service = source.GetService(type);
        if (service is not null)
            return target.AddScopedInstance(type, service);
        else
            return target;
    }

    public static IServiceProvider AddScopedInstance<T>(this IServiceProvider provider, Func<T> instanceCreator)
    {
        return provider.AddScopedInstance(typeof(T), instanceCreator);
    }

    public static IServiceProvider AddScopedInstance<T>(this IServiceProvider provider, T instance)
    {
        return provider.AddScopedInstance(typeof(T), instance!);
    }

    public static IServiceProvider AddScopedInstance(this IServiceProvider provider, Type serviceType, object instance)
    {
        return provider.AddInstance<IScopedInstanceRepository>(serviceType, instance!);
    }

    public static IServiceProvider AddSingletonInstance<T>(this IServiceProvider provider, Func<T> instanceCreator)
    {
        return provider.AddSingletonInstance(typeof(T), instanceCreator);
    }

    public static IServiceProvider AddSingletonInstance<T>(this IServiceProvider provider, T instance)
    {
        return provider.AddSingletonInstance(typeof(T), instance!);
    }

    public static IServiceProvider AddSingletonInstance(this IServiceProvider provider, Type serviceType, object instance)
    {
        return provider.AddInstance<ISingletonInstanceRepository>(serviceType, instance!);
    }

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

    public static IServiceProvider RemoveScopedInstance<T>(this IServiceProvider provider)
    {
        return provider.RemoveScopedInstance(typeof(T));
    }

    public static IServiceProvider RemoveScopedInstance(this IServiceProvider provider, Type serviceType)
    {
        return provider.RemoveInstance<IScopedInstanceRepository>(serviceType);
    }

    public static IServiceProvider RemoveSingletonInstance<T>(this IServiceProvider provider)
    {
        return provider.RemoveScopedInstance(typeof(T));
    }

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

    public static T? GetInstance<T>(this IServiceProvider provider)
    {
        return provider.GetInstance<IScopedInstanceRepository, T>() ??
                provider.GetInstance<ISingletonInstanceRepository, T>();
    }

    private static T? GetInstance<TRepository, T>(this IServiceProvider provider)
        where TRepository : IInstanceRepository
    {
        var singleton = provider.GetRequiredService<TRepository>().GetRepositoryInstance<T>();
        if (singleton is T singletonOfT)
        {
            return singletonOfT;
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
}
