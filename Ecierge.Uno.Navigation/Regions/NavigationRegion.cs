namespace Ecierge.Uno.Navigation.Regions;

using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;

using Microsoft.Extensions.DependencyInjection;

using Ecierge.Uno.Navigation.Routing;

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

public static class Region
{
    #region NavigationInfo
    public static readonly DependencyProperty NavigationInfoProperty =
        DependencyProperty.RegisterAttached("NavigationInfo", typeof(NavigationRegion), typeof(Region), new(null));

    internal static void SetNavigationRegion([NotNull] this FrameworkElement element, NavigationRegion navigationRegion) => element.SetValue(Region.NavigationInfoProperty, navigationRegion);

    internal static NavigationRegion? GetNavigationRegion([NotNull] this FrameworkElement element) => (NavigationRegion?)element.GetValue(Region.NavigationInfoProperty);

    internal static NavigationRegion FindNavigationRegion([NotNull] this FrameworkElement element)
    {
        var regionElement = element;
        var navigationRegion = element.GetNavigationRegion();
        while (navigationRegion is null && regionElement.Parent is not null)
        {
            regionElement = (FrameworkElement)regionElement.Parent;
            navigationRegion = regionElement.GetNavigationRegion();
        }
        if (navigationRegion is null)
        {
            throw new RootNavigationRegionMissingException();
        }
        return navigationRegion;
    }

    public static void SetSegment([NotNull] this FrameworkElement element, string value, FrameworkElement root)
    {
        var parentNavigationRegion =
            root.GetNavigationRegion() ??
            root.FindParentNavigationRegion() ??
            throw new RootNavigationRegionMissingException();
        var parentSegment = parentNavigationRegion.Segment;
        ImmutableArray<NameSegment> nestedSegments;
        string parentSegmentName;
        if (parentSegment.Data is DataSegment dataSegment)
        {
            parentSegmentName = dataSegment.Name;
            nestedSegments = dataSegment.Nested;
        }
        else
        {
            parentSegmentName = parentSegment.Name;
            nestedSegments = parentSegment.Nested;
        }

        var segment = nestedSegments.FirstOrDefault(s => s.Name == value);
        if (segment is null)
        {
            throw new NestedSegmentMissingException(value, parentSegmentName);
        }

#pragma warning disable CA2000 // Dispose objects before losing scope
        var scope = parentNavigationRegion.Scope.CreateScope(segment, element, parentNavigationRegion.Navigator);
#pragma warning restore CA2000 // Dispose objects before losing scope
        var navigationRegion = new NavigationRegion(scope)
        {
            Parent = parentNavigationRegion,
            Target = element,
            Root = root
        };
        element.SetNavigationRegion(navigationRegion);
    }

    #endregion NavigationInfo
}
