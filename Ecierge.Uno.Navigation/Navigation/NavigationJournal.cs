namespace Ecierge.Uno.Navigation;

using System;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// One entry of the history: a route from the root to the page that was shown.
/// </summary>
/// <param name="Id">The number of the entry, which is not repeated in the journal. The browser history keeps it.</param>
/// <param name="Path">The route written as text, as it is in the address bar.</param>
/// <param name="Route">
/// The route with its data. The data holds the handles of the entities, not copies of them, so an entry shows the
/// entity as it is now.
/// </param>
[ImplicitKeys(IsEnabled = false)]
public sealed record JournalEntry(long Id, string Path, Routing.Route Route);

/// <summary>
/// What changed in the journal.
/// </summary>
public enum JournalChange
{
    /// <summary>A new entry was added after the current one.</summary>
    Pushed,

    /// <summary>The current entry was replaced by another one.</summary>
    Replaced,

    /// <summary>The position in the journal moved to another entry.</summary>
    Moved,

    /// <summary>All the entries were removed.</summary>
    Cleared
}

/// <summary>
/// Describes a change of the journal.
/// </summary>
public sealed class JournalChangedEventArgs : EventArgs
{
    internal JournalChangedEventArgs(JournalChange change, JournalEntry? entry, int index, int previousIndex)
    {
        Change = change;
        Entry = entry;
        Index = index;
        PreviousIndex = previousIndex;
    }

    /// <summary>Gets what changed.</summary>
    public JournalChange Change { get; }

    /// <summary>Gets the entry that is current after the change, or <see langword="null"/> if the journal is empty.</summary>
    public JournalEntry? Entry { get; }

    /// <summary>Gets the position of the current entry after the change.</summary>
    public int Index { get; }

    /// <summary>Gets the position of the current entry before the change.</summary>
    public int PreviousIndex { get; }
}

/// <summary>
/// The history of the routes that the user visited, in the order of the visits, and the position in it. Going back and
/// forward moves the position; a new visit after a move drops the entries that were ahead.
/// </summary>
/// <remarks>
/// <para>
/// There is one journal for a window, whichever navigator showed the page. Every entry is the whole route from the
/// root to the page, so going to an entry shows the page and what contains it.
/// </para>
/// <para>
/// The journal is safe to use from any thread. <see cref="Changed"/> is raised on the thread that made the change.
/// </para>
/// </remarks>
public sealed class NavigationJournal
{
    private readonly object gate = new();
    private readonly List<JournalEntry> entries = new();
    private readonly int capacity;
    private int currentIndex = -1;
    private long nextId;

    /// <summary>
    /// Creates an empty journal.
    /// </summary>
    /// <param name="capacity">The number of the entries to keep; the oldest is dropped when a new one does not fit.</param>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="capacity"/> is less than 1.</exception>
    public NavigationJournal(int capacity = 100)
    {
        if (capacity < 1) throw new ArgumentOutOfRangeException(nameof(capacity), "A journal keeps at least one entry.");
        this.capacity = capacity;
    }

    /// <summary>
    /// Gets a value that tells this journal from the journals of the earlier runs of the application, for the history
    /// entries that a browser keeps across a reload.
    /// </summary>
    public string SessionId { get; } = Guid.NewGuid().ToString("N");

    /// <summary>
    /// Raised after the journal changed.
    /// </summary>
    public event EventHandler<JournalChangedEventArgs>? Changed;

    /// <summary>
    /// Gets a copy of the entries, the oldest first.
    /// </summary>
    public IReadOnlyList<JournalEntry> Entries
    {
        get
        {
            lock (gate)
            {
                return entries.ToArray();
            }
        }
    }

    /// <summary>
    /// Gets the position of the current entry, or -1 if the journal is empty.
    /// </summary>
    public int CurrentIndex
    {
        get
        {
            lock (gate)
            {
                return currentIndex;
            }
        }
    }

    /// <summary>
    /// Gets the entry of the page that is shown, or <see langword="null"/> if the journal is empty.
    /// </summary>
    public JournalEntry? Current
    {
        get
        {
            lock (gate)
            {
                return currentIndex >= 0 ? entries[currentIndex] : null;
            }
        }
    }

    /// <summary>
    /// Gets a value that tells whether there is an entry before the current one.
    /// </summary>
    public bool CanGoBack
    {
        get
        {
            lock (gate)
            {
                return currentIndex > 0;
            }
        }
    }

    /// <summary>
    /// Gets a value that tells whether there is an entry after the current one.
    /// </summary>
    public bool CanGoForward
    {
        get
        {
            lock (gate)
            {
                return currentIndex >= 0 && currentIndex < entries.Count - 1;
            }
        }
    }

    /// <summary>
    /// Finds the position of the entry with the number.
    /// </summary>
    /// <param name="id">The number of the entry.</param>
    /// <returns>The position, or -1 if the journal has no such entry.</returns>
    public int IndexOf(long id)
    {
        lock (gate)
        {
            return entries.FindIndex(entry => entry.Id == id);
        }
    }

    /// <summary>
    /// Records the route that was shown.
    /// </summary>
    /// <param name="route">The whole route from the root to the page.</param>
    /// <param name="mode">What to do with the history.</param>
    /// <returns>The entry of the route, or <see langword="null"/> if the history is not changed.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="route"/> is <see langword="null"/>.</exception>
    public JournalEntry? Record(Routing.Route route, HistoryMode mode)
    {
        route = route ?? throw new ArgumentNullException(nameof(route));
        if (mode == HistoryMode.Skip) return null;

        var path = route.ToPath();
        JournalEntry entry;
        JournalChangedEventArgs args;
        lock (gate)
        {
            var current = currentIndex >= 0 ? entries[currentIndex] : null;
            if (current is not null && string.Equals(current.Path, path, StringComparison.OrdinalIgnoreCase))
            {
                // The same location again, for example a dialog that the history does not keep: only the route is
                // fresher, and the browser history needs no new entry
                entry = current with { Route = route };
                entries[currentIndex] = entry;
                return entry;
            }

            var previous = currentIndex;
            entry = new JournalEntry(nextId++, path, route);
            if (mode == HistoryMode.Replace && current is not null)
            {
                entries[currentIndex] = entry;
                args = new JournalChangedEventArgs(JournalChange.Replaced, entry, currentIndex, previous);
            }
            else
            {
                if (currentIndex < entries.Count - 1)
                {
                    entries.RemoveRange(currentIndex + 1, entries.Count - currentIndex - 1);
                }

                entries.Add(entry);
                currentIndex = entries.Count - 1;
                if (entries.Count > capacity)
                {
                    entries.RemoveAt(0);
                    currentIndex--;
                    previous--;
                }

                args = new JournalChangedEventArgs(JournalChange.Pushed, entry, currentIndex, previous);
            }
        }

        Changed?.Invoke(this, args);
        return entry;
    }

    /// <summary>
    /// Moves the position by a number of entries.
    /// </summary>
    /// <param name="delta">The number of entries; negative goes back.</param>
    /// <exception cref="ArgumentOutOfRangeException">There is no entry at the new position.</exception>
    public void Move(int delta) => MoveTo(CurrentIndex + delta);

    /// <summary>
    /// Moves the position to an entry.
    /// </summary>
    /// <param name="index">The position of the entry.</param>
    /// <exception cref="ArgumentOutOfRangeException">There is no entry at the position.</exception>
    public void MoveTo(int index)
    {
        JournalChangedEventArgs args;
        lock (gate)
        {
            if (index < 0 || index >= entries.Count)
                throw new ArgumentOutOfRangeException(nameof(index), index, "The journal has no entry at this position.");
            if (index == currentIndex) return;

            var previous = currentIndex;
            currentIndex = index;
            args = new JournalChangedEventArgs(JournalChange.Moved, entries[index], index, previous);
        }

        Changed?.Invoke(this, args);
    }

    /// <summary>
    /// Moves the position to the entry with the number.
    /// </summary>
    /// <param name="id">The number of the entry.</param>
    /// <returns><see langword="true"/> if the journal has the entry.</returns>
    public bool TryMoveTo(long id)
    {
        var index = IndexOf(id);
        if (index < 0) return false;
        MoveTo(index);
        return true;
    }

    /// <summary>
    /// Removes all the entries.
    /// </summary>
    public void Clear()
    {
        JournalChangedEventArgs args;
        lock (gate)
        {
            if (entries.Count == 0) return;
            var previous = currentIndex;
            entries.Clear();
            currentIndex = -1;
            args = new JournalChangedEventArgs(JournalChange.Cleared, null, -1, previous);
        }

        Changed?.Invoke(this, args);
    }
}
