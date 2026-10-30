namespace Ecierge.Uno.Navigation;

using System;

/// <summary>
/// Gives the links that open the application: the one it was started with, and the ones that reach it while it runs.
/// </summary>
/// <remarks>
/// <para>
/// A platform registers its implementation: Windows and the desktop read the link from the activation of the process,
/// and the browser reads its address bar. A platform without links registers none.
/// </para>
/// <para>
/// The link is not yet a route. <see cref="DeepLinkMapper"/> turns it into the text of a route, which the application
/// then opens with its own rules.
/// </para>
/// </remarks>
public interface IDeepLinkSource
{
    /// <summary>
    /// Gets the link that the application was started with, or <see langword="null"/> if it was started without one.
    /// </summary>
    Uri? LaunchUri { get; }

    /// <summary>
    /// Raised when a link reaches the application while it runs, for example when the user opens a second link on a
    /// platform that hands it to the running instance.
    /// </summary>
    event EventHandler<Uri>? Received;
}
