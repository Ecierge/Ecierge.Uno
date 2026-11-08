namespace Ecierge.Uno.Navigation.Controls;

using System;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;

using Ecierge.Uno.Navigation.Routing;

using Microsoft.Extensions.Localization;

/// <summary>
/// What a click on a breadcrumb did.
/// </summary>
internal enum BreadcrumbOutcome
{
    /// <summary>The breadcrumb leads to the page that is shown, and the page loaded its data again.</summary>
    Refreshed,

    /// <summary>The breadcrumb leads to the page that is shown, but the page cannot load its data again.</summary>
    NotRefreshable,

    /// <summary>The level of the breadcrumb was opened with its default pages.</summary>
    Navigated,

    /// <summary>The navigation to the level of the breadcrumb failed or was denied.</summary>
    NavigationFailed,

    /// <summary>
    /// The level has no page of its own below the pages that are shown, so there is nothing to open.
    /// </summary>
    NothingToOpen
}

/// <summary>
/// The breadcrumbs of a route, and whether a breadcrumb bar shows them.
/// </summary>
/// <param name="Items">The breadcrumbs from the root down to the page that is shown.</param>
/// <param name="IsHidden">
/// <see langword="true"/> if the route passes through a segment that hides the breadcrumbs.
/// </param>
internal sealed record BreadcrumbTrail(ImmutableArray<NavigationBreadcrumb> Items, bool IsHidden)
{
    /// <summary>
    /// Gets the trail of no route.
    /// </summary>
    public static BreadcrumbTrail Empty { get; } = new(ImmutableArray<NavigationBreadcrumb>.Empty, false);

    /// <summary>
    /// Gets a value that tells whether a breadcrumb bar shows the trail.
    /// </summary>
    public bool IsShown => !IsHidden && !Items.IsEmpty;

    /// <summary>
    /// Builds the breadcrumbs of a route. A dialog of the route is left out: the breadcrumbs stand for the page under
    /// it.
    /// </summary>
    /// <param name="route">The route from the root to the page.</param>
    /// <param name="titleOf">Gives the text of a named segment.</param>
    /// <param name="displayNameOf">
    /// Gives the name of the entity of a data step, or <see langword="null"/> if there is none yet.
    /// </param>
    /// <returns>The trail.</returns>
    public static BreadcrumbTrail Build(
        Routing.Route route,
        Func<NameSegment, string> titleOf,
        Func<DataSegmentInstance, string?> displayNameOf)
    {
        route = route.WithoutDialog();
        var segments = route.Segments;
        var isHidden = segments.Any(segment => segment is NameSegmentInstance { NameSegment.HidesBreadcrumbs: true });
        var items = ImmutableArray.CreateBuilder<NavigationBreadcrumb>(segments.Length);
        for (var index = 0; index < segments.Length; index++)
        {
            string title;
            switch (segments[index])
            {
                case NameSegmentInstance named
                    when index + 1 < segments.Length
                         && segments[index + 1] is DataSegmentInstance data
                         && ReferenceEquals(named.NameSegment.DataSegment, data.DataSegment):
                    // The segment and its value are one level: the building, not "Building" followed by its id
                    title = displayNameOf(data) ?? titleOf(named.NameSegment);
                    index++;
                    break;
                case NameSegmentInstance named:
                    title = titleOf(named.NameSegment);
                    break;
                case DataSegmentInstance data:
                    title = displayNameOf(data) ?? data.Primitive;
                    break;
                default:
                    continue;
            }

            items.Add(new NavigationBreadcrumb(title, route.Take(index + 1), isCurrent: index == segments.Length - 1));
        }

        return new(items.ToImmutable(), isHidden);
    }

    /// <summary>
    /// Gives the text of a named segment: its title, localized when the localizer has a resource under the title;
    /// the name of the segment when it has no title.
    /// </summary>
    /// <param name="segment">The segment.</param>
    /// <param name="localizer">The localizer of the application, or <see langword="null"/> if it has none.</param>
    /// <returns>The text.</returns>
    public static string TitleOf(NameSegment segment, IStringLocalizer? localizer)
    {
        if (string.IsNullOrEmpty(segment.Title)) return segment.Name;
        if (localizer is null) return segment.Title;

        var localized = localizer[segment.Title];
        // An empty resource would leave a breadcrumb without text
        return localized.ResourceNotFound || string.IsNullOrEmpty(localized.Value) ? segment.Title : localized.Value;
    }

    /// <summary>
    /// Adds the default segments below the last step of the route, so that the route ends at a page, as a route that
    /// is written as text does.
    /// </summary>
    /// <param name="route">The route of a level.</param>
    /// <returns>The route with the default segments, or the route itself if the level has no default segment.</returns>
    public static Routing.Route WithDefaultPages(Routing.Route route)
    {
        if (route.Segments.IsEmpty) return route;

        var current = route.Segments[^1].Segment;
        while (current.Nested.FirstOrDefault(segment => segment.IsDefault) is { } defaultSegment)
        {
            route = route.Add(defaultSegment);
            current = defaultSegment;
        }

        return route;
    }

    /// <summary>
    /// Opens the level of the breadcrumb. When the level, with its default pages, is the page that is shown, the page
    /// loads its data again instead; otherwise the pages that differ are shown, and the history gets an entry.
    /// </summary>
    /// <param name="root">The root navigator of the window.</param>
    /// <param name="breadcrumb">The breadcrumb that was clicked.</param>
    /// <param name="initiator">The object that starts the navigation.</param>
    /// <returns>What the click did.</returns>
    public static async ValueTask<BreadcrumbOutcome> OpenAsync(Navigator root, NavigationBreadcrumb breadcrumb, object initiator)
    {
        var shown = root.ActualRoute.WithoutDialog();
        var target = WithDefaultPages(breadcrumb.Route);
        var common = target.CommonPrefixLength(shown);
        if (common == target.Segments.Length && common == shown.Segments.Length)
        {
            return root.TryRefreshPage() ? BreadcrumbOutcome.Refreshed : BreadcrumbOutcome.NotRefreshable;
        }

        // The level is shown, but it has no default page that differs from the pages below it
        if (common == target.Segments.Length) return BreadcrumbOutcome.NothingToOpen;

        var response = await root.NavigateToAsync(initiator, target).ConfigureAwait(true);
        return response.Success ? BreadcrumbOutcome.Navigated : BreadcrumbOutcome.NavigationFailed;
    }
}
