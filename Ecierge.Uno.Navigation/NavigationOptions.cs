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
/// Hands the options that the factory of the options pattern created to the callback of <c>AddNavigation</c>, once the
/// configuration was bound and the navigators of the library were added. What the callback returns is what the services
/// get, so it has the last word and nothing is copied: the options are a record with init-only properties, which only a
/// constructor, an object initializer or <c>with</c> can write.
/// </summary>
internal sealed class AdjustedNavigationOptionsFactory(
    IOptionsFactory<NavigationOptions> factory,
    Func<NavigationOptions, NavigationOptions> configure) : IOptionsFactory<NavigationOptions>
{
    public NavigationOptions Create(string name) => configure(factory.Create(name));
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
