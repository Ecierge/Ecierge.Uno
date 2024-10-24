namespace Ecierge.Uno.Behaviors;

using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.Xaml.Interactivity;

public abstract partial class FlyoutActionBase : DependencyObject
{
    #region Flyout

    /// <summary>
    /// Flyout Dependency Property
    /// </summary>
    public static readonly DependencyProperty FlyoutProperty =
        DependencyProperty.Register(nameof(Flyout), typeof(FlyoutBase), typeof(ShowFlyoutAction), new((FlyoutBase?)null));

    /// <summary>
    /// Gets or sets the Flyout property. This dependency property
    /// indicates the flyout to open.
    /// </summary>
    public FlyoutBase? Flyout
    {
        get => (FlyoutBase?)GetValue(FlyoutProperty);
        set => SetValue(FlyoutProperty, value);
    }

    #endregion Flyout

    #region Target

    /// <summary>
    /// Target Dependency Property
    /// </summary>
    public static readonly DependencyProperty TargetProperty =
        DependencyProperty.Register(nameof(Target), typeof(FrameworkElement), typeof(FlyoutActionBase), new ((FrameworkElement?)null));

    /// <summary>
    /// Gets or sets the Target property. This dependency property
    /// indicates the target to show Flyout at.
    /// </summary>
    public FrameworkElement? Target
    {
        get => (FrameworkElement?)GetValue(TargetProperty);
        set => SetValue(TargetProperty, value);
    }

    #endregion
}

public partial class ShowFlyoutAction : FlyoutActionBase, IAction
{
    public object? Execute(object sender, object parameter)
    {
        if (Flyout is Flyout flyout
            && flyout.Content is FrameworkElement element
            && sender is FrameworkElement s)
            element.DataContext = s.DataContext;
        if (Flyout is not null)
            Flyout.DispatcherQueue.TryEnqueue(() =>
            {
                if (Target is not null)
                    Flyout.ShowAt(Target);
                else
                    Flyout.ShowAt(sender as FrameworkElement);
            });

        return null;
    }
}

public partial class HideFlyoutAction : FlyoutActionBase, IAction
{
    public object? Execute(object sender, object parameter)
    {
        if (Flyout is not null)
            Flyout.Hide();

        return null;
    }
}
