namespace Ecierge.Uno.Navigation.Tests;

using Ecierge.Uno.Navigation;
using Ecierge.Uno.Navigation.Routing;
using Ecierge.Uno.Navigation.Tests.Doubles;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

/// <summary>
/// Opening the page that a link names: the common part of what is shown stays, the rules still decide, and a link that
/// names no page fails with the reason.
/// </summary>
[TestClass, UnitTestCategory, NavigationTestCategory]
public class DeepLinkNavigationTests
{
    private static NavigationTestHost CreateHost(Action<IServiceCollection>? configureServices = null)
     => TestRoutes.CreateHost(new BuildingServer(), configureServices, options => options with { DeepLinkScheme = "ecierge-console" });

    [TestMethod]
    public async Task A_link_must_open_the_page_below_the_shell_that_is_shown()
    {
        using var host = CreateHost();
        await host.Root.NavigateRouteAsync(this, "Building/b1/Menu");
        var layout = (FakeNavigator)host.Root.ChildNavigator!;

        var response = await layout.NavigateToLinkAsync(this, new Uri("ecierge-console://Building/b1/Assets"));

        Assert.IsTrue(response.Success, "Expected the page to open.");
        Assert.ContainsSingle(host.Root.Requests, "Expected the building page, which is right, to stay.");
        CollectionAssert.AreEqual(new[] { "Menu", "Assets" }, layout.Requests.Select(r => r.NameSegment.Name).ToArray(), "Expected only the page below the building to change.");
        CollectionAssert.AreEqual(new[] { "Building/b1/Menu", "Building/b1/Assets" }, host.Root.Journal.Entries.Select(e => e.Path).ToArray(), "Expected the link to be a step of the history.");
    }

    [TestMethod]
    public async Task A_link_must_open_the_first_page_of_a_window_that_shows_nothing()
    {
        using var host = CreateHost();

        var response = await host.Root.NavigateToLinkAsync(this, new Uri("https://console.example.com/Building/b1/Details/Floors"));

        Assert.IsTrue(response.Success, "Expected the page to open from nothing.");
        Assert.AreEqual("Building/b1/Details/Floors", host.Root.Journal.Current!.Path, "Expected the whole route of the link to be shown.");
    }

    [TestMethod]
    public async Task A_link_may_replace_the_current_entry_of_the_history()
    {
        using var host = CreateHost();
        await host.Root.NavigateRouteAsync(this, "Login");

        await host.Root.NavigateToLinkAsync(this, new Uri("ecierge-console://Building/b1/Menu"), HistoryMode.Replace);

        CollectionAssert.AreEqual(new[] { "Building/b1/Menu" }, host.Root.Journal.Entries.Select(e => e.Path).ToArray(), "Expected the page of the link to take the place of the login.");
    }

    [TestMethod]
    public async Task A_link_that_is_not_of_the_application_must_fail_with_the_reason()
    {
        using var host = CreateHost();

        var response = await host.Root.NavigateToLinkAsync(this, new Uri("com.example.auth://callback/code"));

        var failed = response as NavigationFailedResponse;
        Assert.IsNotNull(failed, "Expected a failure.");
        Assert.ContainsSingle(failed.Errors, "Expected the reason to be told.");
        Assert.IsEmpty(host.Root.Requests, "Expected no navigation.");
    }

    [TestMethod]
    public async Task A_link_to_a_page_that_does_not_exist_must_fail_with_the_exception()
    {
        using var host = CreateHost();

        var response = await host.Root.NavigateToLinkAsync(this, new Uri("ecierge-console://Nope/x"));

        var failed = response as NavigationFailedResponse;
        Assert.IsNotNull(failed, "Expected a failure.");
        Assert.IsInstanceOfType<NestedSegmentNotFoundException>(failed.Error, "Expected the exception of the segment that was not found.");
        Assert.IsEmpty(host.Root.Requests, "Expected no navigation.");
    }

    [TestMethod]
    public async Task A_link_must_be_checked_by_the_rules_like_any_other_navigation()
    {
        using var host = CreateHost(services =>
        {
            services.RemoveAll<IAuthorizationService>();
            services.AddSingleton<IAuthorizationService>(new PredicateAuthorizationService(_ => false, "Sign in first"));
        });

        var response = await host.Root.NavigateToLinkAsync(this, new Uri("ecierge-console://Building/b1/Assets"));

        var denied = response as NavigationDeniedResponse;
        Assert.IsNotNull(denied, "Expected the rules to refuse the page of the link.");
        CollectionAssert.AreEqual(new[] { "Sign in first" }, denied.Reasons.ToArray(), "Expected the reason, so that the application can send the user to sign in.");
    }

    [TestMethod]
    public async Task A_link_to_a_dialog_must_open_the_dialog_over_its_page()
    {
        using var host = CreateHost();

        var response = await host.Root.NavigateToLinkAsync(this, new Uri("ecierge-console://Building/b1/Menu/%21AddAsset"));

        Assert.IsTrue(response.Success, "Expected the page and the dialog to open.");
        Assert.IsInstanceOfType<FakeDialogNavigator>(host.Root.LeafNavigator, "Expected the dialog to be the innermost region.");
    }

    [TestMethod]
    public async Task A_missing_argument_must_be_refused()
    {
        using var host = CreateHost();

        await Assert.ThrowsExactlyAsync<ArgumentNullException>(async () => await host.Root.NavigateToLinkAsync(this, null!), "Expected a missing link to be refused.");
    }
}
