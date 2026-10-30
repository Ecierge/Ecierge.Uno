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
    /// Whether the browser history follows the history of the application: every page gets an entry in the history
    /// of the browser, and the address bar shows its path (WASM). The default is <see langword="true"/>.
    /// </summary>
    public bool? AddressBarUpdateEnabled { get; init; }

    /// <summary>
    /// Whether the back button of the system goes back in the history of the application. The default is
    /// <see langword="true"/> on Android and iOS and <see langword="false"/> elsewhere, where the application shows
    /// its own button and the browser has its own.
    /// </summary>
    public bool? UseNativeBackButton { get; init; }

    /// <summary>
    /// Whether an open dialog is an entry of the history. When it is, going back closes the dialog and closing the
    /// dialog with its own buttons moves the history back by one entry. When it is not, the history holds the page
    /// below the dialog, and going back with a dialog open goes to the page before it. The default is
    /// <see langword="true"/>.
    /// </summary>
    public bool DialogsInHistory { get; init; } = true;

    /// <summary>
    /// The scheme of the links that open a page of the application, for example <c>ecierge-console</c> for the link
    /// <c>ecierge-console://Building/id/Assets</c>. Links of other schemes, such as the callbacks of a sign-in, are
    /// not routes. Without a scheme no link with a custom scheme is followed. Links of the web (<c>http</c> and
    /// <c>https</c>) are always routes.
    /// </summary>
    public string? DeepLinkScheme { get; init; }

    /// <summary>
    /// The path under which the application is hosted on the web, without which a link is a route. The default is
    /// <c>/</c>.
    /// </summary>
    public string BasePath { get; init; } = "/";

    /// <summary>
    /// The number of the entries that the history keeps. The oldest entry is dropped when a new one does not fit. The
    /// default is 100.
    /// </summary>
    public int JournalCapacity { get; init; } = 100;

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
