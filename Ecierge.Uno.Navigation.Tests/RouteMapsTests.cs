namespace Ecierge.Uno.Navigation.Tests;

using Ecierge.Uno.Navigation;
using Ecierge.Uno.Navigation.Tests.Doubles;

using Microsoft.Extensions.DependencyInjection;

/// <summary>
/// The route that starts with a segment of the route map: its data segment needs a value, unless the data is optional.
/// </summary>
[TestClass, UnitTestCategory, NavigationTestCategory]
public class RouteMapsTests
{
    private const string Search = "Search";
    private const string SearchBuilding = "SearchBuilding";

    private static NameSegment Segment(NavigationTestHost host, string name)
     => host.Services.GetRequiredService<IRouteRegistry>().RootSegment[name];

    /// <summary>A page whose building is optional, next to the pages of <see cref="TestRoutes"/>.</summary>
    private static NavigationTestHost CreateHostWithOptionalData()
     => NavigationTestHost.Create(
            (views, data, routes) =>
            {
                TestRoutes.Register(views, data, routes);
                routes.Register((v, d) => [
                    new NameSegment(
                        Search,
                        v[typeof(PageView)],
                        new DataSegment(SearchBuilding, d[typeof(Building)], false, new NameSegment("Results", v[typeof(PageView)], isDefault: true)))
                ]);
            },
            services => services.AddSingleton(new BuildingServer()));

    [TestMethod]
    public void BuildDefaultRoute_without_data_must_refuse_a_mandatory_data_segment()
    {
        using var host = TestRoutes.CreateHost(new BuildingServer());

        Assert.ThrowsExactly<ArgumentNullException>(
            () => Segment(host, TestRoutes.Building).BuildDefaultRoute(host.Services),
            "Expected a page of a building to be refused without the building.");
    }

    [TestMethod]
    public void BuildDefaultRoute_with_route_data_that_lacks_the_value_must_refuse_a_mandatory_data_segment()
    {
        using var host = TestRoutes.CreateHost(new BuildingServer());

        var error = Assert.ThrowsExactly<ArgumentException>(
            () => Segment(host, TestRoutes.Building).BuildDefaultRoute(host.Services, NavigationData.Empty),
            "Expected route data without the building to be refused like no data at all.");

        Assert.Contains(TestRoutes.BuildingData, error.Message, "Expected the message to name the data segment that has no value.");
    }

    [TestMethod]
    public void BuildDefaultRoute_with_route_data_that_has_the_value_must_hold_the_handle_and_add_the_default_page()
    {
        using var host = TestRoutes.CreateHost(new BuildingServer());
        var data = NavigationData.Empty.SetItem(TestRoutes.BuildingData, "b1");

        var route = Segment(host, TestRoutes.Building).BuildDefaultRoute(host.Services, data);

        Assert.AreEqual("Building/b1/MyProperty", route.ToPath(), "Expected the value of the building and the default page.");
        Assert.IsTrue(route.Data.TryGetValue(TestRoutes.BuildingData, out var held), "Expected the route to hold the building.");
        Assert.IsInstanceOfType<INavigationEntity>(held, "Expected the route to hold the handle of the building.");
    }

    [TestMethod]
    public void BuildDefaultRoute_with_the_value_itself_must_hold_the_handle_and_add_the_default_page()
    {
        using var host = TestRoutes.CreateHost(new BuildingServer());

        var route = Segment(host, TestRoutes.Building).BuildDefaultRoute(host.Services, new Building("b1", "Hall"));

        Assert.AreEqual("Building/b1/MyProperty", route.ToPath(), "Expected the value of the building and the default page.");
    }

    [TestMethod]
    public void BuildDefaultRoute_without_a_value_must_leave_out_an_optional_data_segment()
    {
        using var host = CreateHostWithOptionalData();

        var withoutData = Segment(host, Search).BuildDefaultRoute(host.Services);
        var withEmptyRouteData = Segment(host, Search).BuildDefaultRoute(host.Services, NavigationData.Empty);

        Assert.AreEqual(Search, withoutData.ToPath(), "Expected the page alone when no data is given.");
        Assert.AreEqual(Search, withEmptyRouteData.ToPath(), "Expected the page alone when the route data has no value for it.");
    }

    [TestMethod]
    public void BuildDefaultRoute_with_a_value_must_add_an_optional_data_segment()
    {
        using var host = CreateHostWithOptionalData();
        var data = NavigationData.Empty.SetItem(SearchBuilding, "b1");

        var route = Segment(host, Search).BuildDefaultRoute(host.Services, data);

        Assert.AreEqual("Search/b1/Results", route.ToPath(), "Expected the value and the default page below it.");
    }
}
