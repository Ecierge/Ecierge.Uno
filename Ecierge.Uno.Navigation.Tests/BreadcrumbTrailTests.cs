namespace Ecierge.Uno.Navigation.Tests;

using System.Windows.Input;

using Ecierge.Uno.Navigation;
using Ecierge.Uno.Navigation.Controls;
using Ecierge.Uno.Navigation.Routing;
using Ecierge.Uno.Navigation.Tests.Doubles;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Localization;

/// <summary>
/// The breadcrumbs of the route that is shown: a level for every named segment, the name of the entity for a segment
/// with its value, and what a click on a breadcrumb does.
/// </summary>
[TestClass, UnitTestCategory, NavigationTestCategory]
public class BreadcrumbTrailTests
{
    private static readonly NameSegment MyProperty = new("MyProperty", isDefault: true) { Title = "My property" };
    private static readonly NameSegment Info = new("Info", isDefault: true) { Title = "Info" };
    private static readonly NameSegment Floors = new("Floors") { Title = "Floors" };
    private static readonly NameSegment Details = new("Details", nested: [Info, Floors]) { Title = "Details" };
    private static readonly NameSegment Menu = new("Menu");
    private static readonly NameSegment WebPage = new("Page1");
    private static readonly NameSegment Web = new("Web", nested: [WebPage]) { HidesBreadcrumbs = true };
    private static readonly DialogSegment AddAsset = new("AddAsset");
    private static readonly DataSegment BuildingData = new("BuildingInfo", null, true, MyProperty, Menu, Details, Web, AddAsset);
    private static readonly NameSegment Building = new("Building", null, BuildingData) { Title = "Building" };

    private static Routing.Route BuildingRoute => Routing.Route.Empty.Add(BuildingData, "b1");

    private static BreadcrumbTrail TrailOf(Routing.Route route, string? buildingName = null)
     => BreadcrumbTrail.Build(route, segment => BreadcrumbTrail.TitleOf(segment, null), _ => buildingName);

    private static string[] TitlesOf(BreadcrumbTrail trail) => trail.Items.Select(item => item.Title).ToArray();

    /// <summary>A localizer with the given resources; any other key is not found.</summary>
    private sealed class FakeLocalizer(Dictionary<string, string> resources) : IStringLocalizer
    {
        public LocalizedString this[string name]
         => resources.TryGetValue(name, out var value)
                ? new LocalizedString(name, value, resourceNotFound: false)
                : new LocalizedString(name, name, resourceNotFound: true);

        public LocalizedString this[string name, params object[] arguments] => this[name];

        public IEnumerable<LocalizedString> GetAllStrings(bool includeParentCultures)
         => resources.Select(pair => new LocalizedString(pair.Key, pair.Value));
    }

    [TestMethod]
    public void Every_level_of_the_route_must_be_a_breadcrumb_with_its_title()
    {
        var trail = TrailOf(BuildingRoute.Add(Details).Add(Floors));

        CollectionAssert.AreEqual(new[] { "Building", "Details", "Floors" }, TitlesOf(trail), "Expected a breadcrumb for every level, named by the titles of the segments.");
        CollectionAssert.AreEqual(new[] { false, false, true }, trail.Items.Select(item => item.IsCurrent).ToArray(), "Expected only the last breadcrumb to stand for the page that is shown.");
        Assert.IsTrue(trail.IsShown, "Expected the bar to show the breadcrumbs.");
    }

    [TestMethod]
    public void A_segment_and_its_value_must_be_one_breadcrumb_named_after_the_entity()
    {
        var trail = TrailOf(BuildingRoute.Add(Details), buildingName: "Sunset Tower");

        CollectionAssert.AreEqual(new[] { "Sunset Tower", "Details" }, TitlesOf(trail), "Expected the name of the building in place of the segment and its id.");
    }

    [TestMethod]
    public void A_segment_without_a_title_must_show_its_name()
    {
        var trail = TrailOf(BuildingRoute.Add(Menu));

        CollectionAssert.AreEqual(new[] { "Building", "Menu" }, TitlesOf(trail), "Expected the name of the segment that has no title.");
    }

    [TestMethod]
    public void The_route_of_a_breadcrumb_must_end_at_its_level()
    {
        var trail = TrailOf(BuildingRoute.Add(Details).Add(Floors));

        CollectionAssert.AreEqual(
            new[] { "Building/b1", "Building/b1/Details", "Building/b1/Details/Floors" },
            trail.Items.Select(item => item.Route.ToPath()).ToArray(),
            "Expected each breadcrumb to lead to its own level, the entity with its value.");
    }

    [TestMethod]
    public void A_dialog_must_not_be_a_breadcrumb()
    {
        var trail = TrailOf(BuildingRoute.Add(Menu).Add(AddAsset));

        CollectionAssert.AreEqual(new[] { "Building", "Menu" }, TitlesOf(trail), "Expected the breadcrumbs of the page under the dialog.");
        Assert.IsTrue(trail.Items[^1].IsCurrent, "Expected the page under the dialog to be the current breadcrumb.");
    }

    [TestMethod]
    public void A_segment_that_hides_the_breadcrumbs_must_hide_the_bar()
    {
        var trail = TrailOf(BuildingRoute.Add(Web).Add(WebPage));

        Assert.IsTrue(trail.IsHidden, "Expected the route through the web page to hide the breadcrumbs.");
        Assert.IsFalse(trail.IsShown, "Expected the bar to show nothing.");
        Assert.IsTrue(TrailOf(BuildingRoute.Add(Menu)).IsShown, "Expected the bar to show the breadcrumbs again after the web page.");
    }

    [TestMethod]
    public void An_empty_route_must_show_no_bar()
    {
        Assert.IsFalse(TrailOf(Routing.Route.Empty).IsShown, "Expected no breadcrumbs before the first page.");
    }

    [TestMethod]
    public void A_title_that_is_the_key_of_a_resource_must_show_the_localized_text()
    {
        var localizer = new FakeLocalizer(new() { ["Building"] = "Bâtiment" });

        Assert.AreEqual("Bâtiment", BreadcrumbTrail.TitleOf(Building, localizer), "Expected the text of the resource.");
    }

    [TestMethod]
    public void A_title_without_a_resource_must_be_shown_as_written()
    {
        var localizer = new FakeLocalizer(new() { ["Details"] = "" });

        Assert.AreEqual("My property", BreadcrumbTrail.TitleOf(MyProperty, localizer), "Expected the title when no resource has it as its key.");
        Assert.AreEqual("Details", BreadcrumbTrail.TitleOf(Details, localizer), "Expected the title when its resource is empty.");
        Assert.AreEqual("Building", BreadcrumbTrail.TitleOf(Building, null), "Expected the title when the application has no localizer.");
        Assert.AreEqual("Menu", BreadcrumbTrail.TitleOf(Menu, localizer), "Expected the name of a segment without a title.");
    }

    [TestMethod]
    public void The_route_of_a_level_must_get_the_default_pages_of_the_level()
    {
        Assert.AreEqual("Building/b1/MyProperty", BreadcrumbTrail.WithDefaultPages(BuildingRoute).ToPath(), "Expected the default page of the building.");
        Assert.AreEqual("Building/b1/Details/Info", BreadcrumbTrail.WithDefaultPages(BuildingRoute.Add(Details)).ToPath(), "Expected the default tab of the details.");
        Assert.AreEqual("Building/b1/Web", BreadcrumbTrail.WithDefaultPages(BuildingRoute.Add(Web)).ToPath(), "Expected a level without a default page to stay as it is.");
    }

    [TestMethod]
    public async Task The_breadcrumb_of_an_entity_must_show_its_name_once_it_has_loaded_and_keep_it_while_it_loads_again()
    {
        var server = new BuildingServer();
        var first = new TaskCompletionSource<Building>();
        server.Answer = _ => first.Task;
        var map = new BuildingMap(server);
        using var services = new ServiceCollection().AddSingleton(map).BuildServiceProvider();
        var segment = new DataSegment("BuildingInfo", typeof(BuildingMap));
        var handle = ((INavigationDataMap)map).CreateEntity("b1");
        var data = NavigationData.Empty.Add("BuildingInfo", handle);
        var step = new DataSegmentInstance(segment, "b1", null);
        var names = new BreadcrumbNames(services);

        _ = handle.LoadTask;
        Assert.IsNull(names.DisplayNameOf(step, data), "Expected no name while the building loads the first time.");

        first.SetResult(new Building("b1", "Sunset Tower"));
        await handle.LoadTask;
        Assert.AreEqual("Sunset Tower", names.DisplayNameOf(step, data), "Expected the name of the loaded building.");

        var second = new TaskCompletionSource<Building>();
        server.Answer = _ => second.Task;
        var reload = handle.ReloadAsync();
        Assert.AreEqual("Sunset Tower", names.DisplayNameOf(step, data), "Expected the last name while the building loads again.");

        second.SetResult(new Building("b1", "Sunset Plaza"));
        await reload;
        Assert.AreEqual("Sunset Plaza", names.DisplayNameOf(step, data), "Expected the new name after the building loaded again.");
    }

    [TestMethod]
    public async Task The_breadcrumb_of_an_entity_that_failed_to_load_must_keep_the_title()
    {
        var server = new BuildingServer { IsDown = true };
        var map = new BuildingMap(server);
        using var services = new ServiceCollection().AddSingleton(map).BuildServiceProvider();
        var handle = ((INavigationDataMap)map).CreateEntity("b1");
        var step = new DataSegmentInstance(new DataSegment("BuildingInfo", typeof(BuildingMap)), "b1", null);

        await Assert.ThrowsExactlyAsync<HttpRequestException>(() => handle.LoadTask, "Expected the building to fail to load.");

        Assert.IsNull(new BreadcrumbNames(services).DisplayNameOf(step, NavigationData.Empty.Add("BuildingInfo", handle)), "Expected no name, so that the title of the segment stays.");
    }

    private sealed class CountingCommand : ICommand
    {
        public int Executions { get; private set; }

        public event EventHandler? CanExecuteChanged { add { } remove { } }

        public bool CanExecute(object? parameter) => true;

        public void Execute(object? parameter) => Executions++;
    }

    private sealed class RefreshablePage : IRefreshable
    {
        public CountingCommand Command { get; } = new();

        public ICommand RefreshCommand => Command;
    }

    private static void RegisterTitledRoutes(IViewRegistryBuilder views, INavigationDataRegistryBuilder data, IRouteRegistryBuilder routes)
    {
        views.Register(new ViewMap<PageView>(), new ViewMap<BuildingPageView>());
        data.Register<BuildingMap>();
        routes.Register((v, d) => [
            new NameSegment(
                "Building",
                v[typeof(BuildingPageView)],
                new DataSegment(
                    "BuildingInfo",
                    d[typeof(Building)],
                    true,
                    new NameSegment("MyProperty", v[typeof(PageView)], isDefault: true) { Title = "My property" },
                    new NameSegment("Details", v[typeof(PageView)], nested: [new NameSegment("Info", isDefault: true), new NameSegment("Floors")]) { Title = "Details" },
                    new NameSegment("Web", v[typeof(PageView)], nested: [new NameSegment("Page1")]) { HidesBreadcrumbs = true }))
            {
                Title = "Building"
            }
        ]);
    }

    /// <summary>Creates a host whose details page loads its data again on request.</summary>
    private static NavigationTestHost CreateHost(out RefreshablePage details)
    {
        var page = new RefreshablePage();
        var host = NavigationTestHost.Create(RegisterTitledRoutes, services => services.AddSingleton(new BuildingServer()));
        host.Plan.Intercept = (_, request) =>
            request is NameSegmentNavigationRequest { Segment.Name: "Details" } ? new NavigationResult(request, page) : null;
        details = page;
        return host;
    }

    private static BreadcrumbTrail TrailShownBy(NavigationTestHost host)
    {
        var route = host.Root.ActualRoute;
        var names = new BreadcrumbNames(host.Services);
        return BreadcrumbTrail.Build(route, segment => BreadcrumbTrail.TitleOf(segment, null), step => names.DisplayNameOf(step, route.Data));
    }

    [TestMethod]
    public async Task The_breadcrumbs_of_the_route_shown_must_name_the_loaded_building()
    {
        using var host = CreateHost(out _);
        await host.Root.NavigateRouteAsync(this, "Building/b1/Details/Floors");

        CollectionAssert.AreEqual(new[] { "Building b1", "Details", "Floors" }, TitlesOf(TrailShownBy(host)), "Expected the name of the building that the route loaded.");
    }

    [TestMethod]
    public async Task A_breadcrumb_above_the_page_must_open_the_default_page_of_its_level()
    {
        using var host = CreateHost(out _);
        await host.Root.NavigateRouteAsync(this, "Building/b1/Details/Floors");
        var entries = host.Root.Journal.Entries.Count;

        var outcome = await BreadcrumbTrail.OpenAsync(host.Root, TrailShownBy(host).Items[0], this);

        Assert.AreEqual(BreadcrumbOutcome.Navigated, outcome, "Expected the level of the building to be opened.");
        Assert.AreEqual("Building/b1/MyProperty", host.Root.ActualRoute.ToPath(), "Expected the default page of the building.");
        Assert.AreEqual(entries + 1, host.Root.Journal.Entries.Count, "Expected one entry in the history.");

        await host.Root.GoBackAsync(this);
        Assert.AreEqual("Building/b1/Details/Floors", host.Root.ActualRoute.ToPath(), "Expected Back to return to the page of the click.");
    }

    [TestMethod]
    public async Task The_last_breadcrumb_must_load_the_page_again()
    {
        using var host = CreateHost(out var details);
        await host.Root.NavigateRouteAsync(this, "Building/b1/Details/Floors");
        var entries = host.Root.Journal.Entries.Count;

        var outcome = await BreadcrumbTrail.OpenAsync(host.Root, TrailShownBy(host).Items[^1], this);

        Assert.AreEqual(BreadcrumbOutcome.Refreshed, outcome, "Expected the page to load its data again.");
        Assert.AreEqual(1, details.Command.Executions, "Expected the command of the page to run.");
        Assert.AreEqual(entries, host.Root.Journal.Entries.Count, "Expected no entry in the history.");
    }

    [TestMethod]
    public async Task A_breadcrumb_that_leads_to_the_page_shown_must_load_it_again_instead_of_navigating()
    {
        using var host = CreateHost(out var details);
        await host.Root.NavigateRouteAsync(this, "Building/b1/Details/Info");
        var entries = host.Root.Journal.Entries.Count;
        var requests = host.Plan.Navigated.Count;

        var outcome = await BreadcrumbTrail.OpenAsync(host.Root, TrailShownBy(host).Items[1], this);

        Assert.AreEqual(BreadcrumbOutcome.Refreshed, outcome, "Expected the details, whose default tab is shown, to load again.");
        Assert.AreEqual(1, details.Command.Executions, "Expected the command of the page to run.");
        Assert.AreEqual(requests, host.Plan.Navigated.Count, "Expected no navigation.");
        Assert.AreEqual(entries, host.Root.Journal.Entries.Count, "Expected no entry in the history.");
    }

    [TestMethod]
    public async Task The_last_breadcrumb_of_a_page_without_the_contract_must_change_nothing()
    {
        using var host = CreateHost(out _);
        await host.Root.NavigateRouteAsync(this, "Building/b1/MyProperty");

        var outcome = await BreadcrumbTrail.OpenAsync(host.Root, TrailShownBy(host).Items[^1], this);

        Assert.AreEqual(BreadcrumbOutcome.NotRefreshable, outcome, "Expected the page without the contract to stay as it is.");
        Assert.AreEqual("Building/b1/MyProperty", host.Root.ActualRoute.ToPath(), "Expected the same page.");
    }

    [TestMethod]
    public async Task A_level_without_a_page_of_its_own_must_open_nothing()
    {
        using var host = CreateHost(out _);
        await host.Root.NavigateRouteAsync(this, "Building/b1/Web/Page1");
        var requests = host.Plan.Navigated.Count;

        var outcome = await BreadcrumbTrail.OpenAsync(host.Root, TrailShownBy(host).Items[1], this);

        Assert.AreEqual(BreadcrumbOutcome.NothingToOpen, outcome, "Expected nothing to open above the web page.");
        Assert.AreEqual(requests, host.Plan.Navigated.Count, "Expected no navigation.");
        Assert.AreEqual("Building/b1/Web/Page1", host.Root.ActualRoute.ToPath(), "Expected the same page.");
    }
}
