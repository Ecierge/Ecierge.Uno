namespace Ecierge.Uno.Navigation.Tests;

using Ecierge.Uno.Navigation;
using Ecierge.Uno.Navigation.Tests.Doubles;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

[TestClass, UnitTestCategory, NavigationTestCategory]
public class NavigatorOrchestrationTests
{
    private static NavigationTestHost CreateHost()
     => NavigationTestHost.Create((views, data, routes) =>
            routes.Register((v, d) => [
                new NameSegment("A", nested: [new NameSegment("B"), new NameSegment("C")]),
                new NameSegment("X")
            ]));

    [TestMethod]
    public async Task A_route_of_two_segments_must_be_navigated_through_two_navigators_in_order()
    {
        using var host = CreateHost();

        var response = await host.Root.NavigateRouteAsync(this, "A/B");

        Assert.IsTrue(response.Success, "Expected the route to be navigated.");
        Assert.AreEqual("A", host.Root.Requests.Single().NameSegment.Name, "Expected the root navigator to navigate the first segment.");
        var child = host.Root.ChildNavigator as FakeNavigator;
        Assert.IsNotNull(child, "Expected the view of the first segment to have attached a region.");
        Assert.AreEqual("B", child.Requests.Single().NameSegment.Name, "Expected the child navigator to navigate the second segment.");
        Assert.AreEqual("A/B", host.Root.ActualRoute.ToPath(), "Expected the leaf navigator to hold the whole route.");
    }

    [TestMethod]
    public async Task A_denied_route_must_not_reach_the_navigator()
    {
        using var host = NavigationTestHost.Create(
            (views, data, routes) => routes.Register((v, d) => [new NameSegment("A")]),
            services =>
            {
                // One service that allows a route is enough, so the one that allows everything has to go
                services.RemoveAll<IAuthorizationService>();
                services.AddSingleton<IAuthorizationService>(new PredicateAuthorizationService(route => false, "No access"));
            });

        var response = await host.Root.NavigateRouteAsync(this, "A");

        Assert.IsFalse(response.Success, "Expected a denied route to fail.");
        Assert.IsInstanceOfType<NavigationDeniedResponse>(response, "Expected the response to tell that the rules refused the route.");
        Assert.AreEqual(0, host.Root.Requests.Count, "Expected the navigator not to be asked to show a denied segment.");
    }
}
