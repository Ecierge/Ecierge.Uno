namespace Ecierge.Uno.Navigation;

public interface INavigationRuleChecker
{
    Task<NavigationRuleResult> CanNavigateAsync(string route);
}
