namespace Ecierge.Uno.Navigation;

/// <summary>
/// Represents the result of a navigation rule evaluation, indicating whether navigation is allowed and providing reasons for the decision.
/// </summary>
public class NavigationRuleResult
{
    /// <summary>
    /// Gets a value indicating whether the navigation is allowed.
    /// </summary>
    public bool IsAllowed { get; }
    /// <summary>
    /// Gets the reasons for the navigation decision.
    /// </summary>
    public IReadOnlyList<string> Reasons { get; }

    private NavigationRuleResult(bool isAllowed, IReadOnlyList<string> reason)
    {
        IsAllowed = isAllowed;
        Reasons = reason;
    }

    /// <summary>
    /// Creates a new instance of <see cref="NavigationRuleResult"/> indicating that navigation is allowed.
    /// </summary>
    /// <returns>Allow navigation rule result.</returns>
    public static NavigationRuleResult Allow() => new(true, []);
    /// <summary>
    /// Creates a new instance of <see cref="NavigationRuleResult"/> indicating that navigation is denied with a specific reason.
    /// </summary>
    /// <param name="reason">The reason for denying navigation.</param>
    /// <returns>Deny navigation rule result.</returns>
    public static NavigationRuleResult Deny(string reason) => new(false, [reason]);
    /// <summary>
    /// Creates a new instance of <see cref="NavigationRuleResult"/> indicating that navigation is denied with multiple reasons.
    /// </summary>
    /// <param name="reasons">The reasons for denying navigation.</param>
    /// <returns>Deny navigation rule result.</returns>
    public static NavigationRuleResult Deny(IReadOnlyList<string> reasons) => new(false, reasons);
}
