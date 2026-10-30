namespace Ecierge.Uno.Navigation;

using Microsoft.Xaml.Interactivity;

/// <summary>
/// Goes back in the history of the window, for example from the click of a button.
/// </summary>
/// <example>
/// <code language="xml">
/// &lt;Button Content="Back" IsEnabled="{Binding (nav:Navigation.CanGoBack), ElementName=Root}"&gt;
///     &lt;interactivity:Interaction.Behaviors&gt;
///         &lt;core:EventTriggerBehavior EventName="Click"&gt;
///             &lt;nav:NavigateBackAction /&gt;
///         &lt;/core:EventTriggerBehavior&gt;
///     &lt;/interactivity:Interaction.Behaviors&gt;
/// &lt;/Button&gt;
/// </code>
/// </example>
public partial class NavigateBackAction : DependencyObject, IAction
{
    /// <inheritdoc />
    public object? Execute(object sender, object parameter)
    {
        if (sender is not FrameworkElement element) return null;
        return element.FindNavigationRegion().Navigator.GoBackAsync(sender).AsTask();
    }
}

/// <summary>
/// Goes forward in the history of the window, for example from the click of a button.
/// </summary>
public partial class NavigateForwardAction : DependencyObject, IAction
{
    /// <inheritdoc />
    public object? Execute(object sender, object parameter)
    {
        if (sender is not FrameworkElement element) return null;
        return element.FindNavigationRegion().Navigator.GoForwardAsync(sender).AsTask();
    }
}
