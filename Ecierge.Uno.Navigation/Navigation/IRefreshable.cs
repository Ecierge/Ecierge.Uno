namespace Ecierge.Uno.Navigation;

using System.Windows.Input;

/// <summary>
/// A page, or the view model of a page, that can load its data again, for example when the user clicks the
/// breadcrumb of the page that is shown.
/// </summary>
/// <remarks>
/// <para>
/// The page decides what loading again means: a list keeps its filter and its position, and an editor with changes
/// that are not saved asks before it drops them. The navigation only runs the command; it never creates the page
/// again.
/// </para>
/// </remarks>
/// <seealso cref="NavigatorExtensions.TryRefreshPage(Navigator)"/>
public interface IRefreshable
{
    /// <summary>
    /// Gets the command that loads the data of the page again.
    /// </summary>
    ICommand RefreshCommand { get; }
}
