#nullable enable

using Windows.Foundation;

namespace Ecierge.Uno.Controls;

public partial class LocationBreadcrumbBar : Control
{
    public object? ItemsSource
    {
        get => GetValue(ItemsSourceProperty);
        set => SetValue(ItemsSourceProperty, value);
    }

    public object? ItemTemplate
    {
        get => GetValue(ItemTemplateProperty);
        set => SetValue(ItemTemplateProperty, value);
    }

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
