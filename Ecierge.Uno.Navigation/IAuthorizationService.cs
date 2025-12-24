using System.Security;

namespace Ecierge.Uno.Navigation;

public interface IAuthorizationService
{
    ValueTask<NavigationRuleResult> CanNavigateAsync(Routing.Route route);

    ValueTask<bool> HasPermissionAsync(Routing.Route route);
}
