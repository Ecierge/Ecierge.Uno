namespace Ecierge.Uno.Behaviors;

using Microsoft.Xaml.Interactivity;

/// <summary>
/// Behavior for AutoSuggestBox to execute commands on query submitted and suggestion chosen.
/// </summary>
public class AutoSuggestBoxBehavior : Behavior<AutoSuggestBox>
{
    #region QuerySubmittedCommand

    /// <summary>
    /// QuerySubmittedCommand Dependency Property
    /// </summary>
    public static readonly DependencyProperty QuerySubmittedCommandProperty =
        DependencyProperty.Register(nameof(QuerySubmittedCommand), typeof(ICommand), typeof(AutoSuggestBoxBehavior),
            new PropertyMetadata((ICommand?)null));

    /// <summary>
    /// Gets or sets the QuerySubmittedCommand property. This dependency property
    /// indicates the command to execute when query submitted.
    /// </summary>
    public ICommand? QuerySubmittedCommand
    {
        get => (ICommand?)GetValue(QuerySubmittedCommandProperty);
        set => SetValue(QuerySubmittedCommandProperty, value);
    }

    #endregion QuerySubmittedCommand

    #region SuggestionChosenCommand

    /// <summary>
    /// SuggestionChosenCommand Dependency Property
    /// </summary>
    public static readonly DependencyProperty SuggestionChosenCommandProperty =
        DependencyProperty.Register(nameof(SuggestionChosenCommand), typeof(ICommand), typeof(AutoSuggestBoxBehavior),
            new PropertyMetadata((ICommand?)null));

    /// <summary>
    /// Gets or sets the SuggestionChosenCommand property. This dependency property
    /// indicates the command to execute when suggestion chosen.
    /// </summary>
    public ICommand? SuggestionChosenCommand
    {
        get => (ICommand?)GetValue(SuggestionChosenCommandProperty);
        set => SetValue(SuggestionChosenCommandProperty, value);
    }

    #endregion SuggestionChosenCommand

    /// <inheritdoc />
    protected override void OnAttached()
    {
        base.OnAttached();
        this.AssociatedObject.QuerySubmitted += AssociatedObject_QuerySubmitted;
        this.AssociatedObject.SuggestionChosen += AssociatedObject_SuggestionChosen;
    }

    /// <inheritdoc />
    protected override void OnDetaching()
    {
        base.OnDetaching();
        this.AssociatedObject.QuerySubmitted -= AssociatedObject_QuerySubmitted;
        this.AssociatedObject.SuggestionChosen -= AssociatedObject_SuggestionChosen;
    }

    private void AssociatedObject_QuerySubmitted(AutoSuggestBox sender, AutoSuggestBoxQuerySubmittedEventArgs args)
    {
        if (QuerySubmittedCommand != null && QuerySubmittedCommand.CanExecute(args.QueryText))
        {
            QuerySubmittedCommand.Execute(args.QueryText);
        }
    }

    private void AssociatedObject_SuggestionChosen(AutoSuggestBox sender, AutoSuggestBoxSuggestionChosenEventArgs args)
    {
        if (SuggestionChosenCommand != null && SuggestionChosenCommand.CanExecute(args.SelectedItem))
        {
            SuggestionChosenCommand.Execute(args.SelectedItem);
        }
    }
}
