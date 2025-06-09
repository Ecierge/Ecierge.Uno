namespace Ecierge.Uno.Navigation;

public class NavigationRuleResult
{
    public bool IsAllowed { get; }
    public IReadOnlyList<string> Reasons { get; }

    private NavigationRuleResult(bool isAllowed, IReadOnlyList<string> reason)
    {
        IsAllowed = isAllowed;
        Reasons = reason;
    }

    public static NavigationRuleResult Allow() => new(true, []);
    public static NavigationRuleResult Deny(string reason) => new(false, [reason]);
    public static NavigationRuleResult Deny(IReadOnlyList<string> reasons) => new(false, reasons);
}
