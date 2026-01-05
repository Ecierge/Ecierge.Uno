namespace Ecierge.Uno.Navigation;

/// <summary>
/// Represents the result of a navigation rule evaluation, indicating whether navigation is allowed and providing
/// reasons for the decision.
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
    /// Creates a new instance of <see cref="NavigationRuleResult"/> indicating that navigation is denied with a
    /// specific reason.
    /// </summary>
    /// <param name="reason">The reason for denying navigation.</param>
    /// <returns>Deny navigation rule result.</returns>
    public static NavigationRuleResult Deny(string reason) => new(false, [ reason ]);

    /// <summary>
    /// Creates a new instance of <see cref="NavigationRuleResult"/> indicating that navigation is denied with multiple
    /// reasons.
    /// </summary>
    /// <param name="reasons">The reasons for denying navigation.</param>
    /// <returns>Deny navigation rule result.</returns>
    public static NavigationRuleResult Deny(IReadOnlyList<string> reasons) => new(false, reasons);

    /// <summary>
    /// Merges multiple navigation rule results into a single result. Navigation is allowed only if all results allow it;
    /// otherwise, all reasons from denied results are combined.
    /// </summary>
    /// <param name="results">The collection of navigation rule results to merge.</param>
    /// <returns>A merged navigation rule result that is allowed if all input results are allowed, or denied with all combined reasons otherwise.</returns>
    public static NavigationRuleResult Merge(IReadOnlyList<NavigationRuleResult> results) => results.All(
            r => r.IsAllowed)
        ? Allow()
        : Deny(results.SelectMany(r => r.Reasons).ToList());
}
