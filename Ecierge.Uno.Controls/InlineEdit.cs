namespace Ecierge.Uno.Controls;

using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;

[TemplatePart(Name = PART_EditContentPresenter, Type = typeof(ContentPresenter))]
public sealed partial class InlineEdit : Control
{
    private const string PART_EditContentPresenter = "PART_EditContentPresenter";

    #region IsEditing

    /// <summary>
    /// IsEditing Dependency Property
    /// </summary>
    public static readonly DependencyProperty IsEditingProperty =
        DependencyProperty.Register(nameof(IsEditing), typeof(bool), typeof(InlineEdit),
            new((bool)false,
                new PropertyChangedCallback(OnIsEditingChanged)));

    /// <summary>
    /// Gets or sets the IsEditing property. This dependency property
    /// indicates whether the control is in edit mode.
    /// </summary>
    public bool IsEditing
    {
        get => (bool)GetValue(IsEditingProperty);
        set => SetValue(IsEditingProperty, value);
    }

    /// <summary>
    /// Handles changes to the IsEditing property.
    /// </summary>
    private static void OnIsEditingChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        InlineEdit target = (InlineEdit)d;
        bool oldIsEditing = (bool)e.OldValue;
        bool newIsEditing = (bool)e.NewValue;
        target.OnIsEditingChanged(oldIsEditing, newIsEditing);
    }

    /// <summary>
    /// Provides derived classes an opportunity to handle changes to the IsEditing property.
    /// </summary>
    private void OnIsEditingChanged(bool oldIsEditing, bool newIsEditing)
    {
        if (newIsEditing)
        {
            if (GetTemplateChild(PART_EditContentPresenter) is ContentPresenter presenter)
            {
                DispatcherQueue.TryEnqueue(DispatcherQueuePriority.Low, () =>
                {
                    if (VisualTreeHelper.GetChildrenCount(presenter) > 0)
                    {
                        var child = VisualTreeHelper.GetChild(presenter, 0);
                        (child as Control)?.Focus(FocusState.Programmatic);
                    }
                    else
                        presenter.Focus(FocusState.Programmatic);
                });
            }
            else
                this.Focus(FocusState.Programmatic);
        }
    }

    #endregion

    #region ViewCommand

    /// <summary>
    /// ViewCommand Dependency Property
    /// </summary>
    public static readonly DependencyProperty ViewCommandProperty =
        DependencyProperty.Register(nameof(ViewCommand), typeof(XamlUICommand), typeof(InlineEdit), new((XamlUICommand?)null));

    /// <summary>
    /// Gets or sets the ViewCommand property. This dependency property
    /// indicates the command visible in view mode.
    /// </summary>
    public XamlUICommand? ViewCommand
    {
        get => (XamlUICommand?)GetValue(ViewCommandProperty);
        set => SetValue(ViewCommandProperty, value);
    }

    #endregion ViewCommand

    #region PrimaryEditCommand

    /// <summary>
    /// PrimaryEditCommand Dependency Property
    /// </summary>
    public static readonly DependencyProperty PrimaryEditCommandProperty =
        DependencyProperty.Register(nameof(PrimaryEditCommand), typeof(XamlUICommand), typeof(InlineEdit), new((XamlUICommand?)null));

    /// <summary>
    /// Gets or sets the PrimaryEditCommand property. This dependency property
    /// indicates the primary command visible in edit mode.
    /// </summary>
    public XamlUICommand? PrimaryEditCommand
    {
        get => (XamlUICommand?)GetValue(PrimaryEditCommandProperty);
        set => SetValue(PrimaryEditCommandProperty, value);
    }

    #endregion PrimaryEditCommand

    #region SecondaryEditCommand

    /// <summary>
    /// SecondaryEditCommand Dependency Property
    /// </summary>
    public static readonly DependencyProperty SecondaryEditCommandProperty =
        DependencyProperty.Register(nameof(SecondaryEditCommand), typeof(XamlUICommand), typeof(InlineEdit), new((XamlUICommand?)null));

    /// <summary>
    /// Gets or sets the SecondaryEditCommand property. This dependency property
    /// indicates the secondary command visible in edit mode.
    /// </summary>
    public XamlUICommand? SecondaryEditCommand
    {
        get => (XamlUICommand?)GetValue(SecondaryEditCommandProperty);
        set => SetValue(SecondaryEditCommandProperty, value);
    }

    #endregion SecondaryEditCommand

    #region Content

    /// <summary>
    /// Identifies the Content dependency property.
    /// </summary>
    public static readonly DependencyProperty ContentProperty =
        DependencyProperty.Register(nameof(Content), typeof(object), typeof(InlineEdit), new(null));

    /// <summary>
    /// Gets or sets the content of a ContentControl.
    /// </summary>
    public object Content
    {
        get => (object)GetValue(ContentProperty);
        set { SetValue(ContentProperty, value); }
    }

    #endregion Content

    #region ViewContentTemplate

    /// <summary>
    /// Identifies the ViewContentTemplate dependency property
    /// </summary>
    public static readonly DependencyProperty ViewContentTemplateProperty =
        DependencyProperty.Register(nameof(ViewContentTemplate), typeof(DataTemplate), typeof(InlineEdit), new((DataTemplate?)null));

    /// <summary>
    /// Gets or sets the data template that is used to display the content of the ContentControl.
    /// </summary>
    public DataTemplate? ViewContentTemplate
    {
        get => (DataTemplate?)GetValue(ViewContentTemplateProperty);
        set => SetValue(ViewContentTemplateProperty, value);
    }

    #endregion ViewContentTemplate

    #region EditContentTemplate

    /// <summary>
    /// Identifies the EditContentTemplate dependency property
    /// </summary>
    public static readonly DependencyProperty EditContentTemplateProperty =
        DependencyProperty.Register(nameof(EditContentTemplate), typeof(DataTemplate), typeof(InlineEdit), new((DataTemplate?)null));

    /// <summary>
    /// Gets or sets the data template that is used to edit the content of the ContentControl.
    /// </summary>
    public DataTemplate? EditContentTemplate
    {
        get => (DataTemplate?)GetValue(EditContentTemplateProperty);
        set => SetValue(EditContentTemplateProperty, value);
    }

    #endregion EditContentTemplate

    #region ButtonsStyle

    /// <summary>
    /// ButtonsStyle Dependency Property
    /// </summary>
    public static readonly DependencyProperty ButtonsStyleProperty =
        DependencyProperty.Register(nameof(ButtonsStyle), typeof(Style), typeof(InlineEdit),
            //new ((Style?)null));
            new (Application.Current.Resources[typeof(Button)] as Style));

    /// <summary>
    /// Gets or sets the ButtonsStyle property. This dependency property
    /// indicates the style applied to all buttons.
    /// </summary>
    public Style ButtonsStyle
    {
        get => (Style)GetValue(ButtonsStyleProperty);
        set => SetValue(ButtonsStyleProperty, value);
    }

    #endregion ButtonsStyle

    public InlineEdit()
    {
        this.DefaultStyleKey = typeof(InlineEdit);
    }

    protected override void OnLostFocus(RoutedEventArgs e)
    {
        base.OnLostFocus(e);

        DispatcherQueue.TryEnqueue(DispatcherQueuePriority.Low, () =>
        {
            var focusedElement = FocusManager.GetFocusedElement(this.XamlRoot!) as DependencyObject;
            if (focusedElement is null || IsAnyChildFocused(this, focusedElement))
                return;
            IsEditing = false;
            PrimaryEditCommand?.Execute(null);

            static bool IsAnyChildFocused(DependencyObject parent, DependencyObject? focusedElement)
            {
                for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
                {
                    var child = VisualTreeHelper.GetChild(parent, i);
                    if (child == focusedElement)
                        return true;
                }

                for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
                {
                    var child = VisualTreeHelper.GetChild(parent, i);
                    if (IsAnyChildFocused(child, focusedElement))
                        return true;
                }

                return false;
            }
        });
    }
}
