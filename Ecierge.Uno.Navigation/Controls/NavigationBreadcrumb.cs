namespace Ecierge.Uno.Navigation.Controls;

/// <summary>
/// A breadcrumb of the route that is shown: one level of the route, from the root down to the page.
/// </summary>
/// <remarks>
/// <para>
/// A named segment is a level. A named segment that owns a data segment is one level together with the value of
/// the data segment, and the breadcrumb shows the name of the entity, for example the name of a building rather than
/// "Building" followed by its id.
/// </para>
/// </remarks>
public sealed class NavigationBreadcrumb
{
    internal NavigationBreadcrumb(string title, Routing.Route route, bool isCurrent)
    {
        Title = title;
        Route = route;
        IsCurrent = isCurrent;
    }

    /// <summary>
    /// Gets the text that the user sees: the name of the entity of the level, else the title of its segment, else the
    /// name of its segment.
    /// </summary>
    public string Title { get; }

    /// <summary>
    /// Gets the route from the root to the level, with the data of its data segments.
    /// </summary>
    public Routing.Route Route { get; }

    /// <summary>
    /// Gets a value that tells whether the breadcrumb stands for the page that is shown.
    /// </summary>
    public bool IsCurrent { get; }

    /// <summary>
    /// Gives the text that the user sees, so that a breadcrumb bar shows it without a template.
    /// </summary>
    /// <returns>The <see cref="Title"/>.</returns>
    public override string ToString() => Title;
}
