namespace Ecierge.Uno.Navigation;

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;

using Ecierge.Uno.Navigation.Navigation;

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
        var ctr = type.GetConstructors().FirstOrDefault();
        if (ctr is not null)
        {
            var paras = ctr.GetParameters();
            var args = new List<object>();
            var logger = services.GetRequiredService<ILoggerFactory>().CreateLogger("Ecierge.Uno.Navigation");
            foreach (var para in paras)
            {
                if (navigationData.TryGetValue(para.Name!, out var data))
                {
                    Type navigationDataEntryType = data.GetType();
                    // TODO: Add entity creation from primitive
                    if (para.ParameterType.IsAssignableFrom(navigationDataEntryType))
                    {
                        args.Add(data);
                        continue;
                    }
                    else
                    {
                        logger.LogWarning("ViewModel constructor parameter '{parameterName}' of type '{type}' mismatch. Navigation data parameter type is '{dataType}'", para.Name, para.ParameterType, navigationDataEntryType);
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
}
