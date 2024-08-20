namespace Ecierge.Uno.Navigation.Regions;

using Microsoft.Extensions.DependencyInjection;

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
