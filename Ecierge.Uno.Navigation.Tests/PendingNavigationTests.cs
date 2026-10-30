namespace Ecierge.Uno.Navigation.Tests;

using Ecierge.Uno.Navigation;

[TestClass, UnitTestCategory, NavigationTestCategory]
public class PendingNavigationTests
{
    [TestMethod]
    public void Nothing_must_wait_in_a_new_holder()
    {
        var pending = new PendingNavigation();

        Assert.IsNull(pending.Path, "Expected no route to wait.");
        Assert.IsNull(pending.Consume(), "Expected nothing to take.");
    }

    [TestMethod]
    public void Consume_must_give_the_route_once()
    {
        var pending = new PendingNavigation();
        pending.Set("Building/3f2a/Assets");

        Assert.AreEqual("Building/3f2a/Assets", pending.Path, "Expected the route to be visible without taking it.");
        Assert.AreEqual("Building/3f2a/Assets", pending.Consume(), "Expected the route to be taken.");
        Assert.IsNull(pending.Consume(), "Expected the route to be opened once.");
        Assert.IsNull(pending.Path, "Expected nothing to wait after the route was taken.");
    }

    [TestMethod]
    public void Set_must_replace_the_route_that_waits()
    {
        var pending = new PendingNavigation();
        pending.Set("A");

        pending.Set("B");

        Assert.AreEqual("B", pending.Consume(), "Expected the latest link to win.");
    }

    [TestMethod]
    [DataRow("")]
    [DataRow("  ")]
    public void Set_must_refuse_a_text_without_a_route(string text)
    {
        Assert.ThrowsExactly<ArgumentException>(() => new PendingNavigation().Set(text), "Expected a route that has no text to be refused.");
    }
}
