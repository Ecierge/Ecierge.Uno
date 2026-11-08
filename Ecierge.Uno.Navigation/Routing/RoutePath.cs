namespace Ecierge.Uno.Navigation.Routing;

using System;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

/// <summary>
/// Where a route written as text starts.
/// </summary>
public enum RoutePathStart
{
    /// <summary>The route starts in the region of the navigator that reads it: <c>Menu</c>.</summary>
    Local,

    /// <summary>The route starts in the root region: <c>/Building/id</c>.</summary>
    Root,

    /// <summary>The route starts in the region inside the content of the navigator: <c>./Info</c>.</summary>
    Nested,

    /// <summary>The route starts in the region that contains the navigator: <c>../Menu</c>, with a number of levels.</summary>
    Parent,

    /// <summary>The route goes back in the history: <c>-</c>, with a number of entries.</summary>
    Back,

    /// <summary>The route clears the history and then navigates: <c>-/Login</c>.</summary>
    ClearHistory
}

/// <summary>
/// A route written as text, split into where it starts and the names that follow. The text holds the names of
/// segments, the values of data segments and the names of dialogs with the dialog qualifier in front, separated by the
/// segment separator.
/// </summary>
/// <example>
/// <code>
/// Building/3f2a0c4e-5b1d-4e07-9a52-0a8d43f1c6b9/Details/Floors
/// /Building/3f2a0c4e-5b1d-4e07-9a52-0a8d43f1c6b9/!AddAsset
/// ../Menu
/// -
/// -/Login
/// </code>
/// </example>
/// <param name="Start">Where the route starts.</param>
/// <param name="Levels">The number of the levels up for <see cref="RoutePathStart.Parent"/> or of the entries back for <see cref="RoutePathStart.Back"/>.</param>
/// <param name="Segments">The names, values and dialog names that follow the start.</param>
/// <param name="Query">The text after the query separator, which is not used yet.</param>
public readonly record struct RoutePath(RoutePathStart Start, int Levels, ImmutableArray<string> Segments, string? Query = null)
{
    private const char QuerySeparator = '?';

    /// <summary>
    /// Reads a route from text.
    /// </summary>
    /// <param name="text">The text of the route.</param>
    /// <param name="path">The route.</param>
    /// <returns><see langword="true"/> if the text is a well formed route.</returns>
    public static bool TryParse(string? text, out RoutePath path)
    {
        path = default;
        if (text is null) return false;

        string? query = null;
        var queryIndex = text.IndexOf(QuerySeparator);
        if (queryIndex >= 0)
        {
            query = text[(queryIndex + 1)..];
            text = text[..queryIndex];
        }

        var start = RoutePathStart.Local;
        var levels = 0;
        var rest = text;
        bool StartsWith(string qualifier) => rest.StartsWith(qualifier, StringComparison.Ordinal);

        if (StartsWith(Qualifiers.Root))
        {
            start = RoutePathStart.Root;
            rest = rest[Qualifiers.Root.Length..];
        }
        else if (StartsWith(Qualifiers.ClearBackStack))
        {
            start = RoutePathStart.ClearHistory;
            rest = rest[Qualifiers.ClearBackStack.Length..];
        }
        else if (StartsWith(Qualifiers.Nested))
        {
            start = RoutePathStart.Nested;
            rest = rest[Qualifiers.Nested.Length..];
        }
        else if (StartsWith(Qualifiers.Parent))
        {
            start = RoutePathStart.Parent;
            while (StartsWith(Qualifiers.Parent))
            {
                levels++;
                rest = rest[Qualifiers.Parent.Length..];
            }
        }
        else if (StartsWith(Qualifiers.NavigateBack))
        {
            start = RoutePathStart.Back;
            while (StartsWith(Qualifiers.NavigateBack))
            {
                levels++;
                rest = rest[Qualifiers.NavigateBack.Length..];
            }

            // Going back takes no segments, and the clear qualifier is a single dash
            if (rest.Length > 0) return false;
        }

        var segments = ImmutableArray<string>.Empty;
        if (rest.Length > 0)
        {
            var names = rest.Split(Qualifiers.Separator[0]);
            // A trailing separator is allowed
            if (names.Length > 1 && names[^1].Length == 0) names = names[..^1];
            if (names.Any(name => name.Length == 0 || name == Qualifiers.Dialog)) return false;
            segments = names.ToImmutableArray();
        }

        path = new RoutePath(start, levels, segments, query);
        return true;
    }

    /// <summary>
    /// Reads a route from text.
    /// </summary>
    /// <param name="text">The text of the route.</param>
    /// <returns>The route.</returns>
    /// <exception cref="FormatException">The text is not a well formed route.</exception>
    public static RoutePath Parse(string text)
     => TryParse(text, out var path)
        ? path
        : throw new FormatException($"'{text}' is not a well formed route.");

    /// <summary>
    /// Writes the route as text.
    /// </summary>
    /// <returns>The text of the route.</returns>
    public override string ToString()
    {
        var prefix = Start switch
        {
            RoutePathStart.Root => Qualifiers.Root,
            RoutePathStart.Nested => Qualifiers.Nested,
            RoutePathStart.Parent => string.Concat(Enumerable.Repeat(Qualifiers.Parent, Levels)),
            RoutePathStart.Back => string.Concat(Enumerable.Repeat(Qualifiers.NavigateBack, Levels)),
            RoutePathStart.ClearHistory => Qualifiers.ClearBackStack,
            _ => string.Empty
        };
        var text = prefix + string.Join(Qualifiers.Separator, Segments.IsDefault ? [] : Segments);
        return Query is null ? text : text + QuerySeparator + Query;
    }
}
