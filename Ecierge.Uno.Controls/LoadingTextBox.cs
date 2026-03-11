namespace Ecierge.Uno.Controls;

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
public sealed partial class LoadingTextBox : TextBox
{
    public LoadingTextBox()
    {
        DefaultStyleKey = typeof(LoadingTextBox);
    }

    public static readonly DependencyProperty IsLoadingProperty =
        DependencyProperty.Register(nameof(IsLoading), typeof(bool), typeof(LoadingTextBox), new (false));

    public bool IsLoading
    {
        get => (bool)GetValue(IsLoadingProperty);
        set => SetValue(IsLoadingProperty, value);
    }
}
