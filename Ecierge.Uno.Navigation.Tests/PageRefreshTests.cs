namespace Ecierge.Uno.Navigation.Tests;

using System.Windows.Input;

using Ecierge.Uno.Navigation;
using Ecierge.Uno.Navigation.Tests.Doubles;

/// <summary>
/// The last breadcrumb loads the data of the page that is shown again. The page takes part through
/// <see cref="IRefreshable"/>; the navigation never creates the page again.
/// </summary>
[TestClass, UnitTestCategory, NavigationTestCategory]
public class PageRefreshTests
{
    private sealed class CountingCommand(bool canExecute) : ICommand
    {
        public int Executions { get; private set; }

        public event EventHandler? CanExecuteChanged { add { } remove { } }

        public bool CanExecute(object? parameter) => canExecute;

        public void Execute(object? parameter) => Executions++;
    }

    /// <summary>Stands for a page that loads its data again when it is asked to.</summary>
    private sealed class RefreshablePage(bool canExecute = true) : IRefreshable
    {
        public CountingCommand Command { get; } = new(canExecute);

        public ICommand RefreshCommand => Command;
    }

    /// <summary>Creates a host whose navigators show the page as the view of the named segment.</summary>
    private static NavigationTestHost HostShowing(string segmentName, object page)
    {
        var host = TestRoutes.CreateHost(new BuildingServer());
        host.Plan.Intercept = (_, request) =>
            request is NameSegmentNavigationRequest named && named.Segment.Name == segmentName
                ? new NavigationResult(request, page)
                : null;
        return host;
    }

    [TestMethod]
    public async Task The_page_that_is_shown_must_load_its_data_again()
    {
        var page = new RefreshablePage();
        using var host = HostShowing("Details", page);
        await host.Root.NavigateRouteAsync(this, "Building/b1/Details/Floors");

        var refreshed = host.Root.TryRefreshPage();

        Assert.IsTrue(refreshed, "Expected the page around the tab that is shown to answer.");
        Assert.AreEqual(1, page.Command.Executions, "Expected the command of the page to run once.");
    }

    [TestMethod]
    public async Task A_page_without_the_contract_must_be_left_as_it_is()
    {
        using var host = TestRoutes.CreateHost(new BuildingServer());
        await host.Root.NavigateRouteAsync(this, "Building/b1/Details/Floors");

        Assert.IsFalse(host.Root.TryRefreshPage(), "Expected nothing to load again when no page implements the contract.");
    }

    [TestMethod]
    public async Task A_page_whose_command_cannot_run_must_not_load_again()
    {
        var page = new RefreshablePage(canExecute: false);
        using var host = HostShowing("Assets", page);
        await host.Root.NavigateRouteAsync(this, "Building/b1/Assets");

        Assert.IsFalse(host.Root.TryRefreshPage(), "Expected the page to say that it cannot load again now.");
        Assert.AreEqual(0, page.Command.Executions, "Expected the command not to run.");
    }

    [TestMethod]
    public async Task A_dialog_must_not_load_again_in_place_of_the_page()
    {
        var page = new RefreshablePage();
        var dialog = new RefreshablePage();
        using var host = HostShowing("Assets", page);
        host.Plan.DialogContent = _ => dialog;
        await host.Root.NavigateRouteAsync(this, "Building/b1/Assets");
        await host.Root.LeafNavigator.NavigateRouteAsync(this, "!AddAsset");

        var refreshed = host.Root.TryRefreshPage();

        Assert.IsTrue(refreshed, "Expected the page under the dialog to answer.");
        Assert.AreEqual(1, page.Command.Executions, "Expected the page to load again.");
        Assert.AreEqual(0, dialog.Command.Executions, "Expected the dialog to be left alone.");
    }

    [TestMethod]
    public async Task A_skipped_navigation_must_keep_the_page_that_is_shown()
    {
        var page = new RefreshablePage();
        using var host = TestRoutes.CreateHost(new BuildingServer());
        host.Plan.Intercept = (_, request) => request.NameSegment.Name switch
        {
            "Assets" => new NavigationResult(request, page),
            "Menu" => new NavigationResult(request, isSkipped: true),
            _ => null
        };
        await host.Root.NavigateRouteAsync(this, "Building/b1/Assets");
        await host.Root.ChildNavigator!.NavigateRouteAsync(this, "Menu");

        Assert.IsTrue(host.Root.TryRefreshPage(), "Expected the page that stayed on the screen to answer.");
        Assert.AreEqual(1, page.Command.Executions, "Expected the page that stayed on the screen to load again.");
    }
}
