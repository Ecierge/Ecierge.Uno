namespace Ecierge.Uno.Controls;

using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Input;

public partial class GroupedComboBox : ListView
{
    private Popup? popup;
    private TextBox? textBox;
    private bool isDropDownOpenedOnce = false;

    #region IsDropDownOpen

    /// <summary>
    /// Identifies the IsDropDownOpen dependency property.
    /// </summary>
    public static readonly DependencyProperty IsDropDownOpenProperty =
        DependencyProperty.Register(nameof(IsDropDownOpen), typeof(bool), typeof(GroupedComboBox),
            new (false, new PropertyChangedCallback(OnIsDropDownOpenChanged)));

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
            isDropDownOpenedOnce = true;
        popup?.IsOpen = newIsDropDownOpen;
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

    #region SelectedValue

    /// <summary>
    /// Identifies the  SelectedValue dependency property.
    /// </summary>
    public static readonly DependencyProperty SelectedValueProperty =
        DependencyProperty.Register(nameof(SelectedValue), typeof(object), typeof(GroupedComboBox), new(null));

    /// <summary>
    /// Gets or sets the value of the selected item, obtained by using the SelectedValuePath.
    /// </summary>
    public object? SelectedValue
    {
        get => (object?)GetValue(SelectedValueProperty);
        set => SetValue(SelectedValueProperty, value);
    }

    #endregion SelectedValue

    public GroupedComboBox()
    {
        DefaultStyleKey = typeof(GroupedComboBox);
    }

    /// <inheritdoc/>
    protected override void OnApplyTemplate()
    {
        base.OnApplyTemplate();

        popup = GetTemplateChild("Popup") as Popup;
        textBox = GetTemplateChild("EditableText") as TextBox;
        var mainGrid = GetTemplateChild("MainGrid") as Grid;
        var contentPresenter = GetTemplateChild("ContentPresenter") as ContentPresenter;

        if (popup is null || textBox is null || mainGrid is null || contentPresenter is null)
            return;

        popup.IsOpen = IsDropDownOpen;

        textBox.Tapped -= OnTextBoxTapped;
        textBox.Tapped += OnTextBoxTapped;

        mainGrid.RemoveHandler(UIElement.PointerPressedEvent, new PointerEventHandler(TextBox_PointerPressed));
        mainGrid.AddHandler(UIElement.PointerPressedEvent, new PointerEventHandler(TextBox_PointerPressed), true);

        contentPresenter.RemoveHandler(UIElement.PointerPressedEvent, new PointerEventHandler(TextBox_PointerPressed));
        contentPresenter.AddHandler(UIElement.PointerPressedEvent, new PointerEventHandler(TextBox_PointerPressed), true);

        contentPresenter.Tapped -= OnTextBoxTapped;
        contentPresenter.Tapped += OnTextBoxTapped;

        textBox.GotFocus -= TextBox_GotFocus;
        textBox.GotFocus += TextBox_GotFocus;

        popup.XamlRoot.Content!.PointerPressed -= Content_PointerPressed;
        popup.XamlRoot.Content!.PointerPressed += Content_PointerPressed;

        FocusManager.GotFocus -= FocusManager_GotFocus;
        FocusManager.GotFocus += FocusManager_GotFocus;

        this.SelectionChanged -= GropedComboBox_SelectionChanged;
        this.SelectionChanged += GropedComboBox_SelectionChanged;

        this.Unloaded -= OnUnloaded;
        this.Unloaded += OnUnloaded;
    }

    private void OnUnloaded(object sender, RoutedEventArgs e) => FocusManager.GotFocus -= FocusManager_GotFocus;

    protected override void OnItemsChanged(object e)
    {
        if (isDropDownOpenedOnce)
            base.OnItemsChanged(e);
    }

    private void TextBox_PointerPressed(object sender, PointerRoutedEventArgs e) => IsDropDownOpen = true;

    private void TextBox_GotFocus(object sender, RoutedEventArgs e) => IsDropDownOpen = true;

    private void GropedComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (!isDropDownOpenedOnce)
        {
            if (this.SelectedItem != SelectedValue)
            {
                this.SelectedItem = SelectedValue;
            }
        }
        else
        {
            IsDropDownOpen = false;
            SelectedValue = this.SelectedItem;
        }
    }

    private void FocusManager_GotFocus(object? sender, FocusManagerGotFocusEventArgs e)
    {
        if (e.NewFocusedElement is FrameworkElement element)
        {
            if (!(this == element || element is ListViewItem || element == textBox) && this != element)
            {
                IsDropDownOpen = false;
            }
        }
        else
        {
            IsDropDownOpen = false;
        }
    }

    private void Content_PointerPressed(object sender, PointerRoutedEventArgs e) => IsDropDownOpen = false;

    private void OnTextBoxTapped(object sender, TappedRoutedEventArgs e) => IsDropDownOpen = true;
}
