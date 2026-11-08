namespace Ecierge.Uno.Navigation.Tests;

using System.Collections.Immutable;

using Ecierge.Uno.Navigation.Routing;

[TestClass, UnitTestCategory, NavigationTestCategory]
public class RouteTests
{
    private static readonly NameSegment Info = new("Info", isDefault: true);
    private static readonly NameSegment Floors = new("Floors");
    private static readonly NameSegment Details = new("Details", nested: [Info, Floors]);
    private static readonly NameSegment Menu = new("Menu");
    private static readonly DialogSegment AddAsset = new("AddAsset");
    private static readonly DataSegment BuildingData = new("BuildingInfo", null, true, Menu, Details);
    private static readonly NameSegment Building = new("Building", null, BuildingData);
    private static readonly NameSegment Login = new("Login");

    private static Routing.Route BuildingRoute(string id = "3f2a", Task? entity = null)
     => Routing.Route.Empty.Add(BuildingData, id, entity);

    [TestMethod]
    public void ToPath_must_write_names_values_and_dialogs()
    {
        var route = BuildingRoute().Add(Details).Add(Floors);

        Assert.AreEqual("Building/3f2a/Details/Floors", route.ToPath(), "Expected the value of the data segment in place of its name.");
        Assert.AreEqual("Building/BuildingInfo/Details/Floors", route.ToString(), "Expected the debug text to show the name of the data segment.");
    }

    [TestMethod]
    public void ToPath_must_write_a_dialog_with_the_dialog_qualifier()
    {
        var route = BuildingRoute().Add(AddAsset);

        Assert.AreEqual("Building/3f2a/!AddAsset", route.ToPath(), "Expected the dialog to be written with its qualifier.");
        Assert.IsTrue(route.Segments[^1] is DialogSegmentInstance, "Expected a dialog to be added as a dialog step.");
    }

    [TestMethod]
    public void ToPath_must_write_an_empty_route_as_empty_text()
    {
        Assert.AreEqual(string.Empty, Routing.Route.Empty.ToPath(), "Expected an empty route to have no text.");
    }

    [TestMethod]
    public void Route_must_not_hold_two_dialogs()
    {
        var other = new DialogSegment("AddModel");
        var route = BuildingRoute().Add(AddAsset);

        Assert.ThrowsExactly<InvalidOperationException>(() => route.Add(other), "Expected a second dialog to be refused.");
    }

    [TestMethod]
    public void Route_must_never_have_null_data()
    {
        var route = new Routing.Route(ImmutableArray<RouteSegmentInstance>.Empty, null);

        Assert.IsNotNull(route.Data, "Expected a route without data to have empty data.");
        Assert.AreEqual(0, route.Data.Count, "Expected the empty data to hold nothing.");
    }

    [TestMethod]
    public void Add_must_refuse_a_segment_that_is_not_nested_in_the_last_one()
    {
        var route = Routing.Route.Empty.Add(Login);

        Assert.ThrowsExactly<InvalidOperationException>(() => route.Add(Menu), "Expected a segment of another branch to be refused.");
    }

    [TestMethod]
    public void CommonPrefixLength_must_compare_data_segments_by_value_without_regard_to_case()
    {
        var a = BuildingRoute("ABC").Add(Menu);
        var b = BuildingRoute("abc", Task.FromResult("entity")).Add(Details);
        var other = BuildingRoute("def").Add(Menu);

        Assert.AreEqual(2, a.CommonPrefixLength(b), "Expected the building and its value to be common, and the pages to differ.");
        Assert.AreEqual(1, a.CommonPrefixLength(other), "Expected only the named segment to be common for another value.");
        Assert.IsTrue(BuildingRoute("abc").IsPrefixOf(b), "Expected the building route to be a prefix of its page.");
        Assert.IsFalse(a.IsPrefixOf(b), "Expected a page not to be a prefix of another page.");
    }

    [TestMethod]
    public void Take_must_drop_the_data_of_the_dropped_data_segments()
    {
        var route = BuildingRoute("3f2a", Task.FromResult("entity")).Add(Details);
        Assert.IsTrue(route.Data.ContainsKey("BuildingInfo"), "Expected the entity task to be the data of the route.");

        var taken = route.Take(1);

        Assert.AreEqual(1, taken.Segments.Length, "Expected only the first step to be kept.");
        Assert.IsFalse(taken.Data.ContainsKey("BuildingInfo"), "Expected the data of the dropped data segment to be dropped with it.");
    }

    [TestMethod]
    public void Take_must_return_the_route_when_it_is_not_longer()
    {
        var route = BuildingRoute().Add(Menu);

        Assert.AreSame(route, route.Take(5), "Expected a route that is not longer than the count to stay as it is.");
    }

    [TestMethod]
    public void TrimTill_must_keep_the_route_when_the_step_is_not_in_it()
    {
        var route = BuildingRoute().Add(Menu);
        var step = new NameSegmentInstance(Login);

        Assert.AreSame(route, route.TrimTill(step), "Expected the route to stay as it is for a step that is not in it.");
        Assert.AreSame(route, route.TrimTill(Details), "Expected the route to stay as it is for a segment that is not in it.");
    }

    [TestMethod]
    public void TrimTill_must_drop_the_steps_after_the_segment()
    {
        var route = BuildingRoute().Add(Details).Add(Floors);

        var trimmed = route.TrimTill(Details);

        Assert.AreEqual("Building/3f2a/Details", trimmed.ToPath(), "Expected the steps after the segment to be dropped.");
    }

    [TestMethod]
    public void TrimHead_must_start_with_the_last_step_of_the_other_route()
    {
        var shown = BuildingRoute().Add(Details).Add(Floors);
        var navigatorRoute = BuildingRoute().Add(Details);

        var tail = shown.TrimHead(navigatorRoute);

        Assert.AreEqual("Details/Floors", tail.ToPath(), "Expected the tail to start with the step of the navigator.");
        Assert.AreSame(shown, shown.TrimHead(Routing.Route.Empty), "Expected an empty route to trim nothing.");
    }

    [TestMethod]
    public void LastNamedSegment_must_skip_a_trailing_data_segment_and_name_a_dialog()
    {
        Assert.AreEqual("Building", BuildingRoute().LastNamedSegment?.NameSegment.Name, "Expected the named segment of the data segment.");
        Assert.AreEqual("AddAsset", BuildingRoute().Add(AddAsset).LastNamedSegment?.NameSegment.Name, "Expected a dialog to count as a named segment.");
        Assert.IsNull(Routing.Route.Empty.LastNamedSegment, "Expected an empty route to have no named segment.");
    }

    [TestMethod]
    public void WithoutDialog_must_drop_the_dialog_and_what_is_inside_it()
    {
        var route = BuildingRoute().Add(AddAsset);

        Assert.AreEqual("Building/3f2a", route.WithoutDialog().ToPath(), "Expected the page route without the dialog.");
        var page = BuildingRoute();
        Assert.AreSame(page, page.WithoutDialog(), "Expected a route without a dialog to stay as it is.");
    }

    [TestMethod]
    public void StartsWithSegments_must_compare_the_whole_route_by_the_names_of_the_steps()
    {
        var route = BuildingRoute().Add(Menu);

        Assert.IsTrue(route.StartsWithSegments(["Building", "BuildingInfo", "Menu"]), "Expected the names of the steps to match.");
        Assert.IsFalse(route.StartsWithSegments(["Building", "BuildingInfo"]), "Expected a shorter list not to match.");
    }
}
