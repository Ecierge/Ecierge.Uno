namespace Ecierge.Uno.Navigation;

using System.Threading.Tasks;

/// <summary>
/// What the history of a browser asks of the application when the user moves in it.
/// </summary>
internal interface IHistoryNavigation
{
    /// <summary>
    /// Shows the page of an entry of the journal. The journal is changed only by <paramref name="onShown"/>.
    /// </summary>
    /// <param name="entry">The entry.</param>
    /// <param name="mode">Whether the user went back or forward.</param>
    /// <param name="onShown">
    /// Moves the journal to the entry. It runs when the page is shown and before the change of the route is announced,
    /// so that whoever listens to the change finds the journal at the page that is shown.
    /// </param>
    /// <returns><see langword="true"/> if the page is shown.</returns>
    ValueTask<bool> ShowEntryAsync(JournalEntry entry, NavigationMode mode, Action onShown);

    /// <summary>
    /// Opens the page of a route that the application did not record. The journal starts anew from the page, because
    /// the browser moved to a position that the journal knows nothing about.
    /// </summary>
    /// <param name="routePath">The text of the route.</param>
    /// <returns><see langword="true"/> if the page is shown.</returns>
    ValueTask<bool> ShowPathAsync(string routePath);
}

/// <summary>
/// Shows the pages through the root navigator of the window.
/// </summary>
internal sealed class RootHistoryNavigation(Navigator root) : IHistoryNavigation
{
    public async ValueTask<bool> ShowEntryAsync(JournalEntry entry, NavigationMode mode, Action onShown)
     => (await root.RunAsNavigationAsync(HistoryMode.Skip, mode, async () =>
        {
            var response = await root.NavigateToAsync(this, entry.Route, HistoryMode.Skip, mode);
            if (response.Success) onShown();
            return response;
        })).Success;

    public async ValueTask<bool> ShowPathAsync(string routePath)
    {
        if (!root.TryResolveRoute(routePath, null, out var resolved, out _)) return false;

        // The entries of the journal stand for the entries of the browser at known distances, which the entry of an
        // unknown address breaks, so the journal starts again with the page of the address
        return (await root.NavigateToAsync(this, resolved.Route, HistoryMode.Reset)).Success;
    }
}
