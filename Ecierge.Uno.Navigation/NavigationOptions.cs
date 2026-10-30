namespace Ecierge.Uno.Navigation;

using System.Diagnostics.CodeAnalysis;

using Microsoft.Extensions.Options;

/// <summary>
/// Options to adjust the behavior of Navigation
/// </summary>
public record NavigationOptions
{
    /// <summary>
    /// The type to use to override the default route resolver
    /// </summary>
    public Type? RouteResolver { get; init; }

    /// <summary>
    /// Whether to update the address bar during navigation (WASM)
    /// </summary>
    public bool? AddressBarUpdateEnabled { get; init; }

    /// <summary>
    /// Whether to support the native back button (WASM)
    /// </summary>
    public bool? UseNativeBackButton { get; init; }

    public Dictionary<Type, Type> Navigators { get; init; } = new();

    public bool TryGetNavigatorType(Type controlType, [NotNullWhen(true)] out Type? navigatorType)
    {
        if (Navigators.TryGetValue(controlType, out navigatorType))
        {
            return true;
        }

        var key =
            Navigators.Keys
                .Where(k => k.IsAssignableFrom(controlType))
                .OrderByDescending(k =>
                    k
                    .GetBaseTypes()
                    .Select((t, i) => (Index: i, Type: t))
                    .Where(t => t.Type == controlType)
                    .FirstOrDefault((-1, null!))
                    .Index
                )
                .FirstOrDefault();
        if (key is not null)
        {
            navigatorType = Navigators[key];
            Navigators[controlType] = navigatorType;
            return true;
        }
        return false;
    }

    public bool TryGetNavigatorType<TControl>([NotNullWhen(true)] out Type? navigatorType)
        where TControl : Control
     => TryGetNavigatorType(typeof(TControl), out navigatorType);
}

/// <summary>
/// Applies the callback of <c>AddNavigation</c> to the options after they were bound from the configuration, so the
/// callback sees what the configuration says and has the last word.
/// </summary>
internal sealed class AdjustNavigationOptions(Func<NavigationOptions, NavigationOptions> configure) : IPostConfigureOptions<NavigationOptions>
{
    // The options are a record with init-only properties, so the adjusted copy is written back property by property
    private static readonly System.Reflection.PropertyInfo[] Properties =
        typeof(NavigationOptions)
            .GetProperties(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance)
            .Where(property => property.SetMethod is not null)
            .ToArray();

    public void PostConfigure(string? name, NavigationOptions options)
    {
        var adjusted = configure(options);
        if (ReferenceEquals(adjusted, options)) return;

        foreach (var property in Properties)
        {
            property.SetValue(options, property.GetValue(adjusted));
        }
    }
}

internal class PostConfigureNavigationOptions(IEnumerable<Tuple<Type, Type>> navigators) : IPostConfigureOptions<NavigationOptions>
{
    public void PostConfigure(string? name, NavigationOptions options)
    {
        var navigatorsDictionary = options.Navigators;
        foreach (var navigator in navigators)
        {
            navigatorsDictionary[navigator.Item1] = navigator.Item2;
        }
    }
}
