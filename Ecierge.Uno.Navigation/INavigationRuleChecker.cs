namespace Ecierge.Uno.Navigation;

public interface INavigationRuleChecker
{
    ValueTask<NavigationRuleResult> CanNavigateAsync(Routing.Route route);
}
