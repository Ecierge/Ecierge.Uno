namespace Ecierge.Uno.Navigation;

using System;
using System.Linq;

using Ecierge.Uno.Navigation.Routing;

/// <summary>
/// Turns a link that opens the application into the text of a route.
/// </summary>
/// <remarks>
/// <para>
/// Two kinds of links open a page: the links of the custom scheme of the application, such as
/// <c>ecierge-console://Building/3f2a/Assets</c>, whose first segment stands where a host would be, and the links of the
/// web, such as <c>https://console.example.com/Building/3f2a/Assets</c>, whose path is the route.
/// </para>
/// <para>
/// A link names pages. It cannot give the commands of the text of a route, such as going back (<c>-</c>), clearing
/// the history (<c>-/</c>) or starting in another region (<c>/</c>, <c>../</c>, <c>./</c>), because a link comes from
/// outside and must not steer the application.
/// </para>
/// </remarks>
public static class DeepLinkMapper
{
    private static readonly char[] QueryOrFragment = ['?', '#'];

    /// <summary>
    /// Reads the route that a link names.
    /// </summary>
    /// <param name="uri">The link.</param>
    /// <param name="options">The scheme and the base path that identify the links of the application.</param>
    /// <returns>The text of the route, or <see langword="null"/> if the link does not name a page of the application.</returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="uri"/> or <paramref name="options"/> is <see langword="null"/>.
    /// </exception>
    public static string? ToRoutePath(Uri uri, NavigationOptions options)
    {
        uri = uri ?? throw new ArgumentNullException(nameof(uri));
        options = options ?? throw new ArgumentNullException(nameof(options));
        if (!uri.IsAbsoluteUri) return null;

        string? rawPath;
        if (uri.Scheme is "http" or "https")
        {
            rawPath = RemoveBasePath(uri.AbsolutePath, options.BasePath);
        }
        else if (options.DeepLinkScheme is { Length: > 0 } scheme && string.Equals(uri.Scheme, scheme, StringComparison.OrdinalIgnoreCase))
        {
            rawPath = ReadCustomSchemePath(uri);
        }
        else
        {
            return null;
        }

        if (rawPath is null) return null;
        var segments = rawPath.Split('/', StringSplitOptions.RemoveEmptyEntries).Select(Uri.UnescapeDataString).ToArray();
        if (segments.Length == 0) return null;

        // The page that the web host serves for every address is the application itself, not a page in it
        if (segments is [var only] && string.Equals(only, "index.html", StringComparison.OrdinalIgnoreCase)) return null;

        var text = string.Join('/', segments);
        return RoutePath.TryParse(text, out var path) && path.Start == RoutePathStart.Local && path.Query is null
            ? text
            : null;
    }

    private static string? RemoveBasePath(string absolutePath, string basePath)
    {
        // "/console" and "/console/" are the same base; the path of a page is what follows it
        var prefix = "/" + basePath.Trim('/');
        if (prefix == "/") return absolutePath;
        if (string.Equals(absolutePath, prefix, StringComparison.OrdinalIgnoreCase)) return string.Empty;
        return absolutePath.StartsWith(prefix + "/", StringComparison.OrdinalIgnoreCase)
            ? absolutePath[prefix.Length..]
            : null;
    }

    private static string ReadCustomSchemePath(Uri uri)
    {
        // The name of the first segment is read as a host and Uri writes a host in lower case, so the text of the link is read
        var text = uri.OriginalString;
        var start = text.IndexOf("://", StringComparison.Ordinal);
        text = start >= 0 ? text[(start + 3)..] : text[(uri.Scheme.Length + 1)..];
        var end = text.IndexOfAny(QueryOrFragment);
        return end >= 0 ? text[..end] : text;
    }
}
