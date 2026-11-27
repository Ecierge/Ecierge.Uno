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
    /// The text of the close button of a dialog that the library builds around a view that is not a content dialog. Such
    /// a dialog has no buttons of its own, so without this button the user could close it only with a key that some
    /// platforms do not give to a dialog without buttons. Closing it with the button is going back, like closing a
    /// dialog with its own button. The default is <c>Close</c>; <see langword="null"/> or an empty text leaves the dialog
    /// without a button, and then the view has to close the dialog itself.
    /// </summary>
    public string? DialogCloseButtonText { get; init; } = "Close";

    /// <summary>
    /// The thickness of the outline of a dialog that the library builds around a view that is not a content dialog. A theme
    /// may draw a content dialog with a shadow and no outline, so that its edge cannot be seen on a light page. The outline has
    /// the color of the outline of the theme: <c>OutlineBrush</c> of Material or <c>CardStrokeColorDefaultBrush</c> of Fluent.
    /// The default is <c>1</c>; zero or less, or a theme without such a brush, leaves the dialog as the theme draws it.
    /// </summary>
    public double DialogBorderThickness { get; init; } = 1;

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

    /// <summary>
    /// How long a navigation may stay open before the navigations that start after it stop waiting for it. A
    /// navigation is open from its start until its end, and while one is open the routes that are navigated meanwhile
    /// are recorded in the history and announced when the last of them has ended. A navigation that never ends, for
    /// example because a request is never answered, would hold them back for ever, so a navigation that starts after
    /// this time leaves it out. When it does end later it is recorded and announced, if no other navigation is open.
    /// The default is 30 seconds; <see cref="Timeout.InfiniteTimeSpan"/> waits for ever.
    /// </summary>
    public TimeSpan StaleNavigationTimeout { get; init; } = TimeSpan.FromSeconds(30);

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
/// Hands the options that the factory of the options pattern created to the callback of
/// <see cref="ServiceCollectionExtensions.AddNavigation"/>, once the configuration was bound and the navigators of the library
/// were added, and validates what the callback returns. That is what the services get, so the callback has the last word and
/// nothing is copied: the options are a record with init-only properties, which only a constructor, an object initializer or
/// <c>with</c> can write.
/// </summary>
/// <param name="factory">Creates the options with the configuration bound and the navigators added, without validating them.</param>
/// <param name="configure">The callback of <see cref="ServiceCollectionExtensions.AddNavigation"/>.</param>
/// <param name="validations">The validators of the options, which check the options that the callback returned.</param>
internal sealed class AdjustedNavigationOptionsFactory(
    IOptionsFactory<NavigationOptions> factory,
    Func<NavigationOptions, NavigationOptions> configure,
    IEnumerable<IValidateOptions<NavigationOptions>> validations)
    // Without setups and post-configurations of its own the base only validates the instance that CreateInstance gives it
    : OptionsFactory<NavigationOptions>([], [], validations)
{
    protected override NavigationOptions CreateInstance(string name) => configure(factory.Create(name));
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
