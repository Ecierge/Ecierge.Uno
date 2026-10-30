namespace Ecierge.Uno.Navigation;

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading.Tasks;

using Ecierge.Uno.Navigation.Navigators;
using Ecierge.Uno.Navigation.Routing;

using Microsoft.Extensions.DependencyInjection;

/// <summary>
/// A route that was read from text, with the navigator whose region it starts in.
/// </summary>
/// <param name="Navigator">The navigator whose region the route starts in.</param>
/// <param name="Route">The route, relative to the region of the navigator.</param>
/// <param name="Path">The text of the route, split into where it starts and the names that follow.</param>
internal sealed record ResolvedRoute(Navigator Navigator, Routing.Route Route, RoutePath Path);

/// <summary>
/// Navigation of routes, segments and dialogs through a navigator, and going back and forward in the history.
/// </summary>
public static class NavigatorExtensions
{
    private static Navigator? GetAncestor(this Navigator navigator, int levels)
    {
        var ancestor = navigator;
        for (var i = 0; i < levels && ancestor is not null; i++)
        {
            ancestor = ancestor.Parent;
        }

        return ancestor;
    }

    /// <summary>
    /// Reads the text of a route, and finds the navigator whose region the route starts in.
    /// </summary>
    internal static bool TryResolveRoute(
        [NotNull] this Navigator navigator,
        string text,
        INavigationData? navigationData,
        [NotNullWhen(true)] out ResolvedRoute? resolved,
        [NotNullWhen(false)] out Exception? error)
    {
        resolved = null;
        if (!RoutePath.TryParse(text, out var path))
        {
            error = new FormatException($"'{text}' is not a well formed route.");
            return false;
        }

        Navigator? start = path.Start switch
        {
            RoutePathStart.Root => navigator.RootNavigator,
            RoutePathStart.Nested => navigator.ChildNavigator,
            RoutePathStart.Parent => navigator.GetAncestor(path.Levels),
            RoutePathStart.Back => null,
            _ => navigator
        };
        if (start is null)
        {
            error = path.Start == RoutePathStart.Back
                ? new InvalidOperationException("A route that goes back has no segments.")
                : new InvalidOperationException($"There is no region for the route '{text}': the region '{navigator.Region.Segment.Name}' has no {(path.Start == RoutePathStart.Nested ? "child" : "parent")} region to start in.");
            return false;
        }

        if (!TryParseSegments(start, path.Segments, navigationData, out var route, out error)) return false;

        resolved = new ResolvedRoute(start, route, path);
        return true;
    }

    /// <summary>
    /// Finds the dialog among the nested segments of the segment and of the segments above it.
    /// </summary>
    private static DialogSegment? FindDialogAbove(RouteSegment? segment, string dialogName)
    {
        for (var current = segment; current is not null; current = current.ParentSegment)
        {
            if (current.Nested.OfType<DialogSegment>().FirstOrDefault(dialog => dialog.Name == dialogName) is { } found)
                return found;
        }

        return null;
    }

    private static bool TryParseSegments(
        Navigator navigator,
        IReadOnlyList<string> tokens,
        INavigationData? navigationData,
        [NotNullWhen(true)] out Routing.Route? route,
        [NotNullWhen(false)] out Exception? error)
    {
        route = null;
        error = null;
        var data = (navigator.Parent?.Route.Data ?? NavigationData.Empty).Union(navigationData);
        var instances = new List<RouteSegmentInstance>(tokens.Count + 2);

        // The last navigable segment from which nested segments are searched
        RouteSegment current = navigator.Region.Segment is { DataSegment: { } regionData }
            ? regionData
            : navigator.Region.Segment;

        foreach (var token in tokens)
        {
            RouteSegment? next;
            if (current is NameSegment { DataSegment: { } valueSegment })
            {
                // The next is data: the token is its value
                next = valueSegment;
            }
            else if (token.StartsWith(Qualifiers.Dialog, StringComparison.Ordinal))
            {
                // The next is dialog: one of the segments that the text passed offers it, else a region around the
                // navigator does. The pages of the text do not exist yet when a link is read, so the regions cannot
                // be asked.
                var dialogName = token[Qualifiers.Dialog.Length..];
                next = FindDialogAbove(current, dialogName);
                if (next is null)
                {
                    try
                    {
                        next = navigator.FindDialogSegmentToNavigate(dialogName);
                    }
                    catch (NestedSegmentMissingException ex)
                    {
                        error = ex;
                        return false;
                    }
                }
            }
            else
            {
                // The next is nested
                next = current.Nested.FirstOrDefault(s => s.Name == token);
            }

            if (next is null)
            {
                error = new NestedSegmentNotFoundException(current, token);
                return false;
            }

            switch (next)
            {
                case DialogSegment dialog:
                    instances.Add(new DialogSegmentInstance(dialog));
                    break;
                case NameSegment name:
                    instances.Add(new NameSegmentInstance(name));
                    break;
                case DataSegment value:
                    if (value.DataMap is { } mapType)
                    {
                        // The handle that the routes above already hold is shared when the value is the same
                        var map = (INavigationDataMap)navigator.ServiceProvider.GetRequiredService(mapType);
                        data = data.SetItem(value.Name, map.GetOrCreateEntity(data, value.Name, token));
                    }

                    instances.Add(new DataSegmentInstance(value, token, null));
                    break;
                default:
                    error = new NotSupportedException("Not supported route segment");
                    return false;
            }

            current = next;
        }

        while (current.Nested.FirstOrDefault(s => s.IsDefault) is { } defaultSegment)
        {
            instances.Add(new NameSegmentInstance(defaultSegment));
            current = defaultSegment;
        }

        try
        {
            route = new Routing.Route(instances.ToImmutableArray(), data);
            return true;
        }
        catch (InvalidOperationException ex)
        {
            error = ex;
            return false;
        }
    }

    /// <summary>
    /// Reads a route that is written as text.
    /// </summary>
    /// <param name="navigator">The navigator whose region the route starts in, unless the text starts elsewhere.</param>
    /// <param name="route">The route text, for example <c>Menu</c>, <c>Building/id/Details</c> or <c>/Login</c>.</param>
    /// <param name="navigationData">The data to add to the route.</param>
    /// <param name="parsedRoute">The route, relative to the region that the text starts in.</param>
    /// <returns><see langword="true"/> if the text is a route.</returns>
    public static bool TryParseRoute([NotNull] this Navigator navigator, string route, INavigationData? navigationData, [NotNullWhen(true)] out Routing.Route? parsedRoute)
    {
        if (navigator.TryResolveRoute(route, navigationData, out var resolved, out _))
        {
            parsedRoute = resolved.Route;
            return true;
        }

        parsedRoute = null;
        return false;
    }

    /// <summary>
    /// Reads a route that is written as text.
    /// </summary>
    /// <param name="navigator">The navigator whose region the route starts in, unless the text starts elsewhere.</param>
    /// <param name="route">The route text.</param>
    /// <param name="navigationData">The data to add to the route.</param>
    /// <returns>The route, relative to the region that the text starts in.</returns>
    /// <exception cref="InvalidRouteException">A segment of the text is not a nested segment of the region.</exception>
    /// <exception cref="FormatException">The text is not a well formed route.</exception>
    /// <exception cref="InvalidOperationException">
    /// The text starts in a region that has no parent or child region to start in.
    /// </exception>
    public static Routing.Route ParseRoute([NotNull] this Navigator navigator, string route, INavigationData? navigationData = null)
    {
        if (navigator.TryResolveRoute(route, navigationData, out var resolved, out var error))
            return resolved.Route;

        throw error;
    }

    /// <summary>
    /// Navigates to a route that is written as text. The text can say where the route starts (<c>/</c>, <c>./</c> or
    /// <c>../</c>), go back in the history (<c>-</c>), or clear the history first (<c>-/</c>).
    /// </summary>
    /// <param name="navigator">The navigator whose region the route starts in, unless the text starts elsewhere.</param>
    /// <param name="initiator">The object that starts the navigation.</param>
    /// <param name="route">The route text.</param>
    /// <param name="navigationData">The data to add to the route.</param>
    /// <param name="historyMode">What the navigation does to the history.</param>
    /// <returns>The response of the navigation.</returns>
    /// <exception cref="FormatException"><paramref name="route"/> is not a well formed route.</exception>
    /// <exception cref="InvalidRouteException">A segment of the text is not a nested segment of the region.</exception>
    /// <exception cref="InvalidOperationException">
    /// The text starts in a region that has no parent or child region to start in.
    /// </exception>
    public static async ValueTask<NavigationResponse> NavigateRouteAsync(
        [NotNull] this Navigator navigator,
        object initiator,
        string route,
        INavigationData? navigationData = null,
        HistoryMode historyMode = HistoryMode.Push)
    {
        if (!RoutePath.TryParse(route, out var path))
            throw new FormatException($"'{route}' is not a well formed route.");

        if (path.Start == RoutePathStart.Back)
            return await navigator.GoBackAsync(initiator, path.Levels);

        if (path.Start == RoutePathStart.ClearHistory)
        {
            // Nothing leads back to the pages before this one
            navigator.Journal.Clear();
            historyMode = HistoryMode.Replace;
        }

        var resolved = navigator.TryResolveRoute(route, navigationData, out var found, out var error)
            ? found
            : throw error;
        if (resolved.Route.Segments.Length < 1)
            // TODO: Consider another response
            return new NavigationSuccessfulResponse(resolved.Route, navigator);

        return await resolved.Navigator.NavigateRouteAsync(initiator, resolved.Route, historyMode);
    }

    /// <summary>
    /// Navigates to a route, one segment after another, each through the navigator of its own region.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The pages below a data segment are created after the entity of the segment has loaded. A load that fails ends
    /// the navigation with a <see cref="NavigationFailedResponse"/> that carries the exception and can be retried.
    /// </para>
    /// <para>
    /// When the navigation succeeds, the whole route that is shown is recorded in the history of the window, as
    /// <paramref name="historyMode"/> says.
    /// </para>
    /// </remarks>
    /// <param name="navigator">The navigator that navigates the first segment.</param>
    /// <param name="initiator">The object that starts the navigation.</param>
    /// <param name="route">The route.</param>
    /// <param name="historyMode">What the navigation does to the history.</param>
    /// <param name="mode">How the navigation was started.</param>
    /// <returns>The response of the navigation.</returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="navigator"/> or <paramref name="route"/> is <see langword="null"/>.
    /// </exception>
    public static async ValueTask<NavigationResponse> NavigateRouteAsync(
        [NotNull] this Navigator navigator,
        object initiator,
        Routing.Route route,
        HistoryMode historyMode = HistoryMode.Push,
        NavigationMode mode = NavigationMode.New)
    {
        navigator = navigator ?? throw new ArgumentNullException(nameof(navigator));
        route = route ?? throw new ArgumentNullException(nameof(route));

        var navigation = navigator.ServiceProvider.GetRequiredService<IRouteUpdater>().StartNavigation(navigator, historyMode, mode);
        navigator.RaiseNavigationStarted(() => route);
        NavigationResponse? response = null;
        Exception? error = null;
        try
        {
            response = await NavigateSegmentsAsync(navigator, initiator, route, historyMode, mode);
            return response;
        }
        catch (Exception ex)
        {
            error = ex;
            throw;
        }
        finally
        {
            response ??= new NavigationFailedResponse(route, navigator) { Mode = mode, Error = error };
            navigator.RaiseNavigationCompleted(response);
            await navigation.CompleteAsync(response);
        }
    }

    private static Routing.Route RequestedRouteFromRoot(Navigator navigator, Routing.Route route)
    {
        if (navigator.Parent is not { } parent) return route;
        try
        {
            return parent.Route.Join(route);
        }
        catch (InvalidOperationException)
        {
            return route;
        }
    }

    private static IEnumerable<INavigationEntity> EntitiesOf(IEnumerable<RouteSegmentInstance> segments, INavigationData data)
    {
        foreach (var segment in segments)
        {
            if (segment is DataSegmentInstance step
                && data.TryGetValue(step.DataSegment.Name, out var value)
                && value is INavigationEntity entity)
            {
                yield return entity;
            }
        }
    }

    /// <summary>
    /// Navigates the segments of the route one after another, each through the navigator of its own region.
    /// </summary>
    /// <exception cref="NotSupportedException">A segment of the route is of a kind that is not known.</exception>
    private static async ValueTask<NavigationResponse> NavigateSegmentsAsync(
        Navigator navigator,
        object initiator,
        Routing.Route route,
        HistoryMode historyMode,
        NavigationMode mode)
    {
        // What the routes above hold is shared with this route: the handle of a building is the same for all its pages
        route = route with { Data = (navigator.Parent?.Route.Data ?? NavigationData.Empty).Union(route.Data) };
        route = route.WithEntityHandles(navigator.ServiceProvider);

        var requested = RequestedRouteFromRoot(navigator, route);
        NavigationFailedResponse Failed(Routing.Route routeSoFar, IReadOnlyList<string>? errors = null, Exception? error = null, bool denied = false)
        {
            // Repeating the navigation shows only what is not shown yet; the entity loads again if it failed
            ValueTask<NavigationResponse> Retry()
             => navigator.RootNavigator.NavigateToAsync(initiator, requested, historyMode, NavigationMode.Refresh);

            var reasons = errors ?? Array.Empty<string>();
            return denied
                ? new NavigationDeniedResponse(routeSoFar, navigator, reasons) { Mode = mode, Errors = reasons, RequestedRoute = requested, Retry = Retry }
                : new NavigationFailedResponse(routeSoFar, navigator) { Mode = mode, Errors = reasons, Error = error, RequestedRoute = requested, Retry = Retry };
        }

        var segmentsAbove = navigator.Parent?.Route.Segments ?? ImmutableArray<RouteSegmentInstance>.Empty;
        var navigatableSegments = route.GetNavigatableSegments();
        var currentNavigator = navigator;
        NavigationResult result = default;
        for (int i = 0; i < navigatableSegments.Length; i++)
        {
            var segment = navigatableSegments[i];
            var stepIndex = route.Segments.IndexOf(segment);

            // A page below a data segment needs its entity: it is created after the entity has loaded, and the
            // navigation fails if the entity cannot be loaded. An entity that failed before is loaded again.
            var stepsBefore = route.Segments.Take(Math.Max(stepIndex, 0));
            foreach (var entity in EntitiesOf(segmentsAbove.Concat(stepsBefore), route.Data).ToList())
            {
                if (entity.IsLoaded) continue;
                try
                {
                    var loading = entity.LoadAsync();
                    navigator.RaiseEntityLoading(entity, loading);
                    await loading;
                }
#pragma warning disable CA1031 // Do not catch general exception types
                catch (Exception ex)
#pragma warning restore CA1031 // Do not catch general exception types
                {
                    return Failed(route.Take(Math.Max(stepIndex, 0)), error: ex);
                }
            }

            switch (segment)
            {
                case NameSegmentInstance nameSegmentInstance:
                    result = await currentNavigator.NavigateAsync(new NameSegmentNavigationRequest(initiator, nameSegmentInstance.NameSegment, route));
                    break;
                case DataSegmentInstance dataSegmentInstance:
                    // The route holds the handle of the entity, so the navigator creates the page with the shared handle
                    result = await currentNavigator.NavigateAsync(new PrimitiveDataSegmentNavigationRequest(initiator, dataSegmentInstance.DataSegment, dataSegmentInstance.Primitive, route));
                    if (result.Success)
                        route = route with { Data = result.Request.Route.Data };
                    break;
                case DialogSegmentInstance dialogSegmentInstance:
                    var parentSegment =
                        i > 0 ?
                        navigatableSegments[i - 1].Segment :
                        navigator.Region.Segment;
                    result = await currentNavigator.NavigateAsync(new DialogSegmentNavigationRequest(initiator, dialogSegmentInstance.DialogSegment, parentSegment, route));
                    // The dialog is attached below the last navigator, and the next navigator must be inside the
                    // dialog instead of the navigator of the dialog
                    if (result.Success)
                        currentNavigator = currentNavigator.LeafNavigator;
                    break;
                default:
                    throw new NotSupportedException("Unknown segment type.");
            }

            if (!result.Success)
            {
                var effectiveRoute = route.TrimTill(segment);
                var parentRoute = currentNavigator.Parent?.Route ?? new Routing.Route();
                Routing.Route fullRoute;
                try
                {
                    fullRoute = parentRoute.Join(effectiveRoute);
                }
                catch (InvalidOperationException)
                {
                    fullRoute = effectiveRoute;
                }

                return Failed(fullRoute, result.Errors, denied: result.IsDenied);
            }

            // Child navigator is created after UI is created and attached to a visual tree
            await currentNavigator.WaitForVisualTreeAsync();
            if (currentNavigator.ChildNavigator is not null)
                currentNavigator = currentNavigator.ChildNavigator;
        }

        return new NavigationSuccessfulResponse(currentNavigator.ActualRoute, navigator) { Mode = mode };
    }

    /// <summary>
    /// Navigates to a whole route, starting at the first navigator whose segment does not show what the route
    /// needs. The shell, the layout and the other pages that are already right stay as they are.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Going back and forward, the browser buttons and the links use it: they know the route from the root to the page,
    /// not the navigator that has to change. A dialog that is open and not part of the route is closed first.
    /// </para>
    /// </remarks>
    /// <param name="navigator">Any navigator of the window.</param>
    /// <param name="initiator">The object that starts the navigation.</param>
    /// <param name="target">The route from the root to the page.</param>
    /// <param name="historyMode">What the navigation does to the history.</param>
    /// <param name="mode">How the navigation was started.</param>
    /// <returns>The response of the navigation.</returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="navigator"/> or <paramref name="target"/> is <see langword="null"/>.
    /// </exception>
    public static async ValueTask<NavigationResponse> NavigateToAsync(
        [NotNull] this Navigator navigator,
        object initiator,
        Routing.Route target,
        HistoryMode historyMode = HistoryMode.Push,
        NavigationMode mode = NavigationMode.New)
    {
        navigator = navigator ?? throw new ArgumentNullException(nameof(navigator));
        target = target ?? throw new ArgumentNullException(nameof(target));

        var root = navigator.RootNavigator;
        var chain = new List<Navigator>();
        for (Navigator? current = root; current is not null; current = current.ChildNavigator)
        {
            chain.Add(current);
        }

        // The first navigator that does not show its step of the target has to navigate
        var changed = 0;
        var lengthBefore = 0;
        while (changed < chain.Count)
        {
            var length = chain[changed].Route.Segments.Length;
            if (length <= lengthBefore || chain[changed].Route.CommonPrefixLength(target) < length) break;
            lengthBefore = length;
            changed++;
        }

        // What the route still needs after the steps that are shown; none if the target is not longer than what is shown
        var tail = lengthBefore >= target.Segments.Length
            ? ImmutableArray<RouteSegmentInstance>.Empty
            : target.Segments[lengthBefore..];

        // A dialog that is open must not stay over the pages that change
        foreach (var dialog in chain.Skip(changed).OfType<IDialogNavigator>().Reverse().ToList())
        {
            await dialog.CloseNavigator();
        }

        if (tail.IsEmpty)
            return new NavigationSuccessfulResponse(root.ActualRoute, root) { Mode = mode };

        // When every navigator shows its step, or the navigator that has to change is a dialog that was closed, the
        // route goes on below the last page. Only a dialog can be shown there: the page has no region for another page.
        var hasNoRegion = changed >= chain.Count || chain[changed] is IDialogNavigator;
        var start = hasNoRegion ? chain[Math.Max(changed - 1, 0)] : chain[changed];
        if (hasNoRegion && !tail[0].IsDialog)
            return new NavigationFailedResponse(root.ActualRoute, root)
            {
                Mode = mode,
                Errors = [$"The page '{start.Route.ToPath()}' has no region to show '{tail[0].Segment.Name}'"]
            };

        return await start.NavigateRouteAsync(initiator, new Routing.Route(tail, target.Data), historyMode, mode);
    }

    /// <summary>
    /// Goes back in the history of the window.
    /// </summary>
    /// <param name="navigator">Any navigator of the window.</param>
    /// <param name="initiator">The object that starts the navigation.</param>
    /// <param name="steps">The number of the entries to go back.</param>
    /// <returns>The response of the navigation; a failure if there is no entry to go back to.</returns>
    public static ValueTask<NavigationResponse> GoBackAsync([NotNull] this Navigator navigator, object initiator, int steps = 1)
     => navigator.GoAsync(initiator, -steps);

    /// <summary>
    /// Goes forward in the history of the window.
    /// </summary>
    /// <param name="navigator">Any navigator of the window.</param>
    /// <param name="initiator">The object that starts the navigation.</param>
    /// <param name="steps">The number of the entries to go forward.</param>
    /// <returns>The response of the navigation; a failure if there is no entry to go forward to.</returns>
    public static ValueTask<NavigationResponse> GoForwardAsync([NotNull] this Navigator navigator, object initiator, int steps = 1)
     => navigator.GoAsync(initiator, steps);

    /// <summary>
    /// Moves in the history of the window and shows the page of the entry that it reaches. The position moves only
    /// after the page was shown, so a failure leaves the history where it was.
    /// </summary>
    /// <param name="navigator">Any navigator of the window.</param>
    /// <param name="initiator">The object that starts the navigation.</param>
    /// <param name="delta">The number of the entries to move; negative goes back.</param>
    /// <returns>The response of the navigation; a failure if there is no entry at the new position.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="navigator"/> is <see langword="null"/>.</exception>
    public static async ValueTask<NavigationResponse> GoAsync([NotNull] this Navigator navigator, object initiator, int delta)
    {
        var root = (navigator ?? throw new ArgumentNullException(nameof(navigator))).RootNavigator;
        var mode = delta < 0 ? NavigationMode.Back : NavigationMode.Forward;
        var journal = root.Journal;
        var entries = journal.Entries;
        var index = journal.CurrentIndex + delta;
        if (delta == 0 || index < 0 || index >= entries.Count)
            return new NavigationFailedResponse(root.ActualRoute, root) { Mode = mode, Errors = ["The history has no entry to go to"] };

        var entry = entries[index];
        var response = await root.NavigateToAsync(initiator, entry.Route, HistoryMode.Skip, mode);
        if (response.Success)
        {
            journal.TryMoveTo(entry.Id);
        }
        else if (response is NavigationFailedResponse failed)
        {
            response = failed with { Retry = () => navigator.GoAsync(initiator, delta) };
        }

        return response;
    }

    /// <summary>
    /// Finds the segment among the nested segments of the region of the navigator.
    /// </summary>
    /// <typeparam name="TSegment">The type of the segment to find.</typeparam>
    /// <param name="navigator">The navigator whose region is searched.</param>
    /// <param name="filter">Selects the nested segments of the type.</param>
    /// <param name="segmentName">The name of the segment.</param>
    /// <returns>The segment, or <see langword="null"/> if there is none.</returns>
    public static TSegment? FindNestedSegmentToNavigateCore<TSegment>([NotNull] this Navigator navigator, Func<ImmutableArray<NameSegment>, IEnumerable<TSegment>> filter, string segmentName)
        where TSegment : NameSegment
    {
        NameSegment segment = navigator.Region.Segment;
        ImmutableArray<NameSegment> nested;
        if (segment.DataSegment is DataSegment dataSegment)
            nested = dataSegment.Nested;
        else
            nested = segment.Nested;

        return filter(nested).FirstOrDefault(x => x.Name == segmentName);
    }

    /// <summary>
    /// Finds the named segment among the nested segments of the region of the navigator.
    /// </summary>
    /// <param name="navigator">The navigator whose region is searched.</param>
    /// <param name="segmentName">The name of the segment.</param>
    /// <returns>The segment.</returns>
    /// <exception cref="NestedSegmentMissingException">The region has no such nested segment.</exception>
    public static NameSegment FindNestedSegmentToNavigate([NotNull] this Navigator navigator, string segmentName)
    {
        IEnumerable<NameSegment> OfExactType(ImmutableArray<NameSegment> segments) => segments.Where(s => s.GetType() == typeof(NameSegment));
        var nestedSegment = FindNestedSegmentToNavigateCore(navigator, OfExactType, segmentName);
        if (nestedSegment is not null) return nestedSegment;
        else throw new NestedSegmentMissingException(segmentName, navigator.Region.Segment.Name);
    }

    /// <summary>
    /// Finds the dialog segment among the nested segments of the region of the navigator or of its parents.
    /// </summary>
    /// <param name="navigator">The navigator whose region is searched first.</param>
    /// <param name="segmentName">The name of the dialog segment.</param>
    /// <returns>The segment.</returns>
    /// <exception cref="NestedSegmentMissingException">No region has such a dialog segment.</exception>
    public static DialogSegment FindDialogSegmentToNavigate([NotNull] this Navigator navigator, string segmentName)
    {
        Navigator targetNavigator = navigator.ChildNavigator ?? navigator;
        for (Navigator? current = targetNavigator; current is not null; current = current.Parent)
        {
            var dialogSegment = FindNestedSegmentToNavigateCore(current, s => s.OfType<DialogSegment>(), segmentName);
            if (dialogSegment is not null) return dialogSegment;
        }

        throw new NestedSegmentMissingException(segmentName, targetNavigator.Region.Segment.Name);
    }

    /// <summary>
    /// Navigates to the default nested segment of the segment, and on to the defaults below it.
    /// </summary>
    /// <param name="navigator">The navigator that navigates the first segment.</param>
    /// <param name="initiator">The object that starts the navigation.</param>
    /// <param name="segment">The segment whose default nested segment is navigated.</param>
    /// <returns>The response of the navigation.</returns>
    public static async ValueTask<NavigationResponse> NavigateDefaultAsync([NotNull] this Navigator navigator, object initiator, RouteSegment segment)
    {
        var defaultSegment = segment.Nested.SingleOrDefault(x => x.IsDefault);
        if (defaultSegment is not null)
        {
            var route = defaultSegment.BuildDefaultRoute(navigator.ServiceProvider);
            return await navigator.NavigateRouteAsync(initiator, route);
        }
        else
        {
            return new NoDefaultSegmentNavigationResponse(navigator.Route, navigator);
        }
    }

    /// <summary>
    /// Navigates to a nested segment of the region inside the content of the navigator.
    /// </summary>
    /// <param name="navigator">The navigator whose child region is used.</param>
    /// <param name="initiator">The object that starts the navigation.</param>
    /// <param name="segmentName">The name of the segment.</param>
    /// <param name="data">The data of the segment.</param>
    /// <returns>The response of the navigation.</returns>
    public static ValueTask<NavigationResponse> NavigateNestedSegmentAsync([NotNull] this Navigator navigator, object initiator, string segmentName, object? data = null)
    {
        var childNavigator = navigator.RequireChildNavigator();
        var nestedSegment = childNavigator.FindNestedSegmentToNavigate(segmentName);
        var route = nestedSegment.BuildDefaultRoute(navigator.ServiceProvider, data);
        return childNavigator.NavigateRouteAsync(initiator, route);
    }

    /// <summary>
    /// Navigates to a nested segment of the region of the navigator.
    /// </summary>
    /// <param name="navigator">The navigator whose region is used.</param>
    /// <param name="initiator">The object that starts the navigation.</param>
    /// <param name="segmentName">The name of the segment.</param>
    /// <param name="data">The data of the segment.</param>
    /// <returns>The response of the navigation.</returns>
    public static ValueTask<NavigationResponse> NavigateLocalSegmentAsync([NotNull] this Navigator navigator, object initiator, string segmentName, object? data = null)
    {
        var nestedSegment = navigator.FindNestedSegmentToNavigate(segmentName);
        return navigator.NavigateLocalSegmentAsync(initiator, nestedSegment, data);
    }

    /// <summary>
    /// Navigates to a nested segment of the region of the navigator.
    /// </summary>
    /// <param name="navigator">The navigator whose region is used.</param>
    /// <param name="initiator">The object that starts the navigation.</param>
    /// <param name="segment">The segment.</param>
    /// <param name="data">The data of the segment.</param>
    /// <returns>The response of the navigation.</returns>
    public static ValueTask<NavigationResponse> NavigateLocalSegmentAsync([NotNull] this Navigator navigator, object initiator, NameSegment segment, object? data = null)
    {
        var route = segment.BuildDefaultRoute(navigator.ServiceProvider, data);
        return navigator.NavigateRouteAsync(initiator, route);
    }

    /// <summary>
    /// Navigates to a dialog.
    /// </summary>
    /// <param name="navigator">The navigator that the dialog is requested through.</param>
    /// <param name="initiator">The object that starts the navigation.</param>
    /// <param name="segmentName">The name of the dialog segment.</param>
    /// <param name="data">The data of the dialog.</param>
    /// <returns>The response of the navigation.</returns>
    public static ValueTask<NavigationResponse> NavigateDialogSegmentAsync([NotNull] this Navigator navigator, object initiator, string segmentName, object? data = default)
    {
        var dialogSegment = navigator.FindDialogSegmentToNavigate(segmentName);
        var route = dialogSegment.BuildDefaultRoute(navigator.ServiceProvider, data);
        return navigator.NavigateRouteAsync(initiator, route);
    }
}
