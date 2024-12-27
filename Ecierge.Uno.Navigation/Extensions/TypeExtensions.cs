namespace Ecierge.Uno.Navigation;

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.UI.Dispatching;

internal static class TypeExtensions
{
    private static readonly Type ServiceProviderType = typeof(IServiceProvider);
    private static readonly Type WindowType = typeof(Window);
    private static readonly Type DispatcherType = typeof(DispatcherQueue);
    private static readonly Type NavigationScopeType = typeof(NavigationScope);
    private static readonly Type NameSegmentType = typeof(NameSegment);
    private static readonly Type NavigatorType = typeof(Navigator);

    public static ConstructorInfo? GetNavigationConstructor([NotNull] this Type type, IServiceProvider services, INavigationData navigationData, out object?[] constructorArguments)
    {
        navigationData = navigationData ?? throw new ArgumentNullException(nameof(navigationData));

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
                if (dataRegistry.HasAssignablePrimitive(para.ParameterType))
                {
                    if (navigationData.TryGetValue(para.Name!, out var data))
                    {
                        if (para.ParameterType == data.GetType())
                        {
                            args.Add(data);
                            continue;
                        }
                        logger.LogWarning("Navigation data item found for parameter '{parameterName}' of type '{type}' does not match the expected type '{expectedType}'", para.Name, data.GetType(), para.ParameterType);
                        Debugger.Break();
                    }
                }
                else if (para.ParameterType.IsGenericType && para.ParameterType.GetGenericTypeDefinition() == typeof(Task<>))
                {
                    var entityType = para.ParameterType.GetGenericArguments().First();
                    if (dataRegistry.TryGetForAssignableEntity(entityType, out var dataMapType))
                    {
                        // TODO: Ensure that navigation item mapping resolution happens only once
                        var map = (INavigationDataMap)services.GetRequiredService(dataMapType);
                        if (map.HasValue(navigationData, para.Name!))
                        {
                            var task = map.FromNavigationData(navigationData, para.Name!);
                            args.Add(TaskConverter.Convert(task, entityType));
                        }
                        else
                        {
                            if (para.IsOptional)
                                args.Add(GetDefaultParameterValue(para));
                            else
                            {
                                logger.LogWarning("No navigation data item found for mandatory parameter '{parameterName}' of type '{type}'", para.Name, para.ParameterType);
                                constructorArguments = Array.Empty<object?>();
                                return null;
                            }
                        }
                        continue;
                    }
                    else
                    {
                        logger.LogWarning("No data map found for parameter '{parameterName}' of type '{type}'", para.Name, para.ParameterType);
                    }
                }
                var key = para.GetCustomAttribute<FromKeyedServicesAttribute>()?.Key;
                object? arg;
                if (key is not null)
                {
                    arg = services.GetKeyedServices(para.ParameterType, key).FirstOrDefault();
                }
                else
                {
                    arg = services.GetService(para.ParameterType);
                }
                if (arg is null)
                {
                    if (para.IsOptional)
                    {
                        args.Add(GetDefaultParameterValue(para));
                        continue;
                    }
                    else
                    {
                        logger.LogWarning("No service found for mandatory parameter '{parameterName}' of type '{type}'", para.Name, para.ParameterType);
                        constructorArguments = Array.Empty<object?>();
                        return null;
                    }
                }
                args.Add(arg!);
            }
            constructorArguments = args.ToArray();
            return ctr;
        }

        constructorArguments = Array.Empty<object?>();
        return null;
    }

    public class TaskConverter
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
