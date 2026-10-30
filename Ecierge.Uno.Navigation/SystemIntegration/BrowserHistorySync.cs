namespace Ecierge.Uno.Navigation;

using System;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.Logging;

/// <summary>
/// Keeps the history of a browser in step with the journal of the window, in both directions.
/// </summary>
/// <remarks>
/// <para>
/// What the application records, the browser records: a page that is added to the journal is added to the history of
/// the browser, a page that replaces another one replaces its entry, and a move in the journal, such as the Back
/// button of the application, moves the browser as well. Every entry carries a token that names the journal and the
/// entry, so that the browser history is not confused by the entries of an earlier run of the application.
/// </para>
/// <para>
/// When the user moves in the browser, the page of the entry is shown, and the journal follows once the page is
/// shown. An entry that the application did not make, for example after a reload or an address typed by hand, is
/// opened from its address instead and replaces the current entry.
/// </para>
/// </remarks>
internal sealed class BrowserHistorySync : IDisposable
{
    private readonly NavigationJournal journal;
    private readonly IBrowserHistory history;
    private readonly NavigationOptions options;
    private readonly IHistoryNavigation navigation;
    private readonly ILogger logger;
    private readonly SemaphoreSlim pops = new(1, 1);

    // The browser already shows an entry when the application starts, and the first page takes its place
    private bool replaceNext = true;

    // Set while the synchronization moves the journal itself, so that the move is not written back to the browser
    private bool isMirroring;

    /// <summary>
    /// Starts to keep the browser history and the journal in step.
    /// </summary>
    /// <param name="journal">The journal of the window.</param>
    /// <param name="history">The history of the browser.</param>
    /// <param name="options">The options, which say where the application is hosted.</param>
    /// <param name="navigation">Shows the pages that the browser history asks for.</param>
    /// <param name="logger">The logger.</param>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="journal"/>, <paramref name="history"/>, <paramref name="options"/>,
    /// <paramref name="navigation"/> or <paramref name="logger"/> is <see langword="null"/>.
    /// </exception>
    public BrowserHistorySync(
        NavigationJournal journal,
        IBrowserHistory history,
        NavigationOptions options,
        IHistoryNavigation navigation,
        ILogger logger)
    {
        this.journal = journal ?? throw new ArgumentNullException(nameof(journal));
        this.history = history ?? throw new ArgumentNullException(nameof(history));
        this.options = options ?? throw new ArgumentNullException(nameof(options));
        this.navigation = navigation ?? throw new ArgumentNullException(nameof(navigation));
        this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
        journal.Changed += OnJournalChanged;
        history.PopState += OnPopState;
    }

    /// <summary>
    /// Stops to keep the browser history and the journal in step.
    /// </summary>
    public void Dispose()
    {
        journal.Changed -= OnJournalChanged;
        history.PopState -= OnPopState;
        pops.Dispose();
    }

    private string TokenOf(JournalEntry entry)
     => journal.SessionId + ":" + entry.Id.ToString(CultureInfo.InvariantCulture);

    private string PathOf(JournalEntry entry) => BrowserHistoryUrl.Create(entry.Path, options.BasePath);

    private void OnJournalChanged(object? sender, JournalChangedEventArgs e)
    {
        if (isMirroring) return;

        switch (e.Change)
        {
            case JournalChange.Pushed when e.Entry is { } pushed:
                if (replaceNext)
                    history.Replace(PathOf(pushed), TokenOf(pushed));
                else
                    history.Push(PathOf(pushed), TokenOf(pushed));
                replaceNext = false;
                break;
            case JournalChange.Replaced when e.Entry is { } replaced:
                history.Replace(PathOf(replaced), TokenOf(replaced));
                replaceNext = false;
                break;
            case JournalChange.Moved:
                history.Go(e.Index - e.PreviousIndex);
                break;
            case JournalChange.Cleared:
                // The browser cannot forget its entries, so the page that follows takes the place of the current one
                replaceNext = true;
                break;
        }
    }

    private async void OnPopState(object? sender, BrowserHistoryPopEventArgs e)
    {
#pragma warning disable CA1031 // Do not catch general exception types
        try
        {
            await HandlePopAsync(e).ConfigureAwait(true);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to follow the browser to {Uri}", e.Uri);
        }
#pragma warning restore CA1031 // Do not catch general exception types
    }

    /// <summary>
    /// Follows the browser to the entry it moved to. Moves that arrive while a page is being shown wait for it.
    /// </summary>
    /// <param name="e">The entry.</param>
    /// <returns>A task that completes when the application has followed.</returns>
    internal async Task HandlePopAsync(BrowserHistoryPopEventArgs e)
    {
        await pops.WaitAsync().ConfigureAwait(true);
        try
        {
            if (TryFindEntry(e.Token, out var entry))
            {
                var delta = journal.IndexOf(entry.Id) - journal.CurrentIndex;
                // The echo of a move that the application made itself, such as its own Back button
                if (delta == 0) return;

                var shown = await navigation.ShowEntryAsync(entry, delta < 0 ? NavigationMode.Back : NavigationMode.Forward).ConfigureAwait(true);
                if (shown)
                {
                    WithoutMirroring(() => journal.TryMoveTo(entry.Id));
                }
                else
                {
                    // The page could not be shown, so the browser returns to the entry that the journal still has
                    logger.LogWarning("The page {Path} could not be shown, so the browser returns to the current page", entry.Path);
                    history.Go(-delta);
                }

                return;
            }

            // Not an entry of this journal: a reload, an address typed by hand, or an entry of an earlier run
            if (DeepLinkMapper.ToRoutePath(e.Uri, options) is not { } routePath)
            {
                logger.LogDebug("The address {Uri} is not a page of the application", e.Uri);
                RestoreAddress();
                return;
            }

            if (!await navigation.ShowPathAsync(routePath).ConfigureAwait(true))
            {
                logger.LogWarning("The page {Path} of the address could not be opened", routePath);
                RestoreAddress();
            }
        }
        finally
        {
            pops.Release();
        }
    }

    // The browser shows an address that the application did not open, so the address bar would lie about the page
    private void RestoreAddress()
    {
        if (journal.Current is { } current)
        {
            history.Replace(PathOf(current), TokenOf(current));
        }
    }

    private bool TryFindEntry(string? token, out JournalEntry entry)
    {
        entry = null!;
        if (token is null) return false;

        var separator = token.IndexOf(':', StringComparison.Ordinal);
        if (separator < 0
            || !string.Equals(token[..separator], journal.SessionId, StringComparison.Ordinal)
            || !long.TryParse(token[(separator + 1)..], NumberStyles.None, CultureInfo.InvariantCulture, out var id))
            return false;

        var found = journal.Entries.FirstOrDefault(candidate => candidate.Id == id);
        if (found is null) return false;

        entry = found;
        return true;
    }

    private void WithoutMirroring(Action action)
    {
        isMirroring = true;
        try
        {
            action();
        }
        finally
        {
            isMirroring = false;
        }
    }
}

/// <summary>
/// Writes the path of the address that a route has in the browser.
/// </summary>
internal static class BrowserHistoryUrl
{
    /// <summary>
    /// Writes the path of the address for a route.
    /// </summary>
    /// <param name="routePath">The text of the route.</param>
    /// <param name="basePath">The path under which the application is hosted.</param>
    /// <returns>The path of the address, from the root of the site.</returns>
    public static string Create(string routePath, string basePath)
    {
        var prefix = "/" + basePath.Trim('/');
        if (prefix != "/") prefix += "/";

        var segments = routePath.Split('/').Select(Escape);
        return prefix + string.Join('/', segments);
    }

    // The dialog qualifier is written as it is, as the browser does not need it escaped
    private static string Escape(string segment)
     => segment.StartsWith('!') ? "!" + Uri.EscapeDataString(segment[1..]) : Uri.EscapeDataString(segment);
}
