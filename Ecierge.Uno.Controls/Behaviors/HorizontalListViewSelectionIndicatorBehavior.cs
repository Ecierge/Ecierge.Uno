using Microsoft.Xaml.Interactivity;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml;
using System.Linq;
using CommunityToolkit.WinUI;
using Microsoft.UI.Xaml.Controls.Primitives;
using Windows.UI;

namespace Ecierge.Uno.Controls.Behaviors
{
    public class HorizontalListViewSelectionIndicatorBehavior : Behavior<ListView>
    {
        private ListViewItem? previousSelectedItem;

        protected override void OnAttached()
        {
            base.OnAttached();
#if !HAS_UNO
            if (AssociatedObject is not null)
            {
                AssociatedObject.SelectionChanged += OnListViewSelectionChanged;
            }
#endif
        }

        protected override void OnDetaching()
        {
            base.OnDetaching();
#if !HAS_UNO
            if (AssociatedObject is not null)
            {
                AssociatedObject.SelectionChanged -= OnListViewSelectionChanged;
            }
#endif
        }

        private void OnListViewSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (sender is ListView listView &&
                listView.SelectedItem is var selectedItem &&
                listView.ContainerFromItem(selectedItem) is ListViewItem selectedListViewItem)
            {
                RemoveSelectionIndicator(previousSelectedItem);

                MemberListView_SelectionChanged(listView, e, selectedListViewItem);

                previousSelectedItem = selectedListViewItem;
            }
        }

        private void MemberListView_SelectionChanged(ListView listView, SelectionChangedEventArgs e, ListViewItem selectedListViewItem)
        {
            if (selectedListViewItem.FindDescendant<ListViewItemPresenter>() is { } selectedListViewItemPresenter &&
                selectedListViewItemPresenter.FindDescendants().OfType<Border>().LastOrDefault() is Border selectionIndicator)
            {
                selectionIndicator.Width = 64;
                selectionIndicator.BorderBrush = new SolidColorBrush((Color)Application.Current.Resources["SystemAccentColorLight2"]);
                selectionIndicator.BorderThickness = new Thickness(3);
                selectionIndicator.HorizontalAlignment = HorizontalAlignment.Center;
                selectionIndicator.VerticalAlignment = VerticalAlignment.Bottom;
            }
        }

        private void RemoveSelectionIndicator(ListViewItem? previousSelectedItem)
        {
            if (previousSelectedItem is not null)
            {
                if (previousSelectedItem.FindDescendant<ListViewItemPresenter>() is { } previousListViewItemPresenter &&
                    previousListViewItemPresenter.FindDescendants().OfType<Border>().LastOrDefault() is Border previousSelectionIndicator)
                {
                    previousSelectionIndicator.Width = 3;
                    previousSelectionIndicator.BorderThickness = new Thickness(0, 0, 0, 0);
                }
            }
        }
    }
}
