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

    public NavigationRegion(NavigationScope scope)
    {
        Scope = scope;
        Navigator.Region = this;
    }
}
