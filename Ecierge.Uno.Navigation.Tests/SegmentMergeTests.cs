namespace Ecierge.Uno.Navigation.Tests;

using Ecierge.Uno.Navigation.Helpers;

/// <summary>
/// The modules of an application register a segment of the same name more than once, and the registrations are merged
/// into one segment. What a registration says about the breadcrumbs must survive the merge.
/// </summary>
[TestClass, UnitTestCategory, NavigationTestCategory]
public class SegmentMergeTests
{
    [TestMethod]
    public void A_merged_segment_must_keep_the_title_that_one_registration_has()
    {
        var merged = Segments.Merge(
            new NameSegment("Tenant", nested: [new NameSegment("Staff")]) { Title = "Account" },
            new NameSegment("Tenant", nested: [new NameSegment("Roles")]));

        Assert.AreEqual("Account", merged.Title, "Expected the title of the registration that has one.");
        Assert.HasCount(2, merged.Nested, "Expected the nested segments of both registrations.");
    }

    [TestMethod]
    public void A_merged_segment_with_data_must_keep_the_title_and_hide_the_breadcrumbs_if_one_registration_does()
    {
        var merged = Segments.Merge(
            new NameSegment("Building", null, new DataSegment("BuildingInfo", null, true, new NameSegment("Menu"))),
            new NameSegment("Building", null, new DataSegment("BuildingInfo", null, true, new NameSegment("Web")))
            {
                Title = "Building",
                HidesBreadcrumbs = true
            });

        Assert.AreEqual("Building", merged.Title, "Expected the title of the second registration.");
        Assert.IsTrue(merged.HidesBreadcrumbs, "Expected the breadcrumbs to stay hidden as one registration asked.");
        Assert.HasCount(2, merged.DataSegment!.Nested, "Expected the nested segments of both data segments.");
    }

    [TestMethod]
    public void Registrations_with_the_same_title_must_be_merged()
    {
        var merged = Segments.Merge(
            new NameSegment("Tenant") { Title = "Account" },
            new NameSegment("Tenant") { Title = "Account" });

        Assert.AreEqual("Account", merged.Title, "Expected the title that both registrations have.");
        Assert.IsFalse(merged.HidesBreadcrumbs, "Expected the breadcrumbs to be shown when no registration hides them.");
    }

    [TestMethod]
    public void Registrations_with_different_titles_must_not_be_merged()
    {
        Assert.ThrowsExactly<InvalidOperationException>(
            () => Segments.Merge(
                new NameSegment("Staff") { Title = "Account users" },
                new NameSegment("Staff") { Title = "Building users" }),
            "Expected two titles of one segment to be refused, as the title shown would depend on the order of the modules.");
    }
}
