namespace Ecierge.Uno.Navigation.Tests;

using Ecierge.Uno.Navigation;
using Ecierge.Uno.Navigation.Tests.Doubles;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

/// <summary>
/// How the navigation rules decide: one service that allows a route is enough, a route is refused when every service
/// that answers refuses it, and a service that crashes does not decide.
/// </summary>
[TestClass, UnitTestCategory, NavigationTestCategory]
public class NavigationRulesTests
{
    private static NavigationTestHost HostWith(params IAuthorizationService[] services)
     => TestRoutes.CreateHost(
            new BuildingServer(),
            collection =>
            {
                collection.RemoveAll<IAuthorizationService>();
                foreach (var service in services) collection.AddSingleton(service);
            });

    private static readonly IAuthorizationService Allow = new AllowAllAuthorizationService();

    private static IAuthorizationService Deny(string reason) => new PredicateAuthorizationService(_ => false, reason);

    private static readonly IAuthorizationService Crash = new CrashingAuthorizationService();

    [TestMethod]
    public async Task A_route_must_be_allowed_when_one_service_allows_it_and_another_denies_it()
    {
        using var host = HostWith(Deny("No access"), Allow);

        var result = await host.Root.IsAllowedToNavigateAsync(host.Root.ParseRoute("Login"));

        Assert.IsTrue(result.IsAllowed, "Expected one service that allows the route to be enough.");
    }

    [TestMethod]
    public async Task A_route_must_be_denied_with_all_the_reasons_when_every_service_denies_it()
    {
        using var host = HostWith(Deny("No role"), Deny("No license"));

        var result = await host.Root.IsAllowedToNavigateAsync(host.Root.ParseRoute("Login"));

        Assert.IsFalse(result.IsAllowed, "Expected the route to be denied.");
        CollectionAssert.AreEqual(new[] { "No role", "No license" }, result.Reasons.ToArray(), "Expected the user to learn every reason.");
    }

    [TestMethod]
    public async Task A_service_that_crashes_must_not_stop_the_others_from_allowing()
    {
        using var host = HostWith(Crash, Allow);

        var result = await host.Root.IsAllowedToNavigateAsync(host.Root.ParseRoute("Login"));

        Assert.IsTrue(result.IsAllowed, "Expected the service that works to decide.");
    }

    [TestMethod]
    public async Task A_route_must_be_denied_when_every_service_crashes()
    {
        using var host = HostWith(Crash);

        var result = await host.Root.IsAllowedToNavigateAsync(host.Root.ParseRoute("Login"));

        Assert.IsFalse(result.IsAllowed, "Expected a route nobody could check not to be allowed.");
        CollectionAssert.AreEqual(new[] { "All rule checkers crashed" }, result.Reasons.ToArray(), "Expected the reason to tell that the checks failed.");
    }

    [TestMethod]
    public async Task A_route_must_be_denied_when_no_service_checks_it()
    {
        using var host = HostWith();

        var result = await host.Root.IsAllowedToNavigateAsync(host.Root.ParseRoute("Login"));

        Assert.IsFalse(result.IsAllowed, "Expected a window without rules to refuse everything, as nothing allowed the route.");
    }

    [TestMethod]
    public async Task CanNavigate_must_tell_whether_the_rules_allow_the_route()
    {
        using var host = HostWith(Deny("No access"));

        Assert.IsFalse(await host.Root.CanNavigate(host.Root.ParseRoute("Login")), "Expected the denial to be reported as false.");
    }

    [TestMethod]
    public async Task A_denied_navigation_must_carry_the_reasons_and_not_reach_any_navigator()
    {
        using var host = HostWith(Deny("No access"));

        var response = await host.Root.NavigateRouteAsync(this, "Building/b1/Menu");

        var denied = response as NavigationDeniedResponse;
        Assert.IsNotNull(denied, "Expected the response to tell that the rules refused the route.");
        CollectionAssert.AreEqual(new[] { "No access" }, denied.Reasons.ToArray(), "Expected the reasons of the rules.");
        CollectionAssert.AreEqual(new[] { "No access" }, denied.Errors.ToArray(), "Expected the reasons to be listed as the errors of the failure too.");
        Assert.IsEmpty(host.Root.Requests, "Expected no navigator to be asked to show a page that is refused.");
        Assert.IsNull(denied.Error, "Expected a refusal not to be an exception.");
    }

    [TestMethod]
    public async Task Retry_of_a_denied_navigation_must_check_the_rules_again()
    {
        var allowed = false;
        using var host = HostWith(new PredicateAuthorizationService(_ => allowed, "No access"));
        var denied = (NavigationFailedResponse)await host.Root.NavigateRouteAsync(this, "Login");
        allowed = true;

        var response = await denied.Retry!();

        Assert.IsTrue(response.Success, "Expected the page to open once the rules allow it, for example after the user signed in.");
    }

    [TestMethod]
    public async Task A_denied_navigation_must_raise_NavigationFailed_with_the_denied_response()
    {
        using var host = HostWith(Deny("No access"));
        NavigationFailedResponse? raised = null;
        host.Status.NavigationFailed += (_, response) => raised = response;

        await host.Root.NavigateRouteAsync(this, "Login");

        Assert.IsInstanceOfType<NavigationDeniedResponse>(raised, "Expected listeners to tell a refusal from a failure, to send the user to sign in instead of offering Retry.");
    }
}
