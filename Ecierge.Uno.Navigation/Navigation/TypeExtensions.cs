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

internal static class TypeExtensions
{
    private static readonly Type ServiceProviderType = typeof(IServiceProvider);
    private static readonly Type WindowType = typeof(Window);
    private static readonly Type DispatcherType = typeof(DispatcherQueue);
    private static readonly Type NavigationScopeType = typeof(NavigationScope);
    private static readonly Type NameSegmentType = typeof(NameSegment);
    private static readonly Type NavigatorType = typeof(Navigator);

    public static ConstructorInfo? GetNavigationConstructor([NotNull] this Type type, IServiceProvider services, INavigationData navigationData, out object[] constructorArguments)
    {
        navigationData = navigationData ?? throw new ArgumentNullException(nameof(navigationData));

        var ctr = type.GetConstructors().FirstOrDefault();
        if (ctr is not null)
        {
            var paras = ctr.GetParameters();
            var args = new List<object>();
            var logger = services.GetRequiredService<ILoggerFactory>().CreateLogger("Ecierge.Uno.Navigation");
            var dataRegistry = services.GetRequiredService<INavigationDataRegistry>();
            foreach (var para in paras)
            {
                // TODO: Improve lookup performance
                if (dataRegistry.Items.Any(map => para.ParameterType.IsAssignableFrom(map.PrimitiveType)))
                {
                    if (navigationData.TryGetValue(para.Name!, out var data))
                    {
                        args.Add(data);
                        continue;
                    }
                }
                else if (para.ParameterType.GetGenericTypeDefinition() == typeof(Task<>))
                {
                    var entityType = para.ParameterType.GetGenericArguments().First();
                    var map = dataRegistry.Items.FirstOrDefault(map => entityType.IsAssignableFrom(map.EntityType));
                    if (map is not null)
                    {
                        var task = map.FromNavigationData(navigationData, para.Name!);
                        args.Add(TaskConverter.Convert(task, entityType));
                        continue;
                    }
                    else
                    {
                        logger.LogWarning("No data map found for parameter '{parameterName}' of type '{type}'", para.Name, para.ParameterType);
                    }
                }
                var arg = services.GetService(para.ParameterType);
                if (arg is null)
                {
                    logger.LogWarning("ViewModel constructor parameter '{parameterName}' of type '{type}' not found", para.Name, para.ParameterType);
                }
                args.Add(arg!);
            }
            constructorArguments = args.ToArray();
            return ctr;
        }

        constructorArguments = new object[] { };
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

}
