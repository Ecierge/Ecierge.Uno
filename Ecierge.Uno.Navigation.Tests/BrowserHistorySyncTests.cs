namespace Ecierge.Uno.Navigation.Tests;

using Ecierge.Uno.Navigation;
using Ecierge.Uno.Navigation.Tests.Doubles;

using Microsoft.Extensions.Logging.Abstractions;

/// <summary>
/// The browser history and the journal, kept in step: what the application records the browser records, and a move of
/// the user in the browser shows the page of the entry.
/// </summary>
[TestClass, UnitTestCategory, NavigationTestCategory]
public class BrowserHistorySyncTests
{
    private readonly NavigationJournal journal = new();
    private readonly FakeHistoryNavigation navigation = new();

    private static Routing.Route RouteOf(string name) => Routing.Route.Empty.Add(new NameSegment(name));

    private BrowserHistorySync Start(FakeBrowserHistory browser, NavigationOptions? options = null)
     => new(journal, browser, options ?? new NavigationOptions(), navigation, NullLogger.Instance);

    private void Visit(params string[] names)
    {
        foreach (var name in names) journal.Record(RouteOf(name), HistoryMode.Push);
    }

    [TestMethod]
    public void The_first_page_must_replace_the_entry_the_browser_started_with()
    {
        var browser = new FakeBrowserHistory();
        using var sync = Start(browser);

        Visit("A");

        CollectionAssert.AreEqual(new[] { "replace /A" }, browser.Log, "Expected the page the application starts with to take the place of the entry it was loaded at.");
        CollectionAssert.AreEqual(new[] { "/A" }, browser.Paths, "Expected the browser to hold one entry.");
    }

    [TestMethod]
    public void The_next_pages_must_be_added_to_the_browser_history()
    {
        var browser = new FakeBrowserHistory();
        using var sync = Start(browser);

        Visit("A", "B", "C");

        CollectionAssert.AreEqual(new[] { "/A", "/B", "/C" }, browser.Paths, "Expected an entry for every page, so that Back and Forward work.");
        Assert.AreEqual(2, browser.Index, "Expected the browser to be at the last page.");
    }

    [TestMethod]
    public void A_page_that_replaces_another_must_replace_its_entry()
    {
        var browser = new FakeBrowserHistory();
        using var sync = Start(browser);
        Visit("Login");

        journal.Record(RouteOf("Home"), HistoryMode.Replace);

        CollectionAssert.AreEqual(new[] { "/Home" }, browser.Paths, "Expected Back not to lead to the login page.");
    }

    [TestMethod]
    public void The_page_after_a_cleared_journal_must_take_the_place_of_the_current_entry()
    {
        var browser = new FakeBrowserHistory();
        using var sync = Start(browser);
        Visit("A", "Login");

        journal.Clear();
        journal.Record(RouteOf("Home"), HistoryMode.Replace);

        CollectionAssert.AreEqual(new[] { "/A", "/Home" }, browser.Paths, "Expected the browser, which cannot forget entries, to replace the current one.");
    }

    [TestMethod]
    public void The_address_must_be_written_under_the_base_path_with_escaped_values()
    {
        var browser = new FakeBrowserHistory();
        using var sync = Start(browser, new NavigationOptions { BasePath = "/console/" });
        var data = new DataSegment("BuildingInfo", null, true);
        _ = new NameSegment("Building", null, data);

        journal.Record(Routing.Route.Empty.Add(data, "a b"), HistoryMode.Push);

        CollectionAssert.AreEqual(new[] { "/console/Building/a%20b" }, browser.Paths, "Expected the address to be under the base path and the value to be escaped.");
    }

    [TestMethod]
    public void A_move_of_the_journal_must_move_the_browser_and_ignore_the_echo()
    {
        var browser = new FakeBrowserHistory();
        using var sync = Start(browser);
        Visit("A", "B");

        journal.Move(-1);
        browser.DeliverPops();

        Assert.AreEqual(0, browser.Index, "Expected the browser to follow the in-app Back button.");
        Assert.IsEmpty(navigation.Shown, "Expected the pop that the move caused not to show a page again.");
        Assert.AreEqual(0, journal.CurrentIndex, "Expected the journal to stay where the application moved it.");
    }

    [TestMethod]
    public void A_user_going_back_in_the_browser_must_show_the_page_and_move_the_journal_without_writing_back()
    {
        var browser = new FakeBrowserHistory();
        using var sync = Start(browser);
        Visit("A", "B", "C");
        browser.Log.Clear();

        browser.UserGoes(-1);
        browser.DeliverPops();

        CollectionAssert.AreEqual(new[] { "Back B" }, navigation.Shown, "Expected the page of the entry to be shown.");
        Assert.AreEqual(1, journal.CurrentIndex, "Expected the journal to follow the browser.");
        Assert.IsEmpty(browser.Log, "Expected the move of the journal not to be written back to the browser.");
    }

    [TestMethod]
    public void A_user_going_forward_in_the_browser_must_show_the_page_of_the_entry()
    {
        var browser = new FakeBrowserHistory();
        using var sync = Start(browser);
        Visit("A", "B", "C");
        browser.UserGoes(-2);
        browser.DeliverPops();
        navigation.Shown.Clear();

        browser.UserGoes(1);
        browser.DeliverPops();

        CollectionAssert.AreEqual(new[] { "Forward B" }, navigation.Shown, "Expected the forward page to be shown.");
        Assert.AreEqual(1, journal.CurrentIndex, "Expected the journal to follow the browser.");
    }

    [TestMethod]
    public void A_user_going_several_entries_back_must_show_the_page_of_the_entry_reached()
    {
        var browser = new FakeBrowserHistory();
        using var sync = Start(browser);
        Visit("A", "B", "C");

        browser.UserGoes(-2);
        browser.DeliverPops();

        CollectionAssert.AreEqual(new[] { "Back A" }, navigation.Shown, "Expected only the page that was reached to be shown.");
        Assert.AreEqual(0, journal.CurrentIndex, "Expected the journal to follow the browser over both entries.");
    }

    [TestMethod]
    public void A_page_that_cannot_be_shown_must_return_the_browser_to_the_current_entry()
    {
        var browser = new FakeBrowserHistory();
        using var sync = Start(browser);
        Visit("A", "B", "C");
        navigation.CanShowEntry = _ => false;

        browser.UserGoes(-1);
        browser.DeliverPops();
        browser.DeliverPops();

        Assert.AreEqual(2, journal.CurrentIndex, "Expected the journal to stay at the page that is shown.");
        Assert.AreEqual(2, browser.Index, "Expected the browser to return to the page that is shown.");
        Assert.ContainsSingle(navigation.Shown, "Expected the return not to be taken for a new move.");
    }

    [TestMethod]
    public void An_entry_the_application_did_not_make_must_be_opened_from_its_address()
    {
        var browser = new FakeBrowserHistory(("/Building/b1/Assets", null));
        using var sync = Start(browser);
        Visit("A");

        browser.UserGoes(-1);
        browser.DeliverPops();

        CollectionAssert.AreEqual(new[] { "path Building/b1/Assets" }, navigation.Shown, "Expected the page of the address to be opened, as the journal knows nothing of the entry.");
    }

    [TestMethod]
    public void An_entry_of_an_earlier_run_must_be_opened_from_its_address()
    {
        var browser = new FakeBrowserHistory(("/Building/b1/Menu", "an-earlier-run:3"));
        using var sync = Start(browser);
        Visit("A");

        browser.UserGoes(-1);
        browser.DeliverPops();

        CollectionAssert.AreEqual(new[] { "path Building/b1/Menu" }, navigation.Shown, "Expected the entries of an earlier run to be told from the entries of this one.");
    }

    [TestMethod]
    public void An_entry_that_left_the_journal_must_be_opened_from_its_address()
    {
        var browser = new FakeBrowserHistory();
        var tiny = new NavigationJournal(2);
        using var sync = new BrowserHistorySync(tiny, browser, new NavigationOptions(), navigation, NullLogger.Instance);
        foreach (var name in new[] { "A", "B", "C" }) tiny.Record(RouteOf(name), HistoryMode.Push);

        browser.UserGoes(-2);
        browser.DeliverPops();

        CollectionAssert.AreEqual(new[] { "path A" }, navigation.Shown, "Expected an entry that the journal dropped to be opened from the address, not to fail.");
    }

    [TestMethod]
    public void An_address_that_is_not_a_page_of_the_application_must_be_ignored()
    {
        var browser = new FakeBrowserHistory(("/", null));
        using var sync = Start(browser);
        Visit("A");

        browser.UserGoes(-1);
        browser.DeliverPops();

        Assert.IsEmpty(navigation.Shown, "Expected the root of the site, which names no page, not to be opened.");
        Assert.AreEqual(0, journal.CurrentIndex, "Expected the journal not to move.");
        CollectionAssert.AreEqual(new[] { "/A" }, browser.Paths[^1..], "Expected the address bar to show the page that is shown again.");
    }

    [TestMethod]
    public void A_page_of_an_address_that_cannot_be_opened_must_restore_the_address_of_the_current_page()
    {
        var browser = new FakeBrowserHistory(("/Building/b1/Assets", null));
        using var sync = Start(browser);
        Visit("A");
        navigation.CanShowPath = _ => false;

        browser.UserGoes(-1);
        browser.DeliverPops();

        CollectionAssert.AreEqual(new[] { "path Building/b1/Assets" }, navigation.Shown, "Expected the page of the address to be tried.");
        Assert.AreEqual("/A", browser.Paths[browser.Index], "Expected the address bar not to show a page that is not shown, for example one the rules refuse.");
        Assert.AreEqual(0, journal.CurrentIndex, "Expected the journal not to move.");
    }

    [TestMethod]
    public async Task Moves_that_arrive_while_a_page_is_shown_must_wait_for_it()
    {
        var browser = new FakeBrowserHistory();
        using var sync = Start(browser);
        Visit("A", "B", "C");
        var release = new TaskCompletionSource();
        navigation.Delay = () => release.Task;

        var first = sync.HandlePopAsync(new BrowserHistoryPopEventArgs(new Uri("https://example.com/B"), journal.SessionId + ":" + journal.Entries[1].Id));
        var second = sync.HandlePopAsync(new BrowserHistoryPopEventArgs(new Uri("https://example.com/A"), journal.SessionId + ":" + journal.Entries[0].Id));

        Assert.ContainsSingle(navigation.Shown, "Expected the second move to wait until the first page is shown.");

        release.SetResult();
        await Task.WhenAll(first, second);

        CollectionAssert.AreEqual(new[] { "Back B", "Back A" }, navigation.Shown, "Expected the moves to be followed in the order they came.");
        Assert.AreEqual(0, journal.CurrentIndex, "Expected the journal to end at the last entry the user reached.");
    }

    [TestMethod]
    public void A_stopped_synchronization_must_not_touch_the_browser()
    {
        var browser = new FakeBrowserHistory();
        var sync = Start(browser);
        sync.Dispose();

        Visit("A");

        Assert.IsEmpty(browser.Log, "Expected a window that closed to leave the browser alone.");
    }

    [TestMethod]
    [DataRow("A/B", "/", "/A/B")]
    [DataRow("A/B", "/console/", "/console/A/B")]
    [DataRow("A/B", "/console", "/console/A/B")]
    [DataRow("Building/a b/!AddAsset", "/", "/Building/a%20b/!AddAsset")]
    public void BrowserHistoryUrl_must_write_the_route_under_the_base_path(string route, string basePath, string expected)
    {
        Assert.AreEqual(expected, BrowserHistoryUrl.Create(route, basePath), "Expected the path of the address.");
    }
}
