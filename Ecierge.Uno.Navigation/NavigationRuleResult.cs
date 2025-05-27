namespace Ecierge.Uno.Navigation;

public class NavigationRuleResult
{
    public bool IsAllowed { get; }
    public string Reason { get; }

    private NavigationRuleResult(bool isAllowed, string reason)
    {
        IsAllowed = isAllowed;
        Reason = reason;
    }

    public static NavigationRuleResult Allow() => new(true, string.Empty);
    public static NavigationRuleResult Deny(string reason) => new(false, reason);
}
