namespace Ecierge.Uno.Navigation;

using System;
using System.Runtime.InteropServices.JavaScript;

using Microsoft.Extensions.DependencyInjection;

internal static partial class PlatformServices
{
    static partial void AddForPlatform(IServiceCollection services)
    {
        services.AddSingleton<IBrowserHistory, BrowserHistory>();
        services.AddSingleton<IDeepLinkSource, WebAssemblyDeepLinkSource>();
    }
}

/// <summary>
/// The history of the browser that the application runs in, reached through the History API of the page.
/// </summary>
/// <remarks>
/// <para>
/// The state of an entry is an object, <c>{ ecierge: token }</c>. The platform of Uno keeps its own entry for the back
/// button of the system with a number as its state, and does not touch the entries whose state is an object.
/// </para>
/// </remarks>
internal sealed partial class BrowserHistory : IBrowserHistory
{
    private const string Api = "globalThis.eciergeNavigationHistory";

    // The script is installed once, before the first call, like the other scripts of the application are
    private const string Script = """
        (function () {
            if (globalThis.eciergeNavigationHistory) return;
            globalThis.eciergeNavigationHistory = {
                href: function () { return globalThis.location.href; },
                push: function (path, token) { globalThis.history.pushState({ ecierge: token }, '', path); },
                replace: function (path, token) { globalThis.history.replaceState({ ecierge: token }, '', path); },
                go: function (delta) { globalThis.history.go(delta); },
                subscribe: function (callback) {
                    globalThis.addEventListener('popstate', function (event) {
                        const state = event.state;
                        const token = state && typeof state.ecierge === 'string' ? state.ecierge : null;
                        callback(globalThis.location.href, token);
                    });
                }
            };
        })();
        """;

    static BrowserHistory() => Evaluate(Script);

    public BrowserHistory() => Subscribe(OnPopState);

    /// <inheritdoc />
    public Uri CurrentUri => new(GetHref());

    /// <inheritdoc />
    public event EventHandler<BrowserHistoryPopEventArgs>? PopState;

    /// <inheritdoc />
    public void Push(string path, string token) => PushState(path, token);

    /// <inheritdoc />
    public void Replace(string path, string token) => ReplaceState(path, token);

    /// <inheritdoc />
    public void Go(int delta) => GoState(delta);

    private void OnPopState(string href, string? token)
     => PopState?.Invoke(this, new BrowserHistoryPopEventArgs(new Uri(href), token));

    // The script defines the functions below, so it runs in the global scope before they are imported
    [JSImport("globalThis.eval")]
    private static partial void Evaluate(string script);

    [JSImport(Api + ".href")]
    private static partial string GetHref();

    [JSImport(Api + ".push")]
    private static partial void PushState(string path, string token);

    [JSImport(Api + ".replace")]
    private static partial void ReplaceState(string path, string token);

    [JSImport(Api + ".go")]
    private static partial void GoState(int delta);

    [JSImport(Api + ".subscribe")]
    private static partial void Subscribe([JSMarshalAs<JSType.Function<JSType.String, JSType.String>>] Action<string, string?> callback);
}

/// <summary>
/// Reads the link that opened the application from the address of the page.
/// </summary>
internal sealed class WebAssemblyDeepLinkSource : IDeepLinkSource
{
    /// <summary>
    /// Creates the source.
    /// </summary>
    /// <param name="history">The browser history, which knows the address the page was loaded with.</param>
    public WebAssemblyDeepLinkSource(IBrowserHistory history) => LaunchUri = history.CurrentUri;

    /// <inheritdoc />
    public Uri? LaunchUri { get; }

    /// <summary>
    /// A link that reaches a page that runs is a new address of the same page, which the browser history follows.
    /// </summary>
    public event EventHandler<Uri>? Received
    {
        add { }
        remove { }
    }
}
