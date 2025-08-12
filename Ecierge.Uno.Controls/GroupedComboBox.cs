namespace Ecierge.Uno.Controls;

using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Input;

public class GroupedComboBox : ListView
{
    private Popup? _popup;
    private TextBox? _textBox;
    private bool _isOpened = false;

    #region IsOpen

    /// <summary>
    /// IsOpen Dependency Property
    /// </summary>
    public static readonly DependencyProperty IsOpenProperty =
        DependencyProperty.Register(nameof(IsOpen), typeof(bool), typeof(GroupedComboBox), new(false));

    /// <summary>
    /// Gets or sets the IsOpen property. This dependency property
    /// indicates ....
    /// </summary>
    public bool IsOpen
    {
        get => (bool)GetValue(IsOpenProperty);
        set
        {
            SetValue(IsOpenProperty, value);
            _isOpened = true;
            if (_popup != null && _popup.IsOpen != value)
            {
                _popup.IsOpen = value;
            }
        }
    }

    #endregion IsOpen

    #region PlaceholderText

    /// <summary>
    /// PlaceholderText Dependency Property
    /// </summary>
    public static readonly DependencyProperty PlaceholderTextProperty =
        DependencyProperty.Register(nameof(PlaceholderText), typeof(string), typeof(GroupedComboBox), new(string.Empty));

    /// <summary>
    /// Gets or sets the PlaceholderText property. This dependency property
    /// indicates ....
    /// </summary>
    public string PlaceholderText
    {
        get => (string)GetValue(PlaceholderTextProperty);
        set => SetValue(PlaceholderTextProperty, value);
    }

    #endregion PlaceholderText

    #region TextBoxStyle

    /// <summary>
    /// TextBoxStyle Dependency Property
    /// </summary>
    public static readonly DependencyProperty TextBoxStyleProperty =
        DependencyProperty.Register(nameof(TextBoxStyle), typeof(Style), typeof(GroupedComboBox), new((Style?)null));

    /// <summary>
    /// Gets or sets the TextBoxStyle property. This dependency property
    /// indicates ....
    /// </summary>
    public Style? TextBoxStyle
    {
        get => (Style?)GetValue(TextBoxStyleProperty);
        set => SetValue(TextBoxStyleProperty, value);
    }

    #endregion TextBoxStyle

    #region SelectedObject

    /// <summary>
    /// SelectedObject Dependency Property
    /// </summary>
    public static readonly DependencyProperty SelectedObjectProperty =
        DependencyProperty.Register(nameof(SelectedObject), typeof(object), typeof(GroupedComboBox), new(null));

    /// <summary>
    /// Gets or sets the FirstItemSelected property. This dependency property
    /// indicates ....
    /// </summary>
    public object? SelectedObject
    {
        get => (object?)GetValue(SelectedObjectProperty);
        set => SetValue(SelectedObjectProperty, value);
    }

    #endregion SelectedObject

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

        _popup.IsOpen = IsOpen;

        _textBox.Tapped -= OnTextBoxTapped;
        _textBox.Tapped += OnTextBoxTapped;

        mainGrid.RemoveHandler(UIElement.PointerPressedEvent, new PointerEventHandler(TextBox_PointerPressed));
        mainGrid.AddHandler(UIElement.PointerPressedEvent, new PointerEventHandler(TextBox_PointerPressed), true);

        contentPresenter.RemoveHandler(UIElement.PointerPressedEvent, new PointerEventHandler(TextBox_PointerPressed));
        contentPresenter.AddHandler(UIElement.PointerPressedEvent, new PointerEventHandler(TextBox_PointerPressed), true);

        contentPresenter.Tapped -= OnTextBoxTapped;
        contentPresenter.Tapped += OnTextBoxTapped;

        _textBox.GotFocus -= _textBox_GotFocus;
        _textBox.GotFocus += _textBox_GotFocus;

        _popup.XamlRoot.Content!.PointerPressed -= Content_PointerPressed;
        _popup.XamlRoot.Content!.PointerPressed += Content_PointerPressed;

        FocusManager.GotFocus -= FocusManager_GotFocus;
        FocusManager.GotFocus += FocusManager_GotFocus;

        this.SelectionChanged -= GropedComboBox_SelectionChanged;
        this.SelectionChanged += GropedComboBox_SelectionChanged;

        this.Unloaded -= GroupedComboBox_Unloaded;
        this.Unloaded += GroupedComboBox_Unloaded;
    }

    private void GroupedComboBox_Unloaded(object sender, RoutedEventArgs e) => FocusManager.GotFocus -= FocusManager_GotFocus;

    protected override void OnItemsChanged(object e)
    {
        if (_isOpened)
        {
            base.OnItemsChanged(e);
        }
    }

    private void TextBox_PointerPressed(object sender, PointerRoutedEventArgs e) => IsOpen = true;

    private void _textBox_GotFocus(object sender, RoutedEventArgs e) => IsOpen = true;

    private void GropedComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (!_isOpened)
        {
            if (this.SelectedItem != SelectedObject)
            {
                this.SelectedItem = SelectedObject;
            }
        }
        else
        {
            IsOpen = false;
            SelectedObject = this.SelectedItem;
        }
    }

    private void FocusManager_GotFocus(object? sender, FocusManagerGotFocusEventArgs e)
    {
        if (e.NewFocusedElement is FrameworkElement element)
        {
            if (!(this == element || element is ListViewItem || element == _textBox) && this != element)
            {
                IsOpen = false;
            }
        }
        else
        {
            IsOpen = false;
        }
    }

    private void Content_PointerPressed(object sender, PointerRoutedEventArgs e) => IsOpen = false;

    private void OnTextBoxTapped(object sender, TappedRoutedEventArgs e) => IsOpen = true;
}
