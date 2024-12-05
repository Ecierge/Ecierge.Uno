#nullable enable

using Windows.Foundation;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace Ecierge.Uno.Controls.LocationBreadcrumb;
public partial class LocationBreadcrumbBar : Control
{
    public object? ItemsSource
    {
        get => GetValue(ItemsSourceProperty);
        set => SetValue(ItemsSourceProperty, value);
    }

    /// <summary>
    /// Identifies the ItemsSource depe
    /// ndency property.
    /// </summary>

    /// <summary>
    /// Gets or sets the data template for the BreadcrumbBarItem.
    /// </summary>
    public object? ItemTemplate
    {
        get => GetValue(ItemTemplateProperty);
        set => SetValue(ItemTemplateProperty, value);
    }

    /// <summary>
    /// Identifies the ItemTemplate dependency property.
    /// </summary>
    ///

    public static DependencyProperty ItemsSourceProperty { get; } =
        DependencyProperty.Register(
            nameof(ItemsSource),
            typeof(object),
            typeof(LocationBreadcrumbBar),
            new PropertyMetadata(null, OnPropertyChanged));
    public static DependencyProperty ItemTemplateProperty { get; } =
        DependencyProperty.Register(
            nameof(ItemTemplate),
            typeof(object),
            typeof(LocationBreadcrumbBar),
            new PropertyMetadata(null, OnPropertyChanged));


    private static void OnPropertyChanged(DependencyObject sender, DependencyPropertyChangedEventArgs args)
    {
        var owner = (LocationBreadcrumbBar)sender;
        owner.OnPropertyChanged(args);
    }

    /// <summary>
    /// Occurs when an item is clicked in the BreadcrumbBar.
    /// </summary>
    public event TypedEventHandler<LocationBreadcrumbBar, LocationBreadcrumbBarItemClickedEventArgs>? ItemClicked;
}
