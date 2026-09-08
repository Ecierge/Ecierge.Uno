namespace Ecierge.Uno.Behaviors;
using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Xaml.Interactivity;
using CommunityToolkit.WinUI.Controls;

/// <summary>
/// Behavior for TokenizingTextBox to execute commands on query submitted and suggestion chosen.
/// </summary>
public class TokenizingTextBoxBehavior : Behavior<TokenizingTextBox>
{
    #region IsReadOnly

    /// <summary>
    /// IsReadOnly Dependency Property
    /// </summary>
    public static readonly DependencyProperty IsReadOnlyProperty =
        DependencyProperty.Register(nameof(IsReadOnly), typeof(bool), typeof(TokenizingTextBoxBehavior),
            new PropertyMetadata(false, OnIsReadOnlyChanged));

    /// <summary>
    /// Gets or sets the IsReadOnly property. This dependency property
    /// indicates whether the TokenizingTextBox is in read-only mode.
    /// </summary>
    public bool IsReadOnly
    {
        get => (bool)GetValue(IsReadOnlyProperty);
        set => SetValue(IsReadOnlyProperty, value);
    }

    /// <summary>
    /// Handles changes to the IsReadOnly property.
    /// </summary>
    private static void OnIsReadOnlyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        TokenizingTextBoxBehavior target = (TokenizingTextBoxBehavior)d;
        bool isReadOnly = (bool)e.NewValue;
        target.ApplyReadOnlyState(isReadOnly);
    }

    /// <summary>
    /// Applies the read-only state to the TokenizingTextBox and its template elements.
    /// </summary>
    private void ApplyReadOnlyState(bool isReadOnly)
    {
        if (AssociatedObject is null) return;

        // Control hit testing and tab stop
        AssociatedObject.IsHitTestVisible = !isReadOnly;
        AssociatedObject.IsTabStop = !isReadOnly;
    }

    /// <summary>
    /// Finds a descendant element by name using VisualTreeHelper.
    /// </summary>
    private static DependencyObject? FindDescendantByName(DependencyObject parent, string name)
    {
        int childCount = VisualTreeHelper.GetChildrenCount(parent);
        for (int i = 0; i < childCount; i++)
        {
            var child = VisualTreeHelper.GetChild(parent, i);
            if (child is FrameworkElement fe && fe.Name == name)
            {
                return child;
            }

            var result = FindDescendantByName(child, name);
            if (result is not null)
            {
                return result;
            }
        }

        return null;
    }

    #endregion IsReadOnly

    #region Text

    /// <summary>
    /// Text Dependency Property
    /// </summary>
    public static readonly DependencyProperty TextProperty =
        DependencyProperty.Register(nameof(Text), typeof(string), typeof(TokenizingTextBoxBehavior),
            new((string?)null, OnTextChanged));

    /// <summary>
    /// Gets or sets the Text property. This dependency property
    /// indicates the text currently typed into the <see cref="TokenizingTextBox"/>.
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
        TokenizingTextBoxBehavior target = (TokenizingTextBoxBehavior)d;
        string newText = (string)e.NewValue;
        TokenizingTextBox tokenizingTextBox = target.AssociatedObject;
        // Guard against the control-to-binding-to-control loop: assigning the same text back
        // would raise TextChanged again.
        if (tokenizingTextBox.Text != newText)
            tokenizingTextBox.Text = newText;
    }

    #endregion Text

    #region QuerySubmittedCommand

    /// <summary>
    /// QuerySubmittedCommand Dependency Property
    /// </summary>
    public static readonly DependencyProperty QuerySubmittedCommandProperty =
        DependencyProperty.Register(nameof(QuerySubmittedCommand), typeof(ICommand), typeof(TokenizingTextBoxBehavior),
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
        DependencyProperty.Register(nameof(SuggestionChosenCommand), typeof(ICommand), typeof(TokenizingTextBoxBehavior),
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
        this.AssociatedObject.Loaded += AssociatedObject_Loaded;
    }

    private void AssociatedObject_Loaded(object sender, RoutedEventArgs e)
    {
        ApplyReadOnlyState(IsReadOnly);
    }

    /// <inheritdoc />
    protected override void OnDetaching()
    {
        base.OnDetaching();
        this.AssociatedObject.TextChanged -= AssociatedObject_TextChanged;
        this.AssociatedObject.QuerySubmitted -= AssociatedObject_QuerySubmitted;
        this.AssociatedObject.SuggestionChosen -= AssociatedObject_SuggestionChosen;
        this.AssociatedObject.Loaded -= AssociatedObject_Loaded;
    }

    private void AssociatedObject_TextChanged(AutoSuggestBox sender, AutoSuggestBoxTextChangedEventArgs args)
    {
        // Committing a token clears the text programmatically; only user input is pushed back,
        // so the view model resets its own filter when a token is added.
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

