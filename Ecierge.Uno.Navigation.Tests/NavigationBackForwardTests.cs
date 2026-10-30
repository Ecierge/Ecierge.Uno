namespace Ecierge.Uno.Navigation.Tests;

using Ecierge.Uno.Navigation;
using Ecierge.Uno.Navigation.Tests.Doubles;

/// <summary>
/// Going back and forward in the history of the window: the shell and the layout that are right stay, the position
/// moves only after the page was shown, and the history is not extended by the move.
/// </summary>
[TestClass, UnitTestCategory, NavigationTestCategory]
public class NavigationBackForwardTests
{
    private static string[] Names(FakeNavigator navigator) => navigator.Requests.Select(r => r.NameSegment.Name).ToArray();

    private static string[] Paths(NavigationTestHost host) => host.Root.Journal.Entries.Select(e => e.Path).ToArray();

    /// <summary>Opens the menu of building b1 and then its assets, and gives the navigator of the building pages.</summary>
    private static async Task<(NavigationTestHost Host, FakeNavigator Layout)> OpenMenuThenAssets(BuildingServer? server = null)
    {
        var host = TestRoutes.CreateHost(server ?? new BuildingServer());
        await host.Root.NavigateRouteAsync(typeof(NavigationBackForwardTests), "Building/b1/Menu");
        var layout = (FakeNavigator)host.Root.ChildNavigator!;
        await layout.NavigateRouteAsync(typeof(NavigationBackForwardTests), "Assets");
        return (host, layout);
    }

    [TestMethod]
    public async Task GoBackAsync_must_show_the_previous_page_and_keep_the_building_page()
    {
        var (host, layout) = await OpenMenuThenAssets();
        using var _ = host;

        var response = await layout.GoBackAsync(this);

        Assert.IsTrue(response.Success, "Expected the previous page to be shown.");
        Assert.AreEqual(NavigationMode.Back, response.Mode, "Expected pages to be able to tell back from a new navigation.");
        CollectionAssert.AreEqual(new[] { "Menu", "Assets", "Menu" }, Names(layout), "Expected the previous page to be created again in the region of the building.");
        Assert.ContainsSingle(host.Root.Requests, "Expected the page of the building, which is right, not to be created again.");
    }

    [TestMethod]
    public async Task GoBackAsync_must_move_the_position_without_adding_an_entry()
    {
        var (host, layout) = await OpenMenuThenAssets();
        using var _ = host;

        await layout.GoBackAsync(this);

        CollectionAssert.AreEqual(new[] { "Building/b1/Menu", "Building/b1/Assets" }, Paths(host), "Expected going back not to extend the history.");
        Assert.AreEqual(0, host.Root.Journal.CurrentIndex, "Expected the previous entry to be current.");
        Assert.IsTrue(host.Root.Journal.CanGoForward, "Expected the page that was left to be ahead.");
    }

    [TestMethod]
    public async Task GoForwardAsync_must_return_to_the_page_that_back_left()
    {
        var (host, layout) = await OpenMenuThenAssets();
        using var _ = host;
        await layout.GoBackAsync(this);

        var response = await layout.GoForwardAsync(this);

        Assert.IsTrue(response.Success, "Expected the page to be shown again.");
        Assert.AreEqual(NavigationMode.Forward, response.Mode, "Expected forward to be told apart.");
        Assert.AreEqual("Assets", Names(layout)[^1], "Expected the page that was left.");
        Assert.AreEqual(1, host.Root.Journal.CurrentIndex, "Expected the position to return.");
    }

    [TestMethod]
    public async Task GoBackAsync_without_an_earlier_entry_must_fail_and_change_nothing()
    {
        using var host = TestRoutes.CreateHost(new BuildingServer());
        await host.Root.NavigateRouteAsync(this, "Login");

        var response = await host.Root.GoBackAsync(this);

        Assert.IsInstanceOfType<NavigationFailedResponse>(response, "Expected a failure when there is no page to go back to.");
        CollectionAssert.AreEqual(new[] { "Login" }, Paths(host), "Expected the history not to change.");
        Assert.ContainsSingle(host.Root.Requests, "Expected no navigation.");
    }

    [TestMethod]
    public async Task GoForwardAsync_at_the_last_entry_must_fail()
    {
        var (host, layout) = await OpenMenuThenAssets();
        using var _ = host;

        var response = await layout.GoForwardAsync(this);

        Assert.IsInstanceOfType<NavigationFailedResponse>(response, "Expected a failure when nothing is ahead.");
        Assert.AreEqual(1, host.Root.Journal.CurrentIndex, "Expected the position not to change.");
    }

    [TestMethod]
    public async Task A_new_navigation_after_going_back_must_drop_the_forward_history()
    {
        var (host, layout) = await OpenMenuThenAssets();
        using var _ = host;
        await layout.GoBackAsync(this);

        await layout.NavigateRouteAsync(this, "Details/Floors");

        CollectionAssert.AreEqual(new[] { "Building/b1/Menu", "Building/b1/Details/Floors" }, Paths(host), "Expected the page that was left to be gone from the history.");
        Assert.IsFalse(host.Root.Journal.CanGoForward, "Expected nothing ahead.");
    }

    [TestMethod]
    public async Task GoAsync_must_move_over_several_entries()
    {
        var (host, layout) = await OpenMenuThenAssets();
        using var _ = host;
        await layout.NavigateRouteAsync(this, "Details/Floors");

        var response = await layout.GoAsync(this, -2);

        Assert.IsTrue(response.Success, "Expected the first page to be shown.");
        Assert.AreEqual(0, host.Root.Journal.CurrentIndex, "Expected the position to move by two.");
        Assert.AreEqual("Menu", Names(layout)[^1], "Expected the page of the entry that was reached.");
    }

    [TestMethod]
    public async Task A_failed_back_navigation_must_leave_the_position_and_offer_a_retry()
    {
        var (host, layout) = await OpenMenuThenAssets();
        using var _ = host;
        var fail = true;
        host.Plan.Intercept = (_, request) => fail && request.NameSegment.Name == "Menu" ? new NavigationResult("The page cannot be shown") : null;

        var response = await layout.GoBackAsync(this);

        var failed = response as NavigationFailedResponse;
        Assert.IsNotNull(failed, "Expected the failure of the page to be reported.");
        Assert.AreEqual(1, host.Root.Journal.CurrentIndex, "Expected the position to stay, as the previous page was not shown.");
        Assert.IsNotNull(failed.Retry, "Expected going back to be repeatable.");

        fail = false;
        var retried = await failed.Retry();

        Assert.IsTrue(retried.Success, "Expected the retry to show the page.");
        Assert.AreEqual(0, host.Root.Journal.CurrentIndex, "Expected the position to move once the page was shown.");
    }

    [TestMethod]
    public async Task Going_back_to_another_building_must_not_load_the_buildings_again()
    {
        var server = new BuildingServer();
        using var host = TestRoutes.CreateHost(server);
        await host.Root.NavigateRouteAsync(this, "Building/b1/Menu");
        await host.Root.NavigateRouteAsync(this, "Building/b2/Menu");

        var back = await host.Root.GoBackAsync(this);
        var forward = await host.Root.GoForwardAsync(this);

        Assert.IsTrue(back.Success && forward.Success, "Expected both moves to succeed.");
        CollectionAssert.AreEqual(new[] { "b1", "b2" }, server.Requests, "Expected the entries of the history to hold the handles of the buildings, which are loaded, instead of ids to load.");
        Assert.AreEqual("Building/b2/Menu", host.Root.Journal.Current!.Path, "Expected the last building to be current again.");
    }

    [TestMethod]
    public async Task The_route_text_minus_must_go_back()
    {
        var (host, layout) = await OpenMenuThenAssets();
        using var _ = host;

        var response = await layout.NavigateRouteAsync(this, "-");

        Assert.IsTrue(response.Success, "Expected the previous page to be shown.");
        Assert.AreEqual(NavigationMode.Back, response.Mode, "Expected the text to be the same as going back.");
        Assert.AreEqual(0, host.Root.Journal.CurrentIndex, "Expected the position to move.");
    }

    [TestMethod]
    public async Task The_route_text_minus_slash_must_clear_the_history_before_the_page_is_shown()
    {
        var (host, layout) = await OpenMenuThenAssets();
        using var _ = host;

        await host.Root.NavigateRouteAsync(this, "-/Login");

        CollectionAssert.AreEqual(new[] { "Login" }, Paths(host), "Expected nothing to lead back to the pages before the login.");
        Assert.IsFalse(host.Root.Journal.CanGoBack, "Expected Back to be unavailable.");
        Assert.AreEqual("Login", host.Root.Requests[^1].NameSegment.Name, "Expected the page to be shown.");
    }

    [TestMethod]
    public async Task The_route_text_slash_must_start_at_the_root_whatever_the_navigator()
    {
        var (host, layout) = await OpenMenuThenAssets();
        using var _ = host;

        await layout.NavigateRouteAsync(this, "/Login");

        Assert.AreEqual("Login", host.Root.Requests[^1].NameSegment.Name, "Expected the root navigator to show the page.");
        Assert.AreEqual("Login", host.Root.Journal.Current!.Path, "Expected the whole route to be the location.");
    }

    [TestMethod]
    public async Task The_route_text_dot_dot_slash_must_start_at_the_parent_region()
    {
        var (host, layout) = await OpenMenuThenAssets();
        using var _ = host;

        await layout.NavigateRouteAsync(this, "../Login");

        Assert.AreEqual("Login", host.Root.Requests[^1].NameSegment.Name, "Expected the region above to show the page.");
    }

    [TestMethod]
    public async Task A_route_text_that_goes_above_the_root_must_throw()
    {
        using var host = TestRoutes.CreateHost(new BuildingServer());

        await Assert.ThrowsExactlyAsync<InvalidOperationException>(
            async () => await host.Root.NavigateRouteAsync(this, "../Login"),
            "Expected a route that starts above the first region to be refused.");
    }
}
