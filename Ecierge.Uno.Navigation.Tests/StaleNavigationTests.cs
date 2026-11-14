namespace Ecierge.Uno.Navigation.Tests;

using Ecierge.Uno.Navigation;
using Ecierge.Uno.Navigation.Tests.Doubles;

using Microsoft.Extensions.DependencyInjection;

/// <summary>
/// A navigation that stays open holds back the history and the announcements of the routes that start after it, until
/// it ends or until it has been open for longer than the options allow.
/// </summary>
[TestClass, UnitTestCategory, NavigationTestCategory]
public class StaleNavigationTests
{
    private static readonly TimeSpan StaleAfter = TimeSpan.FromSeconds(10);

    private static NavigationTestHost CreateHost(ManualTimeProvider time, TimeSpan? staleAfter = null)
     => TestRoutes.CreateHost(
            new BuildingServer(),
            services => services.AddSingleton<TimeProvider>(time),
            options => options with { StaleNavigationTimeout = staleAfter ?? StaleAfter });

    private static string[] Paths(NavigationTestHost host) => host.Root.Journal.Entries.Select(e => e.Path).ToArray();

    private static RouteNavigation Start(NavigationTestHost host)
     => host.Services.GetRequiredService<IRouteUpdater>().StartNavigation(host.Root, HistoryMode.Push, NavigationMode.New);

    private static NavigationResponse Success(NavigationTestHost host) => new NavigationSuccessfulResponse(host.Root.ActualRoute, host.Root);

    [TestMethod]
    public async Task A_navigation_that_never_ends_must_not_hold_back_the_routes_that_start_after_it_is_stale()
    {
        var time = new ManualTimeProvider();
        using var host = CreateHost(time);
        var announced = 0;
        host.Services.GetRequiredService<IRouteNotifier>().RouteChanged += (_, _) => announced++;
        _ = Start(host);
        time.Advance(StaleAfter + TimeSpan.FromSeconds(1));

        await host.Root.NavigateRouteAsync(this, "Login");

        CollectionAssert.AreEqual(new[] { "Login" }, Paths(host), "Expected the route to be recorded although an earlier navigation never ended.");
        Assert.AreEqual(1, announced, "Expected the route to be announced.");
    }

    [TestMethod]
    public async Task A_navigation_that_is_open_for_less_than_the_time_must_hold_back_the_routes_that_start_after_it()
    {
        var time = new ManualTimeProvider();
        using var host = CreateHost(time);
        var announced = 0;
        host.Services.GetRequiredService<IRouteNotifier>().RouteChanged += (_, _) => announced++;
        var slow = Start(host);
        time.Advance(StaleAfter - TimeSpan.FromSeconds(1));

        await host.Root.NavigateRouteAsync(this, "Login");
        Assert.IsEmpty(host.Root.Journal.Entries, "Expected nothing to be recorded while the earlier navigation is open.");
        Assert.AreEqual(0, announced, "Expected nothing to be announced while the earlier navigation is open.");
        await slow.CompleteAsync(Success(host));

        CollectionAssert.AreEqual(new[] { "Login" }, Paths(host), "Expected the route to be recorded when the earlier navigation has ended.");
        Assert.AreEqual(1, announced, "Expected one announcement for both navigations.");
    }

    [TestMethod]
    public async Task A_navigation_must_never_become_stale_when_the_time_is_infinite()
    {
        var time = new ManualTimeProvider();
        using var host = CreateHost(time, Timeout.InfiniteTimeSpan);
        _ = Start(host);
        time.Advance(TimeSpan.FromDays(1));

        await host.Root.NavigateRouteAsync(this, "Login");

        Assert.IsEmpty(host.Root.Journal.Entries, "Expected an infinite time to wait for the navigation that is open.");
    }

    [TestMethod]
    public async Task A_stale_navigation_that_ends_late_must_be_announced_when_no_other_navigation_is_open()
    {
        var time = new ManualTimeProvider();
        using var host = CreateHost(time);
        var announced = 0;
        host.Services.GetRequiredService<IRouteNotifier>().RouteChanged += (_, _) => announced++;
        var slow = Start(host);
        time.Advance(StaleAfter + TimeSpan.FromSeconds(1));
        await host.Root.NavigateRouteAsync(this, "Login");
        await host.Root.NavigateRouteAsync(this, "Building/b1/Menu");

        await slow.CompleteAsync(Success(host));

        Assert.AreEqual(3, announced, "Expected the late end to be announced as the state of the window.");
        CollectionAssert.AreEqual(new[] { "Login", "Building/b1/Menu" }, Paths(host), "Expected the late end not to add an entry for the page that is shown already.");
    }

    [TestMethod]
    public async Task A_stale_navigation_that_ends_late_must_not_be_announced_while_another_navigation_is_open()
    {
        var time = new ManualTimeProvider();
        using var host = CreateHost(time);
        var announced = 0;
        host.Services.GetRequiredService<IRouteNotifier>().RouteChanged += (_, _) => announced++;
        var slow = Start(host);
        time.Advance(StaleAfter + TimeSpan.FromSeconds(1));
        var current = Start(host);

        await slow.CompleteAsync(Success(host));
        Assert.AreEqual(0, announced, "Expected the late end to wait for the navigation that is open.");
        await current.CompleteAsync(Success(host));

        Assert.AreEqual(1, announced, "Expected the navigation that was open to be announced when it ended.");
    }

    [TestMethod]
    public async Task An_end_that_is_reported_twice_must_count_as_one_end()
    {
        var time = new ManualTimeProvider();
        using var host = CreateHost(time);
        var announced = 0;
        host.Services.GetRequiredService<IRouteNotifier>().RouteChanged += (_, _) => announced++;
        var navigation = Start(host);

        await navigation.CompleteAsync(Success(host));
        await navigation.CompleteAsync(Success(host));
        await host.Root.NavigateRouteAsync(this, "Login");

        CollectionAssert.AreEqual(new[] { "Login" }, Paths(host), "Expected the next navigation to be recorded.");
        Assert.AreEqual(2, announced, "Expected one announcement for the navigation and one for the next, none for the repeated end.");
    }
}
