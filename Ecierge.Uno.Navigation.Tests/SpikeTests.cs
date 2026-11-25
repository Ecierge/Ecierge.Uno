namespace Ecierge.Uno.Navigation.Tests;

using Ecierge.Uno.Navigation;

using Microsoft.Extensions.DependencyInjection;

[TestClass, UnitTestCategory, NavigationTestCategory]
public class SpikeTests
{
    [TestMethod]
    public void NavigationData_must_be_created_without_a_user_interface()
    {
        var data = NavigationData.Empty.Add("building", "id-1");

        Assert.AreEqual(1, data.Count, "Expected the data to hold the added item.");
    }

    [TestMethod]
    public void Empty_route_must_have_no_segments()
    {
        var route = Routing.Route.Empty;

        Assert.AreEqual(0, route.Segments.Length, "Expected an empty route to have no segments.");
    }

    [TestMethod]
    public void Navigation_services_must_be_registered_without_a_user_interface()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddNavigation();
        using var provider = services.BuildServiceProvider();

        var status = provider.GetRequiredService<INavigationStatus>();

        Assert.IsNotNull(status, "Expected the navigation status service to be registered.");
    }
}
