namespace Ecierge.Uno.Navigation.Tests.Doubles;

using Ecierge.Uno.Navigation;

/// <summary>
/// A browser: a stack of entries with a position, an address bar and the Back and Forward buttons. Like a real browser
/// it raises the pop event after the code that moved it has returned, so a test delivers the events when it wants.
/// </summary>
internal sealed class FakeBrowserHistory : IBrowserHistory
{
    private readonly List<(string Path, string? Token)> entries = new();
    private readonly Queue<BrowserHistoryPopEventArgs> pending = new();

    /// <summary>
    /// Creates a browser that shows the page the application was loaded at, after entries that other pages made.
    /// </summary>
    /// <param name="earlier">The entries that were there before the application was loaded, the oldest first.</param>
    public FakeBrowserHistory(params (string Path, string? Token)[] earlier)
    {
        entries.AddRange(earlier);
        entries.Add(("/", null));
        Index = entries.Count - 1;
    }

    /// <summary>Gets what the application asked the browser to do, in order.</summary>
    public List<string> Log { get; } = new();

    /// <summary>Gets the position in the stack.</summary>
    public int Index { get; private set; }

    /// <summary>Gets the paths of the entries of the stack.</summary>
    public string[] Paths => entries.Select(entry => entry.Path).ToArray();

    /// <inheritdoc />
    public Uri CurrentUri => new("https://example.com" + entries[Index].Path);

    /// <inheritdoc />
    public event EventHandler<BrowserHistoryPopEventArgs>? PopState;

    /// <inheritdoc />
    public void Push(string path, string token)
    {
        Log.Add("push " + path);
        entries.RemoveRange(Index + 1, entries.Count - Index - 1);
        entries.Add((path, token));
        Index = entries.Count - 1;
    }

    /// <inheritdoc />
    public void Replace(string path, string token)
    {
        Log.Add("replace " + path);
        entries[Index] = (path, token);
    }

    /// <inheritdoc />
    public void Go(int delta)
    {
        Log.Add("go " + delta);
        MoveBy(delta);
    }

    /// <summary>The user presses the Back or Forward button, or picks an entry of the history menu.</summary>
    /// <param name="delta">The number of entries; negative goes back.</param>
    public void UserGoes(int delta) => MoveBy(delta);



    /// <summary>Takes the pop event of the next move, for a test that handles it itself and waits for the page.</summary>
    /// <returns>The event.</returns>
    public BrowserHistoryPopEventArgs TakeNextPop() => pending.Dequeue();

    /// <summary>Raises the pop events of the moves, as the browser does when the code has returned.</summary>
    public void DeliverPops()
    {
        while (pending.Count > 0)
        {
            PopState?.Invoke(this, pending.Dequeue());
        }
    }

    private void MoveBy(int delta)
    {
        var target = Index + delta;
        if (target < 0 || target >= entries.Count) return;

        Index = target;
        pending.Enqueue(new BrowserHistoryPopEventArgs(CurrentUri, entries[Index].Token));
    }
}

/// <summary>
/// The application side of the browser history: it records the pages it was asked to show, and says whether it could.
/// </summary>
internal sealed class FakeHistoryNavigation : IHistoryNavigation
{
    /// <summary>Gets the pages that were shown, in order.</summary>
    public List<string> Shown { get; } = new();

    /// <summary>Gets or sets what decides whether the page of an entry can be shown.</summary>
    public Func<JournalEntry, bool> CanShowEntry { get; set; } = _ => true;

    /// <summary>Gets or sets what decides whether the page of an address can be shown.</summary>
    public Func<string, bool> CanShowPath { get; set; } = _ => true;

    /// <summary>Gets or sets a task that a page waits for before it is shown, to hold a navigation back.</summary>
    public Func<Task>? Delay { get; set; }

    /// <inheritdoc />
    public async ValueTask<bool> ShowEntryAsync(JournalEntry entry, NavigationMode mode)
    {
        Shown.Add($"{mode} {entry.Path}");
        if (Delay is { } delay) await delay();
        return CanShowEntry(entry);
    }

    /// <inheritdoc />
    public async ValueTask<bool> ShowPathAsync(string routePath)
    {
        Shown.Add("path " + routePath);
        if (Delay is { } delay) await delay();
        return CanShowPath(routePath);
    }
}
