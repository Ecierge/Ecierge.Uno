namespace Ecierge.Uno.Navigation.Tests;

using Ecierge.Uno.Navigation;
using Ecierge.Uno.Navigation.Tests.Doubles;

/// <summary>
/// The pages below a data segment need the entity of the segment. A page is created after the entity has loaded, a
/// load that fails ends the navigation with an error and a retry, and a retry after the cause is gone loads again.
/// </summary>
[TestClass, UnitTestCategory, NavigationTestCategory]
public class DataGatingTests
{
    private const string BuildingInfo = "BuildingInfo";

    private static FakeNavigator Layout(NavigationTestHost host)
    {
        var layout = host.Root.ChildNavigator as FakeNavigator;
        Assert.IsNotNull(layout, "Expected the building page to have attached the region of its pages.");
        return layout;
    }

    [TestMethod]
    public async Task Pages_below_a_data_segment_must_wait_until_the_entity_is_loaded()
    {
        var answer = new TaskCompletionSource<Building>();
        var server = new BuildingServer { Answer = _ => answer.Task };
        using var host = TestRoutes.CreateHost(server);

        var navigation = host.Root.NavigateRouteAsync(this, "Building/b1/Menu").AsTask();

        Assert.IsFalse(navigation.IsCompleted, "Expected the navigation to wait for the building.");
        Assert.AreEqual("Building", host.Root.Requests.Single().NameSegment.Name, "Expected the building page to be created at once, so that it can show its progress.");
        Assert.IsEmpty(Layout(host).Requests, "Expected no page below the building before the building is loaded.");

        answer.SetResult(new Building("b1", "Hall"));
        var response = await navigation;

        Assert.IsTrue(response.Success, "Expected the navigation to succeed after the building loaded.");
        Assert.AreEqual("Menu", Layout(host).Requests.Single().NameSegment.Name, "Expected the page to be created after the load.");
        Assert.ContainsSingle(server.Requests, "Expected one request for the building, whoever reads it.");
    }

    [TestMethod]
    public async Task A_navigation_that_waits_for_an_entity_must_announce_it()
    {
        var answer = new TaskCompletionSource<Building>();
        using var host = TestRoutes.CreateHost(new BuildingServer { Answer = _ => answer.Task });
        NavigationEntityLoadingEventArgs? announced = null;
        host.Status.EntityLoading += (_, args) => announced = args;

        var navigation = host.Root.NavigateRouteAsync(this, "Building/b1/Menu").AsTask();

        Assert.IsNotNull(announced, "Expected a page that shows progress to be told what is awaited.");
        Assert.AreEqual("b1", announced.Entity.Primitive, "Expected the entity of the building.");
        Assert.IsFalse(announced.Task.IsCompleted, "Expected the task to be pending while the building loads.");

        answer.SetResult(new Building("b1", "Hall"));
        await navigation;
        Assert.IsTrue(announced.Task.IsCompletedSuccessfully, "Expected the task to follow the load.");
    }

    [TestMethod]
    public async Task A_loaded_entity_must_not_be_announced_or_awaited()
    {
        using var host = TestRoutes.CreateHost(new BuildingServer());
        var announced = 0;
        host.Status.EntityLoading += (_, _) => announced++;

        var response = await host.Root.NavigateRouteAsync(this, "Building/b1/Menu");

        Assert.IsTrue(response.Success, "Expected the navigation to succeed.");
        Assert.AreEqual(0, announced, "Expected the building, which loaded at once, not to be announced.");
    }

    [TestMethod]
    public async Task A_failed_load_must_end_the_navigation_with_the_error_and_a_retry()
    {
        var server = new BuildingServer { IsDown = true };
        using var host = TestRoutes.CreateHost(server);

        var response = await host.Root.NavigateRouteAsync(this, "Building/b1/Menu");

        var failed = response as NavigationFailedResponse;
        Assert.IsNotNull(failed, "Expected a failure, not a page that is created without its building.");
        Assert.IsNotInstanceOfType<NavigationDeniedResponse>(failed, "Expected a load that failed not to look like a refusal.");
        Assert.IsInstanceOfType<HttpRequestException>(failed.Error, "Expected the failure of the load to be reported.");
        Assert.IsNotNull(failed.Retry, "Expected the navigation to be repeatable.");
        Assert.AreEqual("Building/b1/Menu", failed.RequestedRoute!.ToPath(), "Expected the whole requested route to be kept for the retry.");
        Assert.AreEqual("Building/b1", failed.Route.ToPath(), "Expected the route up to the page that could not be created.");
        Assert.IsEmpty(Layout(host).Requests, "Expected no page to be created without the building.");
    }

    [TestMethod]
    public async Task A_failed_navigation_must_raise_NavigationFailed_after_NavigationFinished()
    {
        using var host = TestRoutes.CreateHost(new BuildingServer { IsDown = true });
        var events = new List<string>();
        host.Status.NavigationFinished += (_, response) => events.Add("finished " + response.Success);
        host.Status.NavigationFailed += (_, response) => events.Add("failed " + response.Error?.Message);

        await host.Root.NavigateRouteAsync(this, "Building/b1/Menu");

        CollectionAssert.AreEqual(new[] { "finished False", "failed The server is down" }, events, "Expected listeners to learn of the failure and its cause.");
    }

    [TestMethod]
    public async Task Retry_after_the_server_is_back_must_load_the_building_again_and_open_the_page()
    {
        var server = new BuildingServer { IsDown = true };
        using var host = TestRoutes.CreateHost(server);
        var failed = (NavigationFailedResponse)await host.Root.NavigateRouteAsync(this, "Building/b1/Menu");
        server.IsDown = false;

        var response = await failed.Retry!();

        Assert.IsTrue(response.Success, "Expected the retry to succeed once the server is back.");
        Assert.AreEqual(NavigationMode.Refresh, response.Mode, "Expected the retry to be told apart from a new navigation.");
        Assert.AreEqual("Menu", Layout(host).Requests.Single().NameSegment.Name, "Expected the page that failed to open to be created now.");
        Assert.ContainsSingle(host.Root.Requests, "Expected the building page to stay as it was: the retry does not create it again.");
    }

    [TestMethod]
    public async Task Pages_opened_after_the_recovery_must_share_the_loaded_entity_without_another_request()
    {
        var server = new BuildingServer { IsDown = true };
        using var host = TestRoutes.CreateHost(server);
        var failed = (NavigationFailedResponse)await host.Root.NavigateRouteAsync(this, "Building/b1/Menu");
        server.IsDown = false;
        await failed.Retry!();
        var requestsAfterRecovery = server.Requests.Count;
        var layout = Layout(host);

        var response = await layout.NavigateLocalSegmentAsync(this, "Assets");

        Assert.IsTrue(response.Success, "Expected the next page to open.");
        Assert.HasCount(requestsAfterRecovery, server.Requests, "Expected the building that was loaded not to be requested again.");
        var menu = layout.Requests[0].Route.Data[BuildingInfo];
        var assets = layout.Requests[1].Route.Data[BuildingInfo];
        Assert.AreSame(menu, assets, "Expected both pages to receive the same handle of the building.");
        Assert.IsTrue(((INavigationEntity)assets).TryGetLoadedEntity(out var loaded) && ((Building)loaded).Id == "b1", "Expected the page to receive the building that loaded after the recovery, not the failed attempt.");
    }

    [TestMethod]
    public async Task A_page_opened_while_the_server_is_still_down_must_fail_and_open_once_it_is_back()
    {
        var server = new BuildingServer { IsDown = true };
        using var host = TestRoutes.CreateHost(server);
        await host.Root.NavigateRouteAsync(this, "Building/b1/Menu");
        var layout = Layout(host);

        var whileDown = await layout.NavigateLocalSegmentAsync(this, "Assets");
        server.IsDown = false;
        var afterRecovery = await layout.NavigateLocalSegmentAsync(this, "Assets");

        Assert.IsInstanceOfType<NavigationFailedResponse>(whileDown, "Expected the page not to open without the building.");
        Assert.IsTrue(afterRecovery.Success, "Expected the same navigation to succeed with no refresh of the building page: the handle loads itself again.");
        Assert.AreEqual("Assets", layout.Requests.Single().NameSegment.Name, "Expected only the page that could be created to be navigated.");
    }

    [TestMethod]
    public async Task A_second_failed_retry_must_fail_again_and_stay_repeatable()
    {
        var server = new BuildingServer { IsDown = true };
        using var host = TestRoutes.CreateHost(server);
        var first = (NavigationFailedResponse)await host.Root.NavigateRouteAsync(this, "Building/b1/Menu");

        var second = await first.Retry!() as NavigationFailedResponse;

        Assert.IsNotNull(second, "Expected the retry to fail while the server is down.");
        Assert.IsNotNull(second.Retry, "Expected the failed retry to be repeatable too.");
        Assert.AreEqual(NavigationMode.Refresh, second.Mode, "Expected the failed retry to say it was a retry.");
    }

    [TestMethod]
    public async Task A_canceled_attempt_must_be_replaced_by_a_new_one_before_the_pages_are_created()
    {
        var calls = 0;
        var server = new BuildingServer
        {
            Answer = id => ++calls == 1
                ? Task.FromCanceled<Building>(new CancellationToken(true))
                : Task.FromResult(new Building(id, "Hall"))
        };
        using var host = TestRoutes.CreateHost(server);

        var response = await host.Root.NavigateRouteAsync(this, "Building/b1/Menu");

        Assert.IsTrue(response.Success, "Expected the page to open: a canceled attempt, for example of a page that was left, is no answer.");
        Assert.HasCount(2, server.Requests, "Expected the building to be requested again after the cancellation.");
    }

    [TestMethod]
    public async Task A_route_without_a_data_segment_must_not_wait_for_anything()
    {
        using var host = TestRoutes.CreateHost(new BuildingServer { IsDown = true });

        var response = await host.Root.NavigateRouteAsync(this, "Login");

        Assert.IsTrue(response.Success, "Expected a page that needs no entity to open while the server is down.");
    }
}
