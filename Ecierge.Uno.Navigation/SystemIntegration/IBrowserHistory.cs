namespace Ecierge.Uno.Navigation;

using System;

/// <summary>
/// The history of a browser: the entries that its Back and Forward buttons walk through, and the address bar.
/// </summary>
/// <remarks>
/// Only the browser has one, so only WebAssembly registers an implementation. Every entry that the application adds
/// carries a token, which the browser hands back when the user returns to the entry, so that the application can tell
/// its own entries from the ones it did not make.
/// </remarks>
public interface IBrowserHistory
{
    /// <summary>
    /// Gets the address that the browser shows now.
    /// </summary>
    Uri CurrentUri { get; }

    /// <summary>
    /// Adds an entry after the current one, dropping the entries that were ahead, and shows its address.
    /// </summary>
    /// <param name="path">The path of the address, from the root of the site.</param>
    /// <param name="token">The token that is given back when the user returns to the entry.</param>
    void Push(string path, string token);

    /// <summary>
    /// Replaces the current entry and its address.
    /// </summary>
    /// <param name="path">The path of the address, from the root of the site.</param>
    /// <param name="token">The token that is given back when the user returns to the entry.</param>
    void Replace(string path, string token);

    /// <summary>
    /// Moves in the history as the Back and Forward buttons do. The browser raises <see cref="PopState"/> afterwards.
    /// </summary>
    /// <param name="delta">The number of entries to move; negative goes back.</param>
    void Go(int delta);

    /// <summary>
    /// Raised when the user, or <see cref="Go(int)"/>, moved to another entry of the history.
    /// </summary>
    event EventHandler<BrowserHistoryPopEventArgs>? PopState;
}

/// <summary>
/// Describes the entry of the browser history that was moved to.
/// </summary>
public sealed class BrowserHistoryPopEventArgs : EventArgs
{
    /// <summary>
    /// Creates the arguments.
    /// </summary>
    /// <param name="uri">The address of the entry.</param>
    /// <param name="token">The token the application gave the entry, or <see langword="null"/> if it did not make the entry.</param>
    public BrowserHistoryPopEventArgs(Uri uri, string? token)
    {
        Uri = uri ?? throw new ArgumentNullException(nameof(uri));
        Token = token;
    }

    /// <summary>Gets the address of the entry.</summary>
    public Uri Uri { get; }

    /// <summary>Gets the token the application gave the entry, or <see langword="null"/> if it did not make the entry.</summary>
    public string? Token { get; }
}
