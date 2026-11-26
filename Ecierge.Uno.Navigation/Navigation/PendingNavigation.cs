namespace Ecierge.Uno.Navigation;

using System;

/// <summary>
/// Holds a route that was asked for before the application could open it, such as the page of a link that started the
/// application while the user was not signed in. The code that opens the first page takes it, and the code that signs
/// the user in takes it again if the sign-in came first.
/// </summary>
public interface IPendingNavigation
{
    /// <summary>
    /// Gets the text of the route that waits, or <see langword="null"/> if none does.
    /// </summary>
    string? Path { get; }

    /// <summary>
    /// Sets the route that waits, replacing the one that waited before.
    /// </summary>
    /// <param name="path">The text of the route.</param>
    void Set(string path);

    /// <summary>
    /// Takes the route that waits, so that it is opened once.
    /// </summary>
    /// <returns>The text of the route, or <see langword="null"/> if none waited.</returns>
    string? Consume();
}

internal sealed class PendingNavigation : IPendingNavigation
{
    private readonly object gate = new();
    private string? path;

    public string? Path
    {
        get
        {
            lock (gate)
            {
                return path;
            }
        }
    }

    public void Set(string path)
    {
        if (string.IsNullOrWhiteSpace(path)) throw new ArgumentException("A route that waits has a text.", nameof(path));
        lock (gate)
        {
            this.path = path;
        }
    }

    public string? Consume()
    {
        lock (gate)
        {
            var taken = path;
            path = null;
            return taken;
        }
    }
}
