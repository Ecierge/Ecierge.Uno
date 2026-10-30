namespace Ecierge.Uno.Navigation.Tests;

using Ecierge.Uno.Navigation;
using Ecierge.Uno.Navigation.Tests.Doubles;

/// <summary>
/// Dialogs and the history. By default an open dialog is an entry of the history, so going back closes it; with
/// <see cref="NavigationOptions.DialogsInHistory"/> off the history holds the page below the dialog.
/// </summary>
[TestClass, UnitTestCategory, NavigationTestCategory]
public class NavigationDialogTests
{
    private static string[] Paths(NavigationTestHost host) => host.Root.Journal.Entries.Select(e => e.Path).ToArray();

    private static FakeDialogNavigator Dialog(NavigationTestHost host)
    {
        var dialog = host.Root.LeafNavigator as FakeDialogNavigator;
        Assert.IsNotNull(dialog, "Expected the dialog to be the innermost region.");
        return dialog;
    }

    /// <summary>Opens the menu of building b1 and then the dialog AddAsset over it.</summary>
    private static async Task<(NavigationTestHost Host, FakeNavigator Layout)> OpenMenuWithDialog(bool dialogsInHistory)
    {
        var host = TestRoutes.CreateHost(
            new BuildingServer(),
            configureOptions: options => options with { DialogsInHistory = dialogsInHistory });
        await host.Root.NavigateRouteAsync(typeof(NavigationDialogTests), "Building/b1/Menu");
        var layout = (FakeNavigator)host.Root.ChildNavigator!;
        var response = await layout.NavigateRouteAsync(typeof(NavigationDialogTests), "!AddAsset");
        Assert.IsTrue(response.Success, "Expected the dialog to open.");
        return (host, layout);
    }

    [TestMethod]
    public async Task A_dialog_must_be_an_entry_of_the_history_by_default()
    {
        var (host, _) = await OpenMenuWithDialog(true);
        using var _ = host;

        CollectionAssert.AreEqual(new[] { "Building/b1/Menu", "Building/b1/Menu/!AddAsset" }, Paths(host), "Expected the open dialog to be a step of the history.");
        Assert.IsTrue(Dialog(host).IsOpen, "Expected the dialog to be open.");
    }

    [TestMethod]
    public async Task Going_back_must_close_a_dialog_that_is_an_entry_and_keep_the_page_below()
    {
        var (host, layout) = await OpenMenuWithDialog(true);
        using var _ = host;
        var dialog = Dialog(host);
        var pagesBefore = layout.Requests.Count;

        var response = await layout.GoBackAsync(this);

        Assert.IsTrue(response.Success, "Expected going back to succeed.");
        Assert.AreEqual(1, dialog.CloseCount, "Expected the dialog to be closed once.");
        Assert.AreEqual(pagesBefore, layout.Requests.Count, "Expected the page below the dialog, which is right, not to be created again.");
        Assert.AreEqual(0, host.Root.Journal.CurrentIndex, "Expected the position to move to the page.");
        Assert.AreSame(layout, host.Root.LeafNavigator, "Expected the page to be the innermost region again.");
    }

    [TestMethod]
    public async Task Going_forward_must_open_the_dialog_again()
    {
        var (host, layout) = await OpenMenuWithDialog(true);
        using var _ = host;
        await layout.GoBackAsync(this);

        var response = await layout.GoForwardAsync(this);

        Assert.IsTrue(response.Success, "Expected the dialog to open again.");
        Assert.IsTrue(Dialog(host).IsOpen, "Expected a dialog over the page.");
        Assert.AreEqual(1, host.Root.Journal.CurrentIndex, "Expected the position of the dialog entry.");
    }

    [TestMethod]
    public async Task Closing_the_dialog_with_its_own_buttons_must_move_the_history_back()
    {
        var (host, _) = await OpenMenuWithDialog(true);
        using var _ = host;

        Dialog(host).CloseByUser();

        Assert.AreEqual("Building/b1/Menu", host.Root.Journal.Current!.Path, "Expected the page below the dialog to be the current entry.");
        Assert.IsTrue(host.Root.Journal.CanGoForward, "Expected the dialog to be available through Forward.");
    }

    [TestMethod]
    public async Task Closing_the_dialog_after_the_history_moved_on_must_not_move_it_back()
    {
        var (host, _) = await OpenMenuWithDialog(true);
        using var _ = host;
        var dialog = Dialog(host);
        host.Root.Journal.Record(host.Root.ParseRoute("Login"), HistoryMode.Push);

        dialog.CloseByUser();

        Assert.AreEqual("Login", host.Root.Journal.Current!.Path, "Expected a dialog that is not the current entry to leave the position alone.");
    }

    [TestMethod]
    public async Task A_dialog_must_not_be_an_entry_when_the_option_is_off()
    {
        var (host, _) = await OpenMenuWithDialog(false);
        using var _ = host;

        CollectionAssert.AreEqual(new[] { "Building/b1/Menu" }, Paths(host), "Expected the history to hold the page below the dialog.");
        Assert.IsTrue(Dialog(host).IsOpen, "Expected the dialog to be open all the same.");
    }

    [TestMethod]
    public async Task Going_back_with_a_dialog_that_is_not_an_entry_must_close_it_and_show_the_previous_page()
    {
        var (host, layout) = await OpenMenuWithDialog(false);
        using var _ = host;
        await layout.NavigateRouteAsync(this, "/Login");
        await host.Root.NavigateRouteAsync(this, "Building/b1/Assets");
        var layout2 = (FakeNavigator)host.Root.ChildNavigator!;
        await layout2.NavigateRouteAsync(this, "!AddAsset");
        var dialog = Dialog(host);

        var response = await layout2.GoBackAsync(this);

        Assert.IsTrue(response.Success, "Expected going back to succeed.");
        Assert.AreEqual(1, dialog.CloseCount, "Expected the dialog to be closed, as its page is replaced.");
        Assert.AreEqual("Login", host.Root.Journal.Current!.Path, "Expected the page before the page below the dialog.");
    }

    [TestMethod]
    public async Task Closing_a_dialog_that_is_not_an_entry_must_leave_the_history_alone()
    {
        var (host, _) = await OpenMenuWithDialog(false);
        using var _ = host;

        Dialog(host).CloseByUser();

        CollectionAssert.AreEqual(new[] { "Building/b1/Menu" }, Paths(host), "Expected the history not to change.");
        Assert.AreEqual(0, host.Root.Journal.CurrentIndex, "Expected the position not to move.");
    }

    [TestMethod]
    public async Task A_route_text_with_a_dialog_must_open_the_dialog_over_the_page()
    {
        using var host = TestRoutes.CreateHost(new BuildingServer());

        var response = await host.Root.NavigateRouteAsync(this, "Building/b1/Menu/!AddAsset");

        Assert.IsTrue(response.Success, "Expected the page and the dialog to open.");
        Assert.IsTrue(Dialog(host).IsOpen, "Expected the dialog over the page.");
        Assert.AreEqual("Building/b1/Menu/!AddAsset", host.Root.Journal.Current!.Path, "Expected the dialog to be in the location.");
    }

    [TestMethod]
    public async Task A_dialog_that_does_not_exist_must_not_be_found()
    {
        using var host = TestRoutes.CreateHost(new BuildingServer());
        await host.Root.NavigateRouteAsync(this, "Building/b1/Menu");

        Assert.IsFalse(host.Root.TryParseRoute("Building/b1/Menu/!Unknown", null, out _), "Expected a dialog of no region to be refused.");
    }
}
