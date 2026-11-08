namespace Ecierge.Uno.Navigation.Tests;

using Ecierge.Uno.Navigation.Routing;

[TestClass, UnitTestCategory, NavigationTestCategory]
public class RoutePathTests
{
    [TestMethod]
    [DataRow("Menu", RoutePathStart.Local, 0, "Menu")]
    [DataRow("Building/3f2a/Details/Floors", RoutePathStart.Local, 0, "Building|3f2a|Details|Floors")]
    [DataRow("/Building/3f2a", RoutePathStart.Root, 0, "Building|3f2a")]
    [DataRow("/", RoutePathStart.Root, 0, "")]
    [DataRow("./Info", RoutePathStart.Nested, 0, "Info")]
    [DataRow("../Menu", RoutePathStart.Parent, 1, "Menu")]
    [DataRow("../../Menu", RoutePathStart.Parent, 2, "Menu")]
    [DataRow("-", RoutePathStart.Back, 1, "")]
    [DataRow("---", RoutePathStart.Back, 3, "")]
    [DataRow("-/Login", RoutePathStart.ClearHistory, 0, "Login")]
    [DataRow("-/", RoutePathStart.ClearHistory, 0, "")]
    [DataRow("Building/3f2a/!AddAsset", RoutePathStart.Local, 0, "Building|3f2a|!AddAsset")]
    [DataRow("", RoutePathStart.Local, 0, "")]
    public void TryParse_must_split_the_text_into_start_levels_and_segments(string text, RoutePathStart start, int levels, string joinedSegments)
    {
        var parsed = RoutePath.TryParse(text, out var path);

        Assert.IsTrue(parsed, $"Expected '{text}' to be a well formed route.");
        Assert.AreEqual(start, path.Start, $"Expected the route '{text}' to start in a different place.");
        Assert.AreEqual(levels, path.Levels, $"Expected a different number of levels for '{text}'.");
        Assert.AreEqual(joinedSegments, string.Join('|', path.Segments), $"Expected different segments for '{text}'.");
    }

    [TestMethod]
    [DataRow("Building//Details")]
    [DataRow("//Building")]
    [DataRow("-Menu")]
    [DataRow("--/Menu")]
    [DataRow("Building/!")]
    [DataRow("!")]
    public void TryParse_must_refuse_a_text_that_is_not_a_route(string text)
    {
        var parsed = RoutePath.TryParse(text, out _);

        Assert.IsFalse(parsed, $"Expected '{text}' not to be a well formed route.");
    }

    [TestMethod]
    public void TryParse_must_refuse_null()
    {
        var parsed = RoutePath.TryParse(null, out _);

        Assert.IsFalse(parsed, "Expected null not to be a route.");
    }

    [TestMethod]
    public void TryParse_must_allow_one_trailing_separator()
    {
        var parsed = RoutePath.TryParse("Building/3f2a/", out var path);

        Assert.IsTrue(parsed, "Expected a trailing separator to be allowed.");
        Assert.AreEqual("Building|3f2a", string.Join('|', path.Segments), "Expected the trailing separator to be dropped.");
    }

    [TestMethod]
    public void TryParse_must_keep_the_text_after_the_query_separator()
    {
        var parsed = RoutePath.TryParse("Building/3f2a?tab=2&x=y", out var path);

        Assert.IsTrue(parsed, "Expected a route with a query to be well formed.");
        Assert.AreEqual("tab=2&x=y", path.Query, "Expected the query to be kept as text.");
        Assert.AreEqual("Building|3f2a", string.Join('|', path.Segments), "Expected the query not to be a part of the segments.");
    }

    [TestMethod]
    [DataRow("Menu")]
    [DataRow("/Building/3f2a/Details")]
    [DataRow("./Info")]
    [DataRow("../../Menu")]
    [DataRow("--")]
    [DataRow("-/Login")]
    [DataRow("Building/3f2a/!AddAsset?x=1")]
    public void ToString_must_write_the_text_that_was_read(string text)
    {
        var path = RoutePath.Parse(text);

        Assert.AreEqual(text, path.ToString(), "Expected the route to be written as it was read.");
    }

    [TestMethod]
    public void Parse_must_throw_for_a_text_that_is_not_a_route()
    {
        Assert.ThrowsExactly<FormatException>(() => RoutePath.Parse("A//B"), "Expected a malformed route to be refused.");
    }
}
