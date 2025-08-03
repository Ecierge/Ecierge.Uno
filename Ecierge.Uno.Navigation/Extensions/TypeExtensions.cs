namespace Ecierge.Uno.Navigation;

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.UI.Dispatching;

/// <summary>
/// Attribute to specify navigation parameter name which should be bound to the parameter.
/// </summary>
[AttributeUsage(AttributeTargets.Parameter, AllowMultiple = true)]
public class NavigationParameterAttribute : Attribute
{
    /// <summary>
    /// The name of the navigation parameter to bind to the parameter this attribute is applied to.
    /// </summary>
    public string ParameterName { get; }
    /// <summary>
    /// Initializes a new instance of the <see cref="NavigationParameterAttribute"/> class with the specified parameter name.
    /// </summary>
    /// <param name="parameterName">The name of the navigation parameter to bind to the parameter this attribute is applied to.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="parameterName"/> is null.</exception>
    public NavigationParameterAttribute(string parameterName)
    {
        ParameterName = parameterName ?? throw new ArgumentNullException(nameof(parameterName));
    }
}

internal static class TypeExtensions
{
    private static readonly Type ServiceProviderType = typeof(IServiceProvider);
    private static readonly Type WindowType = typeof(Window);
    private static readonly Type DispatcherType = typeof(DispatcherQueue);
    private static readonly Type NavigationScopeType = typeof(NavigationScope);
    private static readonly Type NameSegmentType = typeof(NameSegment);
    private static readonly Type NavigatorType = typeof(Navigator);

    /// <summary>
    /// Creates an instance of the specified service type using the provided service provider.
    /// </summary>
    /// <typeparam name="TService">The type of the service to create.</typeparam>
    /// <exception cref="InvalidOperationException">
    /// Thrown if the service provider does not implement <see cref="IKeyedServiceProvider"/> or if no suitable constructor is found.
    /// </exception>
    public static TService CreateWithNavigationParameters<TService>([NotNull] IServiceProvider serviceProvider)
        where TService : class
    {
        var ksp = serviceProvider as IKeyedServiceProvider ?? throw new InvalidOperationException("Service provider must implement IKeyedServiceProvider to create instances with navigation parameters.");
        var type = typeof(TService);
        var constructorInfo = GetNavigationConstructor(type, ksp);
        if (constructorInfo is null)
            throw new InvalidOperationException($"No suitable constructor found for type '{type.FullName}' with navigation parameters.");
        var (constructor, parameters) = constructorInfo.Value;
        return (TService)constructor.Invoke(parameters);
    }

    /// <summary>
    /// Creates a factory function that can be used to create instances of the specified type with navigation parameters.
    /// </summary>
    /// <param name="type">The type to create an instance of.</param>
    /// <returns>A function that takes an <see cref="IServiceProvider"/> and returns an instance of the specified type.</returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown if the service provider does not implement <see cref="IKeyedServiceProvider"/> or if no suitable constructor is found.
    /// </exception>
    public static Func<IServiceProvider, object> GetFactoryWithNavigationParameters([NotNull] Type type) =>
        (sp) =>
        {
            var ksp = sp as IKeyedServiceProvider ?? throw new InvalidOperationException("Service provider must implement IKeyedServiceProvider to create instances with navigation parameters.");
            var constructorInfo = GetNavigationConstructor(type, ksp);
            if (constructorInfo is null)
                throw new InvalidOperationException($"No suitable constructor found for type '{type.FullName}' with navigation parameters.");
            var (constructor, parameters) = constructorInfo.Value;
            return constructor.Invoke(parameters);
        };

    public static (ConstructorInfo Constructor, object?[] Parameters)? GetNavigationConstructor([NotNull] this Type type, IKeyedServiceProvider services)
    {
        INavigationData navigationData = services.GetRequiredService<INavigationData>();

        var ctr = type.GetConstructors().FirstOrDefault();
        if (ctr is not null)
        {
            var logger = services.GetRequiredService<ILoggerFactory>().CreateLogger("Ecierge.Uno.Navigation");
            object? GetDefaultParameterValue(ParameterInfo parameter)
            {
                if (parameter.DefaultValue is not Missing)
                {
                    return parameter.DefaultValue;
                }
                logger.LogWarning("No data found for parameter '{parameterName}' of type '{type}'", parameter.Name, parameter.ParameterType);
                if (parameter.ParameterType.IsValueType)
                    return Activator.CreateInstance(parameter.ParameterType);
                else
                    return null;
            }

            var paras = ctr.GetParameters();
            var args = new List<object?>(paras.Length);
            var dataRegistry = services.GetRequiredService<INavigationDataRegistry>();
            foreach (var para in paras)
            {
                // 1. Try keyed services
                var serviceKey = para.GetCustomAttribute<FromKeyedServicesAttribute>()?.Key;
                if (serviceKey is not null && services.GetKeyedService(para.ParameterType, serviceKey) is { } keyedValue)
                {
                    args.Add(keyedValue);
                    continue;
                }
                // 2. Try general services
                else if (services.GetService(para.ParameterType) is { } value)
                {
                    args.Add(value);
                    continue;
                }

                var navigationParameterName =
                    para.CustomAttributes
                        .Where(attr => attr.AttributeType == typeof(NavigationParameterAttribute))
                        .Select(attr => attr.ConstructorArguments.First().Value?.ToString())
                        .Append(para.Name)
                        .Where(name => name is not null)
                        .Where(name => navigationData.ContainsKey(name!))
                        .FirstOrDefault();
                if (navigationParameterName is not null)
                {
                    // 3. Try get entity from navigation data
                    if (dataRegistry.TryGetForAssignableEntity(para.ParameterType, out var dataMapType))
                    {
                        var map = (INavigationDataMap)services.GetRequiredService(dataMapType);
                        if (map.TryGetEntity(navigationData, navigationParameterName, out var value))
                        {
                            args.Add(value);
                            continue;
                        }
                    }
                    // 4. Try get entity task from navigation data
                    var parameterType = para.ParameterType;
                    if (parameterType.IsGenericType && parameterType.GetGenericTypeDefinition() == typeof(Task<>))
                    {
                        var entityType = parameterType.GetGenericArguments().First();
                        if (dataRegistry.TryGetForAssignableEntity(entityType, out dataMapType))
                        {
                            var map = (INavigationDataMap)services.GetRequiredService(dataMapType);
                            if (map.TryGetEntityTask(navigationData, navigationParameterName, out var task))
                            {
                                args.Add(task);
                                continue;
                            }
                        }
                    }
                    // TODO: Consider injecting primitives
                    // 5. Try string primitive
                    //if (para.ParameterType == typeof(string))
                    //{
                    //    if (navigationData.TryGetValue(navigationParameterName, out var value))
                    //    {
                    //        args.Add(value);
                    //        continue;
                    //    }
                    //}
                }
                // 6. Get default value if optional
                if (para.IsOptional)
                    args.Add(GetDefaultParameterValue(para));
                else
                {
                    logger.LogWarning("No service or navigation data item found for mandatory parameter '{parameterName}' of type '{type}'", para.Name, para.ParameterType);
                    return null;
                }
            }
            return (ctr, args.ToArray());
        }

        return null;
    }

    internal class TaskConverter
    {
        static MethodInfo castMethod = typeof(TaskConverter).GetMethod(nameof(Cast), BindingFlags.NonPublic | BindingFlags.Static)!;

        public static object Convert(Task<object> task, Type targetType)
        {
            MethodInfo genericCastMethod = castMethod.MakeGenericMethod(targetType);
            return genericCastMethod.Invoke(null, new object[] { task })!;
        }

        private static async Task<T> Cast<T>(Task<object> obj)
        {
            var result = await obj;
            return (T)result;
        }
    }

    internal static IEnumerable<Type> GetBaseTypes(this Type type)
    {
        var previousType = type;
        while (true)
        {
            var baseType = previousType.BaseType;
            if (baseType is null || baseType.FullName == previousType.FullName)
            {
                yield break;
            }
            else
            {
                yield return baseType;
                previousType = baseType;
            }
        }
    }
}
