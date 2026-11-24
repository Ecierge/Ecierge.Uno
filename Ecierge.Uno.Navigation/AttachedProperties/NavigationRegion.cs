namespace Ecierge.Uno.Navigation.Regions;

using System.Diagnostics;

using Microsoft.Extensions.DependencyInjection;

[DebuggerDisplay("{Segment.Name}")]
public record NavigationRegion
{
    public NavigationScope Scope { get; private set; }
    public NameSegment Segment => Scope.Segment;
    public Navigator Navigator => Scope.ServiceProvider.GetRequiredService<Navigator>();
    public NavigationRegion? Parent { get; internal set; }
    public FrameworkElement? Target { get; internal set; }
    public FrameworkElement? Root { get; internal set; }

    /// <summary>
    /// Gets the ancestors of <see cref="Target"/> below <see cref="Root"/> that were marked with this region when it was attached, so that detaching it can remove the marks.
    /// </summary>
    internal FrameworkElement[] MarkedAncestors { get; set; } = [];

    public NavigationRegion(NavigationScope scope)
    {
        Scope = scope;
        Navigator.Region = this;
    }
}
