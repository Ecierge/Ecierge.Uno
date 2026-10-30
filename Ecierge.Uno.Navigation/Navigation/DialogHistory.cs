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
    /// Moves the history back by one entry when the user closed a dialog with one of its own buttons, so that the
    /// entry of the dialog is not the current one while the page below it is shown. It does nothing when dialogs are
    /// not kept in the history, or when the current entry is not the entry of this dialog because a navigation has
    /// already moved on.
    /// </summary>
    /// <param name="dialogNavigator">The navigator of the dialog that was closed.</param>
    public static void OnClosedByUser(Navigator dialogNavigator)
    {
        var options = dialogNavigator.ServiceProvider.GetService<IOptions<NavigationOptions>>()?.Value;
        if (options is null || !options.DialogsInHistory) return;

        var journal = dialogNavigator.Journal;
        var dialogName = dialogNavigator.Region.Segment.Name;
        if (journal.CanGoBack
            && journal.Current is { } current
            && current.Route.Segments.Any(step => step.IsDialog && step.Segment.Name == dialogName))
        {
            journal.Move(-1);
        }
    }
}
