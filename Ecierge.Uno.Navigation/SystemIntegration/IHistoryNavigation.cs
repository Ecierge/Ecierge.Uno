namespace Ecierge.Uno.Navigation;

using System.Threading.Tasks;

/// <summary>
/// What the history of a browser asks of the application when the user moves in it.
/// </summary>
internal interface IHistoryNavigation
{
    /// <summary>
    /// Shows the page of an entry of the journal, without changing the journal.
    /// </summary>
    /// <param name="entry">The entry.</param>
    /// <param name="mode">Whether the user went back or forward.</param>
    /// <returns><see langword="true"/> if the page is shown.</returns>
    ValueTask<bool> ShowEntryAsync(JournalEntry entry, NavigationMode mode);

    /// <summary>
    /// Opens the page of a route that the application did not record, replacing the current entry of the journal.
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
    public async ValueTask<bool> ShowEntryAsync(JournalEntry entry, NavigationMode mode)
     => (await root.NavigateToAsync(this, entry.Route, HistoryMode.Skip, mode)).Success;

    public async ValueTask<bool> ShowPathAsync(string routePath)
    {
        if (!root.TryResolveRoute(routePath, null, out var resolved, out _)) return false;
        return (await root.NavigateToAsync(this, resolved.Route, HistoryMode.Replace)).Success;
    }
}
