namespace Ecierge.Uno.Navigation.Tests;

using Ecierge.Uno.Navigation;
using Ecierge.Uno.Navigation.Tests.Doubles;

using Microsoft.Extensions.Logging.Abstractions;

/// <summary>
/// The browser history and the real navigators together: the buttons of the browser show the pages through the
/// navigators, and what the navigators show is written to the browser.
/// </summary>
[TestClass, UnitTestCategory, NavigationTestCategory]
public class BrowserHistoryNavigationTests
{
    private static BrowserHistorySync Start(NavigationTestHost host, FakeBrowserHistory browser)
     => new(host.Root.Journal, browser, new NavigationOptions(), new RootHistoryNavigation(host.Root), NullLogger.Instance);

    private async Task<(NavigationTestHost Host, FakeNavigator Layout, FakeBrowserHistory Browser, BrowserHistorySync Sync)> OpenMenuThenAssets(FakeBrowserHistory? browser = null)
    {
        var host = TestRoutes.CreateHost(new BuildingServer());
        browser ??= new FakeBrowserHistory();
        var sync = Start(host, browser);
        await host.Root.NavigateRouteAsync(this, "Building/b1/Menu");
        var layout = (FakeNavigator)host.Root.ChildNavigator!;
        await layout.NavigateRouteAsync(this, "Assets");
        return (host, layout, browser, sync);
    }

    [TestMethod]
    public async Task Every_page_the_navigators_show_must_be_an_entry_of_the_browser_with_its_address()
    {
        var (host, _, browser, sync) = await OpenMenuThenAssets();
        using var _h = host;
        using var _s = sync;

        CollectionAssert.AreEqual(new[] { "/Building/b1/Menu", "/Building/b1/Assets" }, browser.Paths, "Expected the address bar to follow the pages.");
        CollectionAssert.AreEqual(new[] { "replace /Building/b1/Menu", "push /Building/b1/Assets" }, browser.Log, "Expected the first page to replace the entry of the load and the next to be added.");
    }

    [TestMethod]
    public async Task The_back_button_of_the_browser_must_show_the_previous_page_and_keep_the_building_page()
    {
        var (host, layout, browser, sync) = await OpenMenuThenAssets();
        using var _h = host;
        using var _s = sync;

        browser.UserGoes(-1);
        await sync.HandlePopAsync(browser.TakeNextPop());

        CollectionAssert.AreEqual(new[] { "Menu", "Assets", "Menu" }, layout.Requests.Select(r => r.NameSegment.Name).ToArray(), "Expected the previous page to be shown by the navigator of the building pages.");
        Assert.ContainsSingle(host.Root.Requests, "Expected the building page not to be created again.");
        Assert.AreEqual(0, host.Root.Journal.CurrentIndex, "Expected the journal to follow the browser.");
        Assert.HasCount(2, host.Root.Journal.Entries, "Expected going back not to add an entry.");
    }

    [TestMethod]
    public async Task The_forward_button_of_the_browser_must_show_the_page_that_back_left()
    {
        var (host, layout, browser, sync) = await OpenMenuThenAssets();
        using var _h = host;
        using var _s = sync;
        browser.UserGoes(-1);
        await sync.HandlePopAsync(browser.TakeNextPop());

        browser.UserGoes(1);
        await sync.HandlePopAsync(browser.TakeNextPop());

        Assert.AreEqual("Assets", layout.Requests[^1].NameSegment.Name, "Expected the page that was left to be shown again.");
        Assert.AreEqual(1, host.Root.Journal.CurrentIndex, "Expected the journal to follow the browser.");
    }

    [TestMethod]
    public async Task The_back_button_of_the_application_must_move_the_browser_and_not_show_the_page_twice()
    {
        var (host, layout, browser, sync) = await OpenMenuThenAssets();
        using var _h = host;
        using var _s = sync;

        await layout.GoBackAsync(this);
        await sync.HandlePopAsync(browser.TakeNextPop());

        Assert.AreEqual(0, browser.Index, "Expected the browser to follow the button of the application.");
        CollectionAssert.AreEqual(new[] { "Menu", "Assets", "Menu" }, layout.Requests.Select(r => r.NameSegment.Name).ToArray(), "Expected the page to be shown once, by the button, not again by the echo of the browser.");
    }

    [TestMethod]
    public async Task An_entry_from_before_the_application_must_be_opened_from_its_address_and_take_the_place_of_the_current_entry()
    {
        var browser = new FakeBrowserHistory(("/Building/b1/Details/Floors", null));
        var (host, layout, _, sync) = await OpenMenuThenAssets(browser);
        using var _h = host;
        using var _s = sync;

        browser.UserGoes(-2);
        await sync.HandlePopAsync(browser.TakeNextPop());

        Assert.AreEqual("Building/b1/Details/Floors", host.Root.Journal.Current!.Path, "Expected the page of the address to be opened.");
        Assert.AreEqual("/Building/b1/Details/Floors", browser.Paths[browser.Index], "Expected the entry to keep its address, now with the token of the application.");
        Assert.AreEqual("Details", layout.Requests[^1].NameSegment.Name, "Expected the page of the address to be shown below the building.");
    }

    [TestMethod]
    public async Task A_dialog_must_be_in_the_address_and_the_back_button_of_the_browser_must_close_it()
    {
        var (host, layout, browser, sync) = await OpenMenuThenAssets();
        using var _h = host;
        using var _s = sync;
        await layout.NavigateRouteAsync(this, "!AddAsset");
        var dialog = (FakeDialogNavigator)host.Root.LeafNavigator;

        Assert.AreEqual("/Building/b1/Assets/!AddAsset", browser.Paths[browser.Index], "Expected the dialog to be part of the address.");

        browser.UserGoes(-1);
        await sync.HandlePopAsync(browser.TakeNextPop());

        Assert.AreEqual(1, dialog.CloseCount, "Expected the back button of the browser to close the dialog.");
        Assert.AreEqual("Building/b1/Assets", host.Root.Journal.Current!.Path, "Expected the page below the dialog to be the current entry.");
    }
}
