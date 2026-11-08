namespace Ecierge.Uno.Navigation.Tests;

using Ecierge.Uno.Navigation;
using Ecierge.Uno.Navigation.Tests.Doubles;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

/// <summary>
/// What the navigators do to the history of the window: one entry per navigation of a route, however many regions
/// took part in it, and none for a navigation that failed or that is not kept.
/// </summary>
[TestClass, UnitTestCategory, NavigationTestCategory]
public class NavigationHistoryTests
{
    private static NavigationTestHost CreateHost(BuildingServer? server = null)
     => TestRoutes.CreateHost(server ?? new BuildingServer());

    private static string[] Paths(NavigationTestHost host) => host.Root.Journal.Entries.Select(e => e.Path).ToArray();

    [TestMethod]
    public async Task A_route_through_several_regions_must_add_one_entry_with_the_whole_route()
    {
        using var host = CreateHost();

        await host.Root.NavigateRouteAsync(this, "Building/b1/Details/Floors");

        CollectionAssert.AreEqual(new[] { "Building/b1/Details/Floors" }, Paths(host), "Expected one entry from the root to the last page.");
    }

    [TestMethod]
    public async Task A_route_must_be_recorded_with_the_default_segments_it_was_completed_with()
    {
        using var host = CreateHost();

        await host.Root.NavigateRouteAsync(this, "Building/b1");

        CollectionAssert.AreEqual(new[] { "Building/b1/MyProperty" }, Paths(host), "Expected the default page to be part of the location.");
    }

    [TestMethod]
    public async Task Navigations_must_be_recorded_in_the_order_they_were_made()
    {
        using var host = CreateHost();

        await host.Root.NavigateRouteAsync(this, "Login");
        await host.Root.NavigateRouteAsync(this, "Building/b1/Menu");
        await ((FakeNavigator)host.Root.ChildNavigator!).NavigateRouteAsync(this, "Assets");

        CollectionAssert.AreEqual(new[] { "Login", "Building/b1/Menu", "Building/b1/Assets" }, Paths(host), "Expected a page of a nested region to be recorded as the whole route.");
        Assert.AreEqual(2, host.Root.Journal.CurrentIndex, "Expected the last page to be current.");
    }

    [TestMethod]
    public async Task Navigating_to_the_page_that_is_shown_must_not_add_an_entry()
    {
        using var host = CreateHost();
        await host.Root.NavigateRouteAsync(this, "Login");

        await host.Root.NavigateRouteAsync(this, "Login");

        CollectionAssert.AreEqual(new[] { "Login" }, Paths(host), "Expected the same location not to be a new step of the history.");
    }

    [TestMethod]
    public async Task A_redirect_with_Replace_must_not_leave_the_page_it_replaced()
    {
        using var host = CreateHost();
        await host.Root.NavigateRouteAsync(this, "Login");

        await host.Root.NavigateRouteAsync(this, "Building/b1/Menu", null, HistoryMode.Replace);

        CollectionAssert.AreEqual(new[] { "Building/b1/Menu" }, Paths(host), "Expected Back not to lead to the login page.");
    }

    [TestMethod]
    public async Task A_navigation_with_Skip_must_not_be_recorded()
    {
        using var host = CreateHost();
        await host.Root.NavigateRouteAsync(this, "Login");

        await host.Root.NavigateRouteAsync(this, "Building/b1/Menu", null, HistoryMode.Skip);

        CollectionAssert.AreEqual(new[] { "Login" }, Paths(host), "Expected the history not to know about a page that is not kept.");
    }

    [TestMethod]
    public async Task A_failed_navigation_must_not_be_recorded()
    {
        using var host = CreateHost(new BuildingServer { IsDown = true });
        await host.Root.NavigateRouteAsync(this, "Login");

        await host.Root.NavigateRouteAsync(this, "Building/b1/Menu");

        CollectionAssert.AreEqual(new[] { "Login" }, Paths(host), "Expected the history to hold only what was shown.");
    }

    [TestMethod]
    public async Task A_denied_navigation_must_not_be_recorded()
    {
        using var host = TestRoutes.CreateHost(
            new BuildingServer(),
            services =>
            {
                services.RemoveAll<IAuthorizationService>();
                services.AddSingleton<IAuthorizationService>(new PredicateAuthorizationService(route => route.ToPath() != "Building/b1/Menu"));
            });
        await host.Root.NavigateRouteAsync(this, "Login");

        var response = await host.Root.NavigateRouteAsync(this, "Building/b1/Menu");

        Assert.IsInstanceOfType<NavigationDeniedResponse>(response, "Expected the rules to refuse the page.");
        CollectionAssert.AreEqual(new[] { "Login" }, Paths(host), "Expected a page that was refused not to be in the history.");
    }

    [TestMethod]
    public async Task RouteChanged_must_be_raised_once_per_navigation_after_the_history_was_updated()
    {
        using var host = CreateHost();
        var seen = new List<string?>();
        host.Services.GetRequiredService<IRouteNotifier>().RouteChanged += (_, _) => seen.Add(host.Root.Journal.Current?.Path);

        await host.Root.NavigateRouteAsync(this, "Building/b1/Details/Floors");

        CollectionAssert.AreEqual(new[] { "Building/b1/Details/Floors" }, seen, "Expected one announcement for the whole route, with the entry already in the history.");
    }

    [TestMethod]
    public async Task RouteChanged_must_also_be_raised_when_the_navigation_failed()
    {
        using var host = CreateHost(new BuildingServer { IsDown = true });
        var responses = new List<NavigationResponse?>();
        host.Services.GetRequiredService<IRouteNotifier>().RouteChanged += (_, args) => responses.Add(args.Response);

        await host.Root.NavigateRouteAsync(this, "Building/b1/Menu");

        Assert.IsFalse(responses.Single()!.Success, "Expected listeners to learn that the route did not change as asked.");
    }

    [TestMethod]
    public async Task Overlapping_navigations_must_be_recorded_once_and_announced_once_with_the_mode_of_the_outermost()
    {
        using var host = CreateHost();
        await host.Root.NavigateRouteAsync(this, "Login", null, HistoryMode.Skip);
        var announced = 0;
        host.Services.GetRequiredService<IRouteNotifier>().RouteChanged += (_, _) => announced++;
        var updater = host.Services.GetRequiredService<IRouteUpdater>();
        var response = new NavigationSuccessfulResponse(host.Root.ActualRoute, host.Root);

        var outer = updater.StartNavigation(host.Root, HistoryMode.Push, NavigationMode.New);
        var inner = updater.StartNavigation(host.Root, HistoryMode.Skip, NavigationMode.New);
        await inner.CompleteAsync(response);
        Assert.IsEmpty(host.Root.Journal.Entries, "Expected nothing to be recorded while the outer navigation runs.");
        Assert.AreEqual(0, announced, "Expected nothing to be announced while the outer navigation runs.");
        await outer.CompleteAsync(response);

        CollectionAssert.AreEqual(new[] { "Login" }, Paths(host), "Expected the outermost navigation to decide what is recorded.");
        Assert.AreEqual(1, announced, "Expected one announcement for both navigations.");
    }

    [TestMethod]
    public async Task Navigations_started_from_several_threads_must_leave_no_navigation_open()
    {
        using var host = CreateHost();
        var updater = host.Services.GetRequiredService<IRouteUpdater>();
        await host.Root.NavigateRouteAsync(this, "Login", null, HistoryMode.Skip);
        var response = new NavigationSuccessfulResponse(host.Root.ActualRoute, host.Root);

        await Parallel.ForAsync(0, 100, async (_, _) =>
        {
            var navigation = updater.StartNavigation(host.Root, HistoryMode.Push, NavigationMode.New);
            await Task.Yield();
            await navigation.CompleteAsync(response);
        });
        await host.Root.NavigateRouteAsync(this, "Building/b1/Menu");

        CollectionAssert.AreEqual(new[] { "Login", "Building/b1/Menu" }, Paths(host), "Expected the count of open navigations to return to zero, so that the next one is recorded.");
    }
}
