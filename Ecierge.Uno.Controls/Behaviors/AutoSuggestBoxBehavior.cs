namespace Ecierge.Uno.Behaviors;

using Microsoft.Xaml.Interactivity;

/// <summary>
/// Behavior for AutoSuggestBox to execute commands on query submitted and suggestion chosen.
/// </summary>
public class AutoSuggestBoxBehavior : Behavior<AutoSuggestBox>
{
    #region Text

    /// <summary>
    /// Text Dependency Property
    /// </summary>
    public static readonly DependencyProperty TextProperty =
        DependencyProperty.Register(nameof(Text), typeof(string), typeof(AutoSuggestBoxBehavior), new((string?)null, OnTextChanged));

    /// <summary>
    /// Gets or sets the Text property. This dependency property
    /// indicates then AuthoSuggestBox text.
    /// </summary>
    public string? Text
    {
        get => (string?)GetValue(TextProperty);
        set => SetValue(TextProperty, value);
    }

    /// <summary>
    /// Handles changes to the Text property.
    /// </summary>
    private static void OnTextChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        AutoSuggestBoxBehavior target = (AutoSuggestBoxBehavior)d;
        //string oldText = (string)e.OldValue;
        string newText = (string)e.NewValue;
        AutoSuggestBox autoSuggestBox = target.AssociatedObject;
        if (autoSuggestBox.Text != newText)
            autoSuggestBox.Text = newText;
        //target.OnTextChanged(oldText, newText);
    }

    ///// <summary>
    ///// Provides derived classes an opportunity to handle changes to the Text property.
    ///// </summary>
    //protected virtual void OnTextChanged(string oldText, string newText)
    //{
    //}

    #endregion Text

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
        this.AssociatedObject.TextChanged += AssociatedObject_TextChanged;
        this.AssociatedObject.QuerySubmitted += AssociatedObject_QuerySubmitted;
        this.AssociatedObject.SuggestionChosen += AssociatedObject_SuggestionChosen;
    }

    /// <inheritdoc />
    protected override void OnDetaching()
    {
        base.OnDetaching();
        this.AssociatedObject.TextChanged -= AssociatedObject_TextChanged;
        this.AssociatedObject.QuerySubmitted -= AssociatedObject_QuerySubmitted;
        this.AssociatedObject.SuggestionChosen -= AssociatedObject_SuggestionChosen;
    }

    private void AssociatedObject_TextChanged(AutoSuggestBox sender, AutoSuggestBoxTextChangedEventArgs args)
    {
        if (args.Reason == AutoSuggestionBoxTextChangeReason.UserInput)
        {
            Text = sender.Text;
        }
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
