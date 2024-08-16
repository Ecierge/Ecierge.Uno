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

    public static void SetSegment([NotNull] this FrameworkElement element, string value)
    {
        var parentNavigationRegion = element.FindParentNavigationRegion() ?? throw new RootNavigationRegionMissingException();
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
            Target = element.GetNavigationTarget() ?? element
        };
        element.SetNavigationRegion(navigationRegion);
    }

    #endregion NavigationInfo
}

public static class Route
{
    #region SegmentName

    /// <summary>
    /// SegmentName Attached Dependency Property
    /// </summary>
    public static readonly DependencyProperty SegmentNameProperty =
        DependencyProperty.RegisterAttached("SegmentName", typeof(string), typeof(Route),
            new PropertyMetadata((string?)null,
                new PropertyChangedCallback(OnSegmentNameChanged)));

    /// <summary>
    /// Handles changes to the SegmentName property.
    /// </summary>
    private static void OnSegmentNameChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        string oldSegmentName = (string)e.OldValue;
        string newSegmentName = (string)e.NewValue;
        if (d is FrameworkElement element)
        {
            element.Loaded += (e, args) =>
            {
                element.SetSegment(newSegmentName);
                element.Unloaded += (e, args) =>
                {
                    element.GetNavigationRegion()!.Scope!.Dispose();
                    element.SetValue(Region.NavigationInfoProperty, null);
                };
            };
        }
    }

    #endregion SegmentName

    #region NavigationTarget

    /// <summary>
    /// NavigationTarget Attached Dependency Property
    /// </summary>
    public static readonly DependencyProperty NavigationTargetProperty =
        DependencyProperty.RegisterAttached("NavigationTarget", typeof(FrameworkElement), typeof(Route), new((FrameworkElement?)null));

    /// <summary>
    /// Gets the NavigationTarget property. This dependency property
    /// indicates the navigation target.
    /// </summary>
    public static FrameworkElement GetNavigationTarget([NotNull] this FrameworkElement element) => (FrameworkElement)element.GetValue(NavigationTargetProperty);

    /// <summary>
    /// Sets the NavigationTarget property. This dependency property
    /// indicates the navigation target.
    /// </summary>
    public static void SetNavigationTarget([NotNull] this FrameworkElement element, FrameworkElement value) => element.SetValue(NavigationTargetProperty, value);

    #endregion NavigationTarget
}
