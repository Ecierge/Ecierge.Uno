namespace Ecierge.Uno.Controls;

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;


public sealed partial class LoadingTextBox : TextBox
{
    public LoadingTextBox()
    {
        DefaultStyleKey = typeof(LoadingTextBox);
    }

    public bool IsValidating
    {
        get => (bool)GetValue(IsValidatingProperty);
        set => SetValue(IsValidatingProperty, value);
    }

    public static readonly DependencyProperty IsValidatingProperty =
        DependencyProperty.Register(
            nameof(IsValidating),
            typeof(bool),
            typeof(LoadingTextBox),
            new PropertyMetadata(false));
}
