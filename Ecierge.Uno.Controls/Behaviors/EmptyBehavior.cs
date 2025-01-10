using Microsoft.Xaml.Interactivity;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml;
using System.Linq;
using CommunityToolkit.WinUI;
using Microsoft.UI.Xaml.Controls.Primitives;
using Windows.UI;
using System.Collections.Specialized;
using Microsoft.UI.Xaml.Data;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace Ecierge.Uno.Controls.Behaviors;
public class EmptyBehavior : Behavior<ListViewItem>
{
    public static readonly DependencyProperty IsSelectedProperty =
    DependencyProperty.Register("IsSelected", typeof(bool), typeof(EmptyBehavior),
        new PropertyMetadata(false, (d, e) => ((EmptyBehavior)d).OnIsSelectedChanged(e.OldValue, e.NewValue)));

    private void OnIsSelectedChanged(object? oldValue, object? newValue)
    {
        if (this.AssociatedObject.FindDescendant<ListViewItemPresenter>() is { } selectedListViewItemPresenter &&
                 selectedListViewItemPresenter.FindDescendants().OfType<Border>().LastOrDefault() is Border selectionIndicator)
        {
            if (newValue is true)
            {
                selectionIndicator.Width = 64;
                selectionIndicator.BorderBrush = new SolidColorBrush(Color.FromArgb(255, 168, 113, 95));
                selectionIndicator.BorderThickness = new Thickness(3);
                selectionIndicator.HorizontalAlignment = HorizontalAlignment.Center;
                selectionIndicator.VerticalAlignment = VerticalAlignment.Bottom;
            }
            else
            {
                selectionIndicator.Width = 0;
                selectionIndicator.BorderThickness = new Thickness(0);
            }
        }
    }

    protected override void OnAttached()
    {
        base.OnAttached();

        var binding = new Binding
        {
            Source = AssociatedObject,
            Path = new PropertyPath("IsSelected"),
            Mode = BindingMode.OneWay
        };

        BindingOperations.SetBinding(this, IsSelectedProperty, binding);
    }

    protected override void OnDetaching()
    {
        base.OnDetaching();
        this.ClearValue(IsSelectedProperty);
    }

}
