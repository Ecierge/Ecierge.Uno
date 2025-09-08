namespace Ecierge.Uno.Controls;

using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Input;

public partial class GroupedComboBox : GridView
{
    private Popup? popupIfEditable;
    private Popup? popupIfNotEditable;
    private Button? dropDownButton;
    private TextBox? textBox;
    private bool isDropDownOpenedOnce = false;

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
            isDropDownOpenedOnce = true;
        if (popupIfEditable is not null && IsEditable)
            popupIfEditable.IsOpen = newIsDropDownOpen;
        if (popupIfNotEditable is not null && !IsEditable)
            popupIfNotEditable.IsOpen = newIsDropDownOpen;
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


    #region IsEditable

    /// <summary>
    /// Identifies the IsEditable dependency property.
    /// </summary>
    public static readonly DependencyProperty IsEditableProperty =
         DependencyProperty.Register(nameof(IsEditable), typeof(bool), typeof(GroupedComboBox),
            new(false));

    /// <summary>
    /// Gets or sets a value that indicates whether the ComboBox is editable.
    /// </summary>
    public bool IsEditable
    {
        get => (bool)GetValue(IsEditableProperty);
        set => SetValue(IsEditableProperty, value);
    }

    #endregion IsEditable


    public GroupedComboBox()
    {
        DefaultStyleKey = typeof(GroupedComboBox);
    }

    /// <inheritdoc/>
    protected override void OnApplyTemplate()
    {
        base.OnApplyTemplate();

        popupIfEditable = GetTemplateChild("PopupIfEditable") as Popup;
        popupIfNotEditable = GetTemplateChild("PopupIfNotEditable") as Popup;
        textBox = GetTemplateChild("EditableText") as TextBox;
        dropDownButton = GetTemplateChild("DropDownButton") as Button;
        var mainGrid = GetTemplateChild("MainGrid") as Grid;
        var contentPresenter = GetTemplateChild("ContentPresenter") as ContentPresenter;

        if (popupIfEditable is null || popupIfNotEditable is null || textBox is null || mainGrid is null || contentPresenter is null || dropDownButton is null)
            return;

        if (IsEditable)
            popupIfEditable.IsOpen = IsDropDownOpen;
        else
            popupIfNotEditable.IsOpen = IsDropDownOpen;

        contentPresenter.Content = PlaceholderText;
        textBox.PlaceholderText = PlaceholderText;

        dropDownButton.Click -= (s, e) => IsDropDownOpen = !IsDropDownOpen;
        dropDownButton.Click += (s, e) => IsDropDownOpen = !IsDropDownOpen;

        contentPresenter.Tapped -= (s, e) => IsDropDownOpen = !IsDropDownOpen;
        contentPresenter.Tapped += (s, e) => IsDropDownOpen = !IsDropDownOpen;

        popupIfEditable.Opened -= (s, e) => FocusManager.TryFocusAsync(textBox, FocusState.Programmatic);
        popupIfEditable.Opened += (s, e) => FocusManager.TryFocusAsync(textBox, FocusState.Programmatic);

        popupIfNotEditable.Closed -= (s, e) => IsDropDownOpen = false;
        popupIfNotEditable.Closed += (s, e) => IsDropDownOpen = false;

        textBox.TextChanged -= FindItems;
        textBox.TextChanged += FindItems;

        this.SelectionChanged -= GropedComboBox_SelectionChanged;
        this.SelectionChanged += GropedComboBox_SelectionChanged;

        this.LostFocus -= (s, e) =>
        {
            if (textBox.FocusState == FocusState.Unfocused && dropDownButton.FocusState == FocusState.Unfocused && IsEditable)
                IsDropDownOpen = false;
        };
        this.LostFocus += (s, e) =>
        {
            if (textBox.FocusState == FocusState.Unfocused && dropDownButton.FocusState == FocusState.Unfocused && IsEditable)
                IsDropDownOpen = false;
        };

    }

    protected override void OnItemsChanged(object e)
    {
        if (isDropDownOpenedOnce)
            base.OnItemsChanged(e);
    }

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
            if (textBox.FocusState == FocusState.Unfocused)
            {
                IsDropDownOpen = false;
                SelectedValue = this.SelectedItem;
                if (this.SelectedValue is null)
                    textBox!.Text = String.Empty;
            }
        }
        this.Focus(FocusState.Programmatic);
    }

    private void FindItems(object sender, RoutedEventArgs e)
    {
        var s = sender as TextBox;
        if (s is null) return;
        if (string.IsNullOrEmpty(s.Text))
        {
            this.SelectedItem = null;
        }
        else
        {
            var item = this.Items.FirstOrDefault(i => string.Equals(i?.ToString(), s.Text, StringComparison.InvariantCultureIgnoreCase));
            if (item is not null)
            {
                if (this.SelectedItem != item)
                    this.SelectedItem = item;
            }
            else
            {
                this.SelectedItem = null;
            }
        }
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
    }
}
