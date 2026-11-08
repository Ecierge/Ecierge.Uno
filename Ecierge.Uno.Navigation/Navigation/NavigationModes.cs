namespace Ecierge.Uno.Navigation;

/// <summary>
/// Tells how a navigation was started, as the navigation mode of a WinUI frame does.
/// </summary>
public enum NavigationMode
{
    /// <summary>The user or the code navigated to a new route.</summary>
    New,

    /// <summary>The route is an earlier entry of the history that the user went back to.</summary>
    Back,

    /// <summary>The route is a later entry of the history that the user went forward to.</summary>
    Forward,

    /// <summary>The route is repeated, for example by a retry after a failed navigation.</summary>
    Refresh
}

/// <summary>
/// Tells what a navigation does to the history of the visited routes.
/// </summary>
public enum HistoryMode
{
    /// <summary>The route is added to the history after the current entry, and the entries that were ahead are dropped.</summary>
    Push,

    /// <summary>The route replaces the current entry. A redirect uses it, so that Back does not return to the page it left.</summary>
    Replace,

    /// <summary>The history is not changed. Going back and forward use it, as they move the position themselves.</summary>
    Skip
}
