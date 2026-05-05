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

    /// <inheritdoc />
    protected override void OnAttached()
    {
        base.OnAttached();
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
        this.AssociatedObject.Loaded -= AssociatedObject_Loaded;
    }

}

