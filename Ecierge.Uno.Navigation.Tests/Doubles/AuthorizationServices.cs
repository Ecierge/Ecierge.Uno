namespace Ecierge.Uno.Navigation.Tests.Doubles;

using Ecierge.Uno.Navigation;

/// <summary>
/// Allows every route.
/// </summary>
internal sealed class AllowAllAuthorizationService : IAuthorizationService
{
    public ValueTask<NavigationRuleResult> CanNavigateAsync(Routing.Route route)
     => ValueTask.FromResult(NavigationRuleResult.Allow());

    public ValueTask<bool> HasPermissionAsync(string permissionName) => ValueTask.FromResult(true);
}

/// <summary>
/// Allows the routes that the predicate accepts and denies the others with the given reason.
/// </summary>
internal sealed class PredicateAuthorizationService(Func<Routing.Route, bool> isAllowed, string reason = "Not allowed")
    : IAuthorizationService
{
    public ValueTask<NavigationRuleResult> CanNavigateAsync(Routing.Route route)
     => ValueTask.FromResult(isAllowed(route) ? NavigationRuleResult.Allow() : NavigationRuleResult.Deny(reason));

    public ValueTask<bool> HasPermissionAsync(string permissionName) => ValueTask.FromResult(true);
}

/// <summary>
/// Throws on every check, like a rule checker that crashes.
/// </summary>
internal sealed class CrashingAuthorizationService : IAuthorizationService
{
    public ValueTask<NavigationRuleResult> CanNavigateAsync(Routing.Route route)
     => throw new InvalidOperationException("The rule checker crashed");

    public ValueTask<bool> HasPermissionAsync(string permissionName) => throw new InvalidOperationException("The rule checker crashed");
}
