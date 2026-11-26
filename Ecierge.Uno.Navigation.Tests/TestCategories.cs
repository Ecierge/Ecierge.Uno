namespace Ecierge.Uno.Navigation.Tests;

/// <summary>
/// Categorizes a test that runs in process, without a user interface, a browser or a server.
/// </summary>
public sealed class UnitTestCategoryAttribute : TestCategoryBaseAttribute
{
    /// <inheritdoc />
    public override IList<string> TestCategories => ["Unit"];
}

/// <summary>
/// Categorizes a test of the navigation library.
/// </summary>
public sealed class NavigationTestCategoryAttribute : TestCategoryBaseAttribute
{
    /// <inheritdoc />
    public override IList<string> TestCategories => ["Navigation"];
}
