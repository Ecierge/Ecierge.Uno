namespace Ecierge.Uno.Controls;

using System;
using System.Linq;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Input;

[TemplatePart(Name = EditableText, Type = typeof(TextBox))]
[TemplatePart(Name = MainGrid, Type = typeof(Grid))]
[TemplatePart(Name = Popup, Type = typeof(Popup))]
[TemplatePart(Name = DropDownButton, Type = typeof(Button))]
[TemplatePart(Name = ContentPresenter, Type = typeof(ContentPresenter))]
[TemplatePart(Name = PlaceholderTextBlock, Type = typeof(TextBlock))]

public partial class GroupedComboBox : ListViewBase
{
    #region TemplatePartNames

    private const string EditableText = "EditableText";
    private const string MainGrid = "MainGrid";
    private const string Popup = "Popup";
    private const string DropDownButton = "DropDownButton";
    private const string ContentPresenter = "ContentPresenter";
    private const string PlaceholderTextBlock = "PlaceholderTextBlock";

    #endregion TemplatePartNames

    private Popup? popup;
    private Button? dropDownButton;
    private TextBox? textBox;
    private Grid? mainGrid;
    private ContentPresenter? contentPresenter;
    private TextBlock? placeholderTextBlock;

    private string placeholderTextCache = string.Empty;
    private string SelectedValueCache = string.Empty;
    private bool isDropDownOpenedOnce = false;
    private bool isKeyDown = false;

    private bool AreAllControlsUnfocused =>
        IsEditable
        && textBox is not null && textBox.FocusState == FocusState.Unfocused
        && dropDownButton is not null && dropDownButton.FocusState == FocusState.Unfocused
        && popup is not null && popup.FocusState == FocusState.Unfocused;

    #region IsDropDownOpen

    /// <summary>
    /// Identifies the IsDropDownOpen dependency property.
    /// </summary>
    public static readonly DependencyProperty IsDropDownOpenProperty =
        DependencyProperty.Register(nameof(IsDropDownOpen), typeof(bool), typeof(GroupedComboBox),
            new(false, new PropertyChangedCallback(OnIsDropDownOpenChanged)));

    /// <summary>
    /// Gets or sets a value that indicates whether the drop-down portion of the GroupedComboBox
    /// is currently open.
    /// </summary>
    public bool IsDropDownOpen
    {
        get => (bool)GetValue(IsDropDownOpenProperty);
        set => SetValue(IsDropDownOpenProperty, value);
    }

    /// <summary>
    /// Handles changes to the IsDropDownOpen property.
    /// </summary>
    private static void OnIsDropDownOpenChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        GroupedComboBox target = (GroupedComboBox)d;
        bool oldIsDropDownOpen = (bool)e.OldValue;
        bool newIsDropDownOpen = (bool)e.NewValue;
        target.OnIsDropDownOpenChanged(oldIsDropDownOpen, newIsDropDownOpen);
    }

    /// <summary>
    /// Provides derived classes an opportunity to handle changes to the IsDropDownOpen property.
    /// </summary>
    protected virtual void OnIsDropDownOpenChanged(bool oldIsDropDownOpen, bool newIsDropDownOpen)
    {
        if (newIsDropDownOpen)
        {
            isDropDownOpenedOnce = true;
        }
        if (popup is not null)
        {
            popup.IsOpen = newIsDropDownOpen;
            if (isKeyDown && !IsDropDownOpen)
            {
                popup.IsOpen = true;
                IsDropDownOpen = true;
            }
            if (IsEditable && textBox is not null && popup.FocusState != FocusState.Unfocused)
                textBox.Focus(FocusState.Programmatic);
        }
        else
        {
            if (contentPresenter is not null)
                contentPresenter.Focus(FocusState.Programmatic);
            VisualStateManager.GoToState(this, "Focused", true);
        }
    }

    #endregion IsDropDownOpen

    #region PlaceholderText

    /// <summary>
    /// Identifies the PlaceholderText dependency property.
    /// </summary>
    public static readonly DependencyProperty PlaceholderTextProperty =
        DependencyProperty.Register(nameof(PlaceholderText), typeof(string), typeof(GroupedComboBox), new(string.Empty));

    /// <summary>
    /// Gets or sets the text that is displayed in the control until the value is changed
    /// by a user action or some other operation.
    /// </summary>
    public string PlaceholderText
    {
        get => (string)GetValue(PlaceholderTextProperty);
        set => SetValue(PlaceholderTextProperty, value);
    }

    #endregion PlaceholderText

    #region TextBoxStyle

    /// <summary>
    /// Identifies the TextBoxStyle dependency property.
    /// </summary>
    public static readonly DependencyProperty TextBoxStyleProperty =
        DependencyProperty.Register(nameof(TextBoxStyle), typeof(Style), typeof(GroupedComboBox), new((Style?)null));

    /// <summary>
    /// Gets or sets the style of the TextBox in the ComboBox when the ComboBox is editable.
    /// </summary>
    public Style? TextBoxStyle
    {
        get => (Style?)GetValue(TextBoxStyleProperty);
        set => SetValue(TextBoxStyleProperty, value);
    }

    #endregion TextBoxStyle

    #region IsEditable

    /// <summary>
    /// Identifies the IsEditable dependency property.
    /// </summary>
    public static readonly DependencyProperty IsEditableProperty =
     DependencyProperty.Register(
         nameof(IsEditable),
         typeof(bool),
         typeof(GroupedComboBox),
         new PropertyMetadata(true, OnIsEditableChanged));

    private static void OnIsEditableChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var control = (GroupedComboBox)d;
        control.OnIsEditableChanged((bool)e.OldValue, (bool)e.NewValue);
    }

    /// <summary>
    /// Gets or sets a value that indicates whether the ComboBox is editable.
    /// </summary>
    public bool IsEditable
    {
        get => (bool)GetValue(IsEditableProperty);
        set => SetValue(IsEditableProperty, value);
    }

    protected virtual void OnIsEditableChanged(bool oldValue, bool newValue)
    {
        DetachSpecificEventHandlers();
        ReAttachEventHandlersOnIsEditableChanged();
        isKeyDown = false;
    }

    #endregion IsEditable

    #region Description

    /// <summary>
    /// Identifies the Description dependency property.
    /// </summary>
    public static readonly DependencyProperty DescriptionProperty =
        DependencyProperty.Register(nameof(Description), typeof(object), typeof(GroupedComboBox), new(null));

    /// <summary>
    /// Gets or sets the text that is displayed in the control until the value is changed
    /// by a user action or some other operation.
    /// </summary>
    public object? Description
    {
        get => (object?)GetValue(DescriptionProperty);
        set => SetValue(DescriptionProperty, value);
    }

    #endregion Description

    #region LightDismissOverlayMode

    /// <summary>
    /// Gets or sets a value that specifies whether the area outside of a light-dismiss UI is darkened.
    /// </summary>
    public LightDismissOverlayMode LightDismissOverlayMode
    {
        get => (LightDismissOverlayMode)GetValue(LightDismissOverlayModeProperty);
        set => SetValue(LightDismissOverlayModeProperty, value);
    }

    /// <summary>
    /// Identifies the LightDismissOverlayMode dependency property.
    /// </summary>
    public static DependencyProperty LightDismissOverlayModeProperty { get; } =
        DependencyProperty.Register(
            nameof(LightDismissOverlayMode), typeof(LightDismissOverlayMode), typeof(GroupedComboBox), new(null));

    #endregion LightDismissOverlayMode

    #region PlaceholderForeground

    /// <summary>
    /// Identifies the PlaceholderForeground dependency property.
    /// </summary>
    public static DependencyProperty PlaceholderForegroundProperty =
         DependencyProperty.Register(nameof(PlaceholderForeground), typeof(Brush), typeof(GroupedComboBox), new(null));

    /// <summary>
    /// Gets or sets a brush that describes the color of placeholder text.
    /// </summary>
    public Brush? PlaceholderForeground
    {
        get => (Brush?)GetValue(PlaceholderForegroundProperty);
        set => SetValue(PlaceholderForegroundProperty, value);
    }

    #endregion

    public GroupedComboBox()
    {
        DefaultStyleKey = typeof(GroupedComboBox);
    }

    /// <inheritdoc/>
    protected override void OnApplyTemplate()
    {
        if (!GroupStyle.Any())
            GroupStyle.Add((GroupStyle)Application.Current.Resources["DefaultGroupedComboBoxGroupStyle"]);

        this.SelectionChanged -= GroupedComboBox_SelectionChanged;
        if (popup is not null)
        {
            popup.Opened -= PopupOpened;
            popup.Closed -= PopupClosed;
        }
        if (mainGrid is not null)
            mainGrid.RemoveHandler(KeyDownEvent, new KeyEventHandler(ItemsHost_KeyDown));
        if (dropDownButton is not null)
            dropDownButton.Click -= ButtonOrContentClick;
        DetachSpecificEventHandlers();

        base.OnApplyTemplate();

        mainGrid = GetTemplateChild(MainGrid) as Grid;
        popup = GetTemplateChild(Popup) as Popup;
        textBox = GetTemplateChild(EditableText) as TextBox;
        dropDownButton = GetTemplateChild(DropDownButton) as Button;
        contentPresenter = GetTemplateChild(ContentPresenter) as ContentPresenter;
        placeholderTextBlock = GetTemplateChild(PlaceholderTextBlock) as TextBlock;

        placeholderTextCache = PlaceholderText;

        if (popup is null || textBox is null || mainGrid is null || contentPresenter is null || dropDownButton is null)
            return;

        this.SelectionChanged += GroupedComboBox_SelectionChanged;
        mainGrid.AddHandler(KeyDownEvent, new KeyEventHandler(ItemsHost_KeyDown), true);
        mainGrid.Tapped += (s, e) => isKeyDown = false;
        dropDownButton.Click += ButtonOrContentClick;
        textBox.TextChanged += FindItems;
        popup.Opened += PopupOpened;
        popup.Closed += PopupClosed;

        ReAttachEventHandlersOnIsEditableChanged();
    }

    protected override void OnItemsChanged(object e)
    {
        if (isDropDownOpenedOnce)
            base.OnItemsChanged(e);
    }

    private void GroupedComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (!isDropDownOpenedOnce)
        {
            if (this.SelectedItem != SelectedValue)
            {
                this.SelectedItem = SelectedValue;
                PlaceholderText = string.Empty;
            }
        }
        else
        {
            IsDropDownOpen = false;
            if (this.SelectedItem is null && textBox is not null)
                textBox.Text = string.Empty;
            else
                SelectedValue = this.SelectedItem;
        }
        if (SelectedValue is not null)
        {
            PlaceholderText = string.Empty;
            if (textBox is not null && IsEditable)
                textBox.Text = SelectedValue?.ToString() ?? string.Empty;
            if (contentPresenter is not null && !IsEditable)
                contentPresenter.Content = SelectedValue?.ToString() ?? string.Empty;
        }
        else if (textBox is not null && IsEditable)
            textBox.Text = SelectedValueCache;
        else if (contentPresenter is not null && !string.IsNullOrEmpty(SelectedValueCache) && !IsEditable)
            contentPresenter.Content = SelectedValueCache;
        if (textBox is not null && IsEditable)
            textBox.Focus(FocusState.Programmatic);
    }

    private void FindItems(object sender, RoutedEventArgs e)
    {
        var senderAsTextBox = sender as TextBox;
        if (senderAsTextBox is null) return;
        SelectedValueCache = senderAsTextBox.Text;
        if (string.IsNullOrEmpty(senderAsTextBox.Text))
        {
            this.SelectedItem = null;
            PlaceholderText = placeholderTextCache;
        }
        else
        {
            // TODO: Use DisplaymemberPath
            var item = this.Items.FirstOrDefault(i => string.Equals(i?.ToString(), senderAsTextBox.Text, StringComparison.InvariantCultureIgnoreCase));
            if (item is not null)
            {
                if (this.SelectedItem != item)
                    this.SelectedItem = item;
            }
            else
            {
                this.SelectedItem = null;
                PlaceholderText = placeholderTextCache;
            }
        }
        if (textBox is not null && textBox.FocusState != FocusState.Unfocused)
            PlaceholderText = string.Empty;
    }

    protected override void OnGotFocus(RoutedEventArgs e)
    {
        base.OnGotFocus(e);
        VisualStateManager.GoToState(this, "Focused", true);
    }

    protected override void OnLostFocus(RoutedEventArgs e)
    {
        base.OnLostFocus(e);
        VisualStateManager.GoToState(this, "Unfocused", true);
        if (AreAllControlsUnfocused)
        {
            if (popup is not null)
                popup.IsOpen = false;
            isKeyDown = false;
        }
    }

    private void ItemsHost_KeyDown(object sender, KeyRoutedEventArgs e)
    {
        int count = this.Items.Count;
        int index = this.SelectedIndex;
        bool handled = false;
        switch (e.Key)
        {
            case Windows.System.VirtualKey.Down:
                if (popup is not null && !popup.IsOpen && !isKeyDown)
                {
                    IsDropDownOpen = true;
                    isKeyDown = true;
                    handled = true;
                }
                else if (count > 0)
                {
                    isKeyDown = true;
                    if (index < count - 1)
                        this.SelectedIndex = index + 1;
                    else
                        this.SelectedIndex = 0;
                    handled = true;
                }
                break;
            case Windows.System.VirtualKey.Up:
                if (popup is not null && !popup.IsOpen && !isKeyDown)
                {
                    IsDropDownOpen = true;
                    isKeyDown = true;
                    handled = true;
                }
                else if (count > 0)
                {
                    isKeyDown = true;
                    if (index > 0)
                        this.SelectedIndex = index - 1;
                    else
                        this.SelectedIndex = count - 1;
                    handled = true;
                }
                break;
            case Windows.System.VirtualKey.Enter:
                isKeyDown = false;
                IsDropDownOpen = false;
                handled = true;
                break;
            case Windows.System.VirtualKey.Home:
                if (count > 0)
                {
                    this.SelectedIndex = 0;
                    handled = true;
                }
                break;
            case Windows.System.VirtualKey.End:
                if (count > 0)
                {
                    this.SelectedIndex = count - 1;
                    handled = true;
                }
                break;
            case Windows.System.VirtualKey.Escape:
                if (IsDropDownOpen)
                {
                    isKeyDown = false;
                    IsDropDownOpen = false;
                    handled = true;
                }
                break;
            case Windows.System.VirtualKey.F4:
                isKeyDown = !isKeyDown;
                IsDropDownOpen = !IsDropDownOpen;
                handled = true;
                break;
        }
        if (handled)
            e.Handled = true;
    }

    private void ButtonOrContentClick(object sender, RoutedEventArgs e)
    {
        isKeyDown = !isKeyDown;
        IsDropDownOpen = !IsDropDownOpen;
        if (!IsEditable && contentPresenter is not null && popup is not null && popup.IsOpen == false)
            IsDropDownOpen = true;
    }

    private void PlaceholderTextBlockTapped(object sender, TappedRoutedEventArgs e)
    {
        if (textBox is not null && IsEditable)
            textBox.Focus(FocusState.Programmatic);
    }

    private void PopupOpened(object? sender, object? e)
    {
        VisualStateManager.GoToState(this, "Focused", true);
    }

    private void PopupClosed(object? sender, object? e)
    {
        if (IsEditable)
            this.Focus(FocusState.Programmatic);
        else
            contentPresenter?.Focus(FocusState.Programmatic);
    }

    protected virtual void ReAttachEventHandlersOnIsEditableChanged()
    {
        if (placeholderTextBlock is not null)
            placeholderTextBlock.Tapped += PlaceholderTextBlockTapped;
        if (this.SelectedItem is not null)
            SelectedValue = this.SelectedItem;
        else
            SelectedValue = null;
        if (textBox is not null && !string.IsNullOrEmpty(SelectedValueCache))
            textBox.Text = SelectedValueCache;
        if (contentPresenter is not null)
            contentPresenter.AddHandler(TappedEvent, new TappedEventHandler(ButtonOrContentClick), true);
        if (this.SelectedItem is null && contentPresenter is not null)
            contentPresenter.Content = PlaceholderText;
        if (contentPresenter is not null && !string.IsNullOrEmpty(SelectedValueCache))
            contentPresenter.Content = SelectedValueCache;
    }
    protected void DetachSpecificEventHandlers()
    {
        if (contentPresenter is not null)
            contentPresenter.RemoveHandler(TappedEvent, new TappedEventHandler(ButtonOrContentClick));
        if (placeholderTextBlock is not null)
            placeholderTextBlock.Tapped -= PlaceholderTextBlockTapped;
    }

    protected override bool IsItemItsOwnContainerOverride(object item)
    {
        return item is GroupedComboBoxItem;
    }

    protected override DependencyObject GetContainerForItemOverride()
    {
        return new GroupedComboBoxItem();
    }
}
