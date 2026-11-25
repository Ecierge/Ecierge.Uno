namespace Ecierge.Uno.Navigation;

using System.Linq;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

/// <summary>
/// Keeps the history in step with the dialogs that the user closes.
/// </summary>
internal static class DialogHistory
{
    /// <summary>
    /// Brings the history in step with a dialog that the user closed with one of its own buttons, so that the entry of
    /// the dialog is not the current one while the page below it is shown. It does nothing when dialogs are not kept in
    /// the history, or when the current entry is not the entry of this dialog because a navigation has already moved
    /// on.
    /// </summary>
    /// <remarks>
    /// The dialog is detached from its parent when this is called, so the route that is shown is the one of the page.
    /// The history moves back when the entry before the dialog is that page, as when the dialog was opened over it. A
    /// dialog that was opened together with its page, in one navigation, has no entry of the page before it, so the
    /// entry of the dialog is replaced by the page.
    /// </remarks>
    /// <param name="dialogNavigator">The navigator of the dialog that was closed.</param>
    public static void OnClosedByUser(Navigator dialogNavigator)
    {
        var options = dialogNavigator.ServiceProvider.GetService<IOptions<NavigationOptions>>()?.Value;
        if (options is null || !options.DialogsInHistory) return;

        var journal = dialogNavigator.Journal;
        var dialogName = dialogNavigator.Region.Segment.Name;
        if (journal.Current is not { } current
            || !current.Route.Segments.Any(step => step.IsDialog && step.Segment.Name == dialogName))
            return;

        var shown = dialogNavigator.RootNavigator.ActualRoute;
        if (journal.CanGoBack && IsEntryOf(journal.Entries[journal.CurrentIndex - 1], shown))
        {
            journal.Move(-1);
        }
        else if (!shown.Segments.IsEmpty)
        {
            journal.Record(shown, HistoryMode.Replace);
        }
    }

    private static bool IsEntryOf(JournalEntry entry, Routing.Route route)
     => string.Equals(entry.Path, route.ToPath(), StringComparison.OrdinalIgnoreCase);
}
