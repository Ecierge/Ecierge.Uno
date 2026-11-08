namespace Ecierge.Uno.Navigation;

using System;
using System.Threading.Tasks;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

/// <summary>
/// Opens the pages that links name.
/// </summary>
public static class DeepLinkExtensions
{
    /// <summary>
    /// Opens the page that a link names. The shell and the pages that the link has in common with what is shown stay as
    /// they are, and the rules decide whether the user may see the page, like for any other navigation.
    /// </summary>
    /// <param name="navigator">Any navigator of the window.</param>
    /// <param name="initiator">The object that starts the navigation.</param>
    /// <param name="link">The link.</param>
    /// <param name="historyMode">What the navigation does to the history.</param>
    /// <returns>
    /// The response of the navigation. It is a failure when the link does not name a page of the application, with the
    /// reason; a link of the application that names no page is a failure too, with the exception.
    /// </returns>
    public static async ValueTask<NavigationResponse> NavigateToLinkAsync(
        this Navigator navigator,
        object initiator,
        Uri link,
        HistoryMode historyMode = HistoryMode.Push)
    {
        navigator = navigator ?? throw new ArgumentNullException(nameof(navigator));
        link = link ?? throw new ArgumentNullException(nameof(link));

        var root = navigator.RootNavigator;
        var options = root.ServiceProvider.GetRequiredService<IOptions<NavigationOptions>>().Value;
        if (DeepLinkMapper.ToRoutePath(link, options) is not { } path)
            return new NavigationFailedResponse(root.ActualRoute, root) { Errors = [$"'{link}' is not a link to a page of the application"] };

        if (!root.TryResolveRoute(path, null, out var resolved, out var error))
            return new NavigationFailedResponse(root.ActualRoute, root) { Error = error, Errors = [error.Message] };

        return await root.NavigateToAsync(initiator, resolved.Route, historyMode);
    }
}
