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

#if !HAS_UNO
    protected override void OnAttached()
    {
        base.OnAttached();
        if (AssociatedObject is not null)
        {
            AssociatedObject.SelectionChanged += OnListViewSelectionChanged;
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
            selectionIndicator.BorderBrush = (Brush)Application.Current.Resources["AccentFillColorDefaultBrush"];
            selectionIndicator.BorderThickness = new Thickness(3);
            selectionIndicator.HorizontalAlignment = HorizontalAlignment.Center;
            selectionIndicator.VerticalAlignment = VerticalAlignment.Bottom;
            selectionIndicator.Width = 64;
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
