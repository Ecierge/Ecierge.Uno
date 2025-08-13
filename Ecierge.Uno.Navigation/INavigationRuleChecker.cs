namespace Ecierge.Uno.Navigation;

public interface INavigationRuleChecker
{
    ValueTask<NavigationRuleResult> CanNavigateAsync(string route);
}
