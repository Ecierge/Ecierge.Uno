namespace Ecierge.Uno.App;

using System.Threading.Tasks;

using Ecierge.Uno.Navigation;

internal sealed class DemoAuthorizationService : IAuthorizationService
{
    public ValueTask<NavigationRuleResult> CanNavigateAsync(Uno.Navigation.Routing.Route route)
    {
        return ValueTask.FromResult(NavigationRuleResult.Allow());
    }

    public ValueTask<bool> HasPermissionAsync(string permissionName)
    {
        return ValueTask.FromResult(true);
    }
}
