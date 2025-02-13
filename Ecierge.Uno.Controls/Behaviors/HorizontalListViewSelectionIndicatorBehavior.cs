namespace Ecierge.Uno.Behaviors;

using System.Linq;
using CommunityToolkit.WinUI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.Xaml.Interactivity;
public class HorizontalListViewSelectionIndicatorBehavior : Behavior<ListView>
{
    private ListViewItem? previousSelectedItem;

    public static readonly DependencyProperty SelectorMarginProperty =
        DependencyProperty.Register(
            "SelectorMargin",
            typeof(Thickness),
            typeof(HorizontalListViewSelectionIndicatorBehavior),
            new PropertyMetadata(new Thickness(0))
        );

    public Thickness Margin
    {
        get => (Thickness)GetValue(SelectorMarginProperty);
        set => SetValue(SelectorMarginProperty, value);
    }

    public static readonly DependencyProperty SelectorWidthProperty =
        DependencyProperty.Register(
            "SelectorWidth",
            typeof(double),
            typeof(HorizontalListViewSelectionIndicatorBehavior),
            new PropertyMetadata(64.0)
        );

    public double Width
    {
        get => (double)GetValue(SelectorWidthProperty);
        set => SetValue(SelectorWidthProperty, value);
    }

    public static readonly DependencyProperty VerticalAlignmentProperty =
        DependencyProperty.Register(
            "SelectorVerticalAlignment",
            typeof(VerticalAlignment),
            typeof(HorizontalListViewSelectionIndicatorBehavior),
            new PropertyMetadata(VerticalAlignment.Bottom)
        );

    public VerticalAlignment VerticalAlignment
    {
        get => (VerticalAlignment)GetValue(VerticalAlignmentProperty);
        set => SetValue(VerticalAlignmentProperty, value);
    }

    public static readonly DependencyProperty HorizontalAlignmentProperty =
        DependencyProperty.Register(
            "SelectorHorizontalAlignment",
            typeof(HorizontalAlignment),
            typeof(HorizontalListViewSelectionIndicatorBehavior),
            new PropertyMetadata(HorizontalAlignment.Center)
        );

    public HorizontalAlignment HorizontalAlignment
    {
        get => (HorizontalAlignment)GetValue(HorizontalAlignmentProperty);
        set => SetValue(HorizontalAlignmentProperty, value);
    }

    public static readonly DependencyProperty BorderBrushProperty =
        DependencyProperty.Register(
            "SelectorBorderBrush",
            typeof(Brush),
            typeof(HorizontalListViewSelectionIndicatorBehavior),
            new PropertyMetadata((Brush)Application.Current.Resources["AccentFillColorDefaultBrush"])
        );

    public Brush BorderBrush
    {
        get => (Brush)GetValue(BorderBrushProperty);
        set => SetValue(BorderBrushProperty, value);
    }

    public static readonly DependencyProperty BorderThicknessProperty =
        DependencyProperty.Register(
            "SelectorBorderThickness",
            typeof(Thickness),
            typeof(HorizontalListViewSelectionIndicatorBehavior),
            new PropertyMetadata(new Thickness(3))
        );

    public Thickness Thickness
    {
        get => (Thickness)GetValue(BorderThicknessProperty);
        set => SetValue(BorderThicknessProperty, value);
    }

#if !HAS_UNO
    protected override void OnAttached()
    {
        base.OnAttached();
        if (AssociatedObject is not null)
        {
            AssociatedObject.SelectionChanged += OnListViewSelectionChanged;
            if (AssociatedObject.SelectedIndex >= 0)
            {
                AssociatedObject.Loaded += OnListViewLoaded;
            }
        }
    }

    protected override void OnDetaching()
    {
        base.OnDetaching();
        if (AssociatedObject is not null)
        {
            AssociatedObject.SelectionChanged -= OnListViewSelectionChanged;
        }
    }
#endif

    private void OnListViewLoaded(object sender, RoutedEventArgs e)
    {
        AssociatedObject.Loaded -= OnListViewLoaded;
        var args = new SelectionChangedEventArgs([], [AssociatedObject.SelectedItem]);
        OnListViewSelectionChanged(sender, args);
    }

    private void OnListViewSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (sender is ListView listView &&
            listView.SelectedItem is var selectedItem &&
            listView.ContainerFromItem(selectedItem) is ListViewItem selectedListViewItem)
        {
            RemoveSelectionIndicator(previousSelectedItem);
            SetSelectionIndicatorProperties(listView, e, selectedListViewItem);
            previousSelectedItem = selectedListViewItem;
        }
    }

    private void SetSelectionIndicatorProperties(ListView listView, SelectionChangedEventArgs e, ListViewItem selectedListViewItem)
    {
        if (selectedListViewItem.FindDescendant<ListViewItemPresenter>() is { } selectedListViewItemPresenter &&
            selectedListViewItemPresenter.FindDescendants().OfType<Border>().LastOrDefault() is Border selectionIndicator)
        {
            selectionIndicator.BorderBrush = BorderBrush;
            selectionIndicator.BorderThickness = Thickness;
            selectionIndicator.HorizontalAlignment = HorizontalAlignment;
            selectionIndicator.VerticalAlignment = VerticalAlignment;
            selectionIndicator.Width = Width;
            selectionIndicator.Margin = Margin;
        }
    }

    private void RemoveSelectionIndicator(ListViewItem? previousSelectedItem)
    {
        if (previousSelectedItem is not null)
        {
            if (previousSelectedItem.FindDescendant<ListViewItemPresenter>() is { } previousListViewItemPresenter &&
                previousListViewItemPresenter.FindDescendants().OfType<Border>().LastOrDefault() is Border previousSelectionIndicator)
            {
                previousSelectionIndicator.BorderThickness = new Thickness(0);
            }
        }
    }
}
