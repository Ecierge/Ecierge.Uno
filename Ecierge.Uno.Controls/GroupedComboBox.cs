namespace Ecierge.Uno.Controls;

using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Input;

public class GroupedComboBox : ListView
{
    private Popup? _popup;
    private TextBox? _textBox;
    private bool _IsDropDownOpened = false;

    #region IsDropDownOpen

    /// <summary>
    /// Identifies the IsDropDownOpen dependency property.
    /// </summary>
    public static readonly DependencyProperty IsDropDownOpenProperty =
        DependencyProperty.Register(nameof(IsDropDownOpen), typeof(bool), typeof(GroupedComboBox), new(false));

    /// <summary>
    /// Gets or sets a value that indicates whether the drop-down portion of the GroupedComboBox
    /// is currently open.
    /// </summary>
    public bool IsDropDownOpen
    {
        get => (bool)GetValue(IsDropDownOpenProperty);
        set
        {
            SetValue(IsDropDownOpenProperty, value);
            _IsDropDownOpened = true;
            if (_popup != null && _popup.IsOpen != value)
            {
                _popup.IsOpen = value;
            }
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

        _popup = GetTemplateChild("Popup") as Popup;
        _textBox = GetTemplateChild("EditableText") as TextBox;
        var mainGrid = GetTemplateChild("MainGrid") as Grid;
        var contentPresenter = GetTemplateChild("ContentPresenter") as ContentPresenter;

        if (_popup is null || _textBox is null || mainGrid is null || contentPresenter is null)
            return;

        _popup.IsOpen = IsDropDownOpen;

        _textBox.Tapped -= OnTextBoxTapped;
        _textBox.Tapped += OnTextBoxTapped;

        mainGrid.RemoveHandler(UIElement.PointerPressedEvent, new PointerEventHandler(TextBox_PointerPressed));
        mainGrid.AddHandler(UIElement.PointerPressedEvent, new PointerEventHandler(TextBox_PointerPressed), true);

        contentPresenter.RemoveHandler(UIElement.PointerPressedEvent, new PointerEventHandler(TextBox_PointerPressed));
        contentPresenter.AddHandler(UIElement.PointerPressedEvent, new PointerEventHandler(TextBox_PointerPressed), true);

        contentPresenter.Tapped -= OnTextBoxTapped;
        contentPresenter.Tapped += OnTextBoxTapped;

        _textBox.GotFocus -= TextBox_GotFocus;
        _textBox.GotFocus += TextBox_GotFocus;

        _popup.XamlRoot.Content!.PointerPressed -= Content_PointerPressed;
        _popup.XamlRoot.Content!.PointerPressed += Content_PointerPressed;

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
        if (_IsDropDownOpened)
        {
            base.OnItemsChanged(e);
        }
    }

    private void TextBox_PointerPressed(object sender, PointerRoutedEventArgs e) => IsDropDownOpen = true;

    private void TextBox_GotFocus(object sender, RoutedEventArgs e) => IsDropDownOpen = true;

    private void GropedComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (!_IsDropDownOpened)
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
            if (!(this == element || element is ListViewItem || element == _textBox) && this != element)
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
