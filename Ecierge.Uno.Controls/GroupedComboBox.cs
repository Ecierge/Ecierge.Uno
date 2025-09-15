namespace Ecierge.Uno.Controls;

using System;
using System.Linq;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Input;

public partial class GroupedComboBox : GridView
{
    private Popup? popupIfEditable;
    private Popup? popupIfNotEditable;
    private Button? dropDownButton;
    private TextBox? textBox;
    private Grid? mainGrid;
    private bool isDropDownOpenedOnce = false;
    private bool isKeyDown = false;
    private ContentPresenter? contentPresenter;
    private TextBlock? header;
    private TextBlock? description;
    private Border? border;
    private ScrollViewer? contentElementIfEditable;
    private string placeholderTextCache = string.Empty;

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
        if (popupIfEditable is not null && IsEditable)
            popupIfEditable.IsOpen = newIsDropDownOpen;
        if (popupIfNotEditable is not null && !IsEditable)
            popupIfNotEditable.IsOpen = newIsDropDownOpen;
        if (isKeyDown && !IsDropDownOpen)
        {
            if (IsEditable && textBox is not null && popupIfEditable is not null && textBox.FocusState != FocusState.Unfocused)
                popupIfEditable.IsOpen = true;
            else if (popupIfNotEditable is not null)
                popupIfNotEditable.IsOpen = true;
            IsDropDownOpen = true;
        }
        if (IsEditable && textBox is not null && popupIfEditable is not null && popupIfEditable.FocusState != FocusState.Unfocused)
            textBox.Focus(FocusState.Programmatic);
        else
        {
            if (!IsDropDownOpen && contentPresenter is not null)
                contentPresenter.Focus(FocusState.Programmatic);
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

    #region Header

    /// <summary>
    /// Header Dependency Property
    /// </summary>
    public static readonly DependencyProperty HeaderProperty =
         DependencyProperty.Register(nameof(Header), typeof(object), typeof(GroupedComboBox), new(null));

    /// <summary>
    /// Gets or sets the Header property. This dependency property
    /// indicates header.
    /// </summary>
    public object? Header
    {
        get => (object?)GetValue(HeaderProperty);
        set => SetValue(HeaderProperty, value);
    }

    #endregion Header

    #region HeaderTemplate

    /// <summary>
    /// Identifies the HeaderTemplate dependency property.
    /// </summary>
    public static readonly DependencyProperty HeaderTemplateProperty =
        DependencyProperty.Register(nameof(HeaderTemplate), typeof(DataTemplate), typeof(GroupedComboBox), new((DataTemplate?)null));

    /// <summary>
    /// Gets or sets the template used to display the header.
    /// </summary>
    public DataTemplate? HeaderTemplate
    {
        get => (DataTemplate?)GetValue(HeaderTemplateProperty);
        set => SetValue(HeaderTemplateProperty, value);
    }

    #endregion HeaderTemplate

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
            nameof(LightDismissOverlayMode), typeof(object), typeof(GroupedComboBox), new(null));

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
        this.SelectionChanged -= GropedComboBox_SelectionChanged;
        this.LostFocus -= (s, e) =>
        {
            if (textBox is null || dropDownButton is null || popupIfEditable is null || popupIfNotEditable is null)
                return;
            if (textBox.FocusState == FocusState.Unfocused && dropDownButton.FocusState == FocusState.Unfocused && IsEditable && popupIfEditable.FocusState == FocusState.Unfocused)
            {
                popupIfEditable.IsOpen = false;
                popupIfNotEditable.IsOpen = false;
                isKeyDown = false;
            }
        };
        if (textBox is not null)
        {
            textBox.RemoveHandler(KeyDownEvent, new KeyEventHandler(ItemsHost_KeyDown));
            textBox.TextChanged -= FindItems;
        }
        if (contentPresenter is not null)
            contentPresenter.RemoveHandler(KeyDownEvent, new KeyEventHandler(ItemsHost_KeyDown));
        if (popupIfNotEditable is not null)
        {
            popupIfNotEditable.KeyDown -= ItemsHost_KeyDown;
            if (border is not null)
                popupIfNotEditable.Opened += (s, e) =>
                {
                    border.CornerRadius = new CornerRadius(4, 4, 0, 0);
                    if (contentElementIfEditable is not null)
                        contentElementIfEditable.CornerRadius = new CornerRadius(4, 4, 0, 0);
                };
        }
        if (popupIfEditable is not null)
        {
            popupIfEditable.KeyDown -= ItemsHost_KeyDown;
            if (border is not null)
                popupIfEditable.Opened += (s, e) =>
                {
                    border.CornerRadius = new CornerRadius(4, 4, 0, 0);
                    if (contentElementIfEditable is not null)
                        contentElementIfEditable.CornerRadius = new CornerRadius(4, 4, 0, 0);
                };
        }
        if (mainGrid is not null)
            mainGrid.KeyDown -= ItemsHost_KeyDown;
        if (dropDownButton is not null)
            dropDownButton.KeyDown -= ItemsHost_KeyDown;

        base.OnApplyTemplate();

        mainGrid = GetTemplateChild("MainGrid") as Grid;
        popupIfEditable = GetTemplateChild("PopupIfEditable") as Popup;
        popupIfNotEditable = GetTemplateChild("PopupIfNotEditable") as Popup;
        textBox = GetTemplateChild("EditableText") as TextBox;
        dropDownButton = GetTemplateChild("DropDownButton") as Button;
        contentPresenter = GetTemplateChild("ContentPresenter") as ContentPresenter;
        header = GetTemplateChild("Header") as TextBlock;
        description = GetTemplateChild("Description") as TextBlock;
        border = GetTemplateChild("RootBorder") as Border;
        contentElementIfEditable = GetTemplateChild("ContentElement") as ScrollViewer;

        placeholderTextCache = PlaceholderText;

        if (Header is not null && header is not null)
            header.Text = Header.ToString();
        if (Description is not null && description is not null)
        {
            description.Text = Description.ToString();
        }

        if (popupIfEditable is null || popupIfNotEditable is null || textBox is null || mainGrid is null || contentPresenter is null || dropDownButton is null || border is null)
            return;

        if (IsEditable)
            popupIfEditable.IsOpen = IsDropDownOpen;
        else
        {
            popupIfNotEditable.IsOpen = IsDropDownOpen;
            contentPresenter.Content = PlaceholderText;
        }

        dropDownButton.Click += (s, e) => { isKeyDown = false; IsDropDownOpen = !IsDropDownOpen; };

        contentPresenter.Tapped += (s, e) => { isKeyDown = false; IsDropDownOpen = !IsDropDownOpen; };

        textBox.TextChanged += FindItems;

        this.SelectionChanged += GropedComboBox_SelectionChanged;

        popupIfEditable.Opened += (s, e) =>
        {
            border.CornerRadius = new CornerRadius(4, 4, 0, 0);
            if (contentElementIfEditable is not null)
                contentElementIfEditable.CornerRadius = new CornerRadius(4, 4, 0, 0);
        };

        popupIfEditable.Closed += (s, e) =>
        {
            border.CornerRadius = new CornerRadius(4);
            if (contentElementIfEditable is not null)
                contentElementIfEditable.CornerRadius = new CornerRadius(4);
        };

        this.LostFocus += (s, e) =>
        {
            if (textBox.FocusState == FocusState.Unfocused && dropDownButton.FocusState == FocusState.Unfocused && IsEditable && popupIfEditable.FocusState == FocusState.Unfocused)
            {
                popupIfEditable.IsOpen = false;
                popupIfNotEditable.IsOpen = false;
                isKeyDown = false;
            }
        };

        textBox.LostFocus += (s, e) => SelectedValue = this.SelectedItem;

        if (IsEditable)
        {
            textBox.AddHandler(KeyDownEvent, new KeyEventHandler(ItemsHost_KeyDown), true);
        }
        else
        {
            contentPresenter.AddHandler(KeyDownEvent, new KeyEventHandler(ItemsHost_KeyDown), true);
        }
        popupIfNotEditable.KeyDown += ItemsHost_KeyDown;
        popupIfEditable.KeyDown += ItemsHost_KeyDown;
        mainGrid.KeyDown += ItemsHost_KeyDown;
        dropDownButton.KeyDown += ItemsHost_KeyDown;
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
                PlaceholderText = string.Empty;
            }
        }
        else
        {
            if (textBox is not null && textBox.FocusState == FocusState.Unfocused)
            {
                IsDropDownOpen = false;
                SelectedValue = this.SelectedItem;
                if (this.SelectedValue is null)
                    textBox.Text = string.Empty;
            }
        }
        if (SelectedValue is not null)
            PlaceholderText = string.Empty;

        if (textBox is not null && IsEditable)
            textBox.Focus(FocusState.Programmatic);
    }

    private void FindItems(object sender, RoutedEventArgs e)
    {
        var s = sender as TextBox;
        if (s is null) return;
        if (string.IsNullOrEmpty(s.Text))
        {
            this.SelectedItem = null;
            PlaceholderText = placeholderTextCache;
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
                PlaceholderText = placeholderTextCache;
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

    private void ItemsHost_KeyDown(object sender, KeyRoutedEventArgs e)
    {
        int count = this.Items.Count;
        int index = this.SelectedIndex;
        bool handled = false;
        switch (e.Key)
        {
            case Windows.System.VirtualKey.Down:
                if (((popupIfEditable is not null && !popupIfEditable.IsOpen) || (popupIfNotEditable is not null && !popupIfNotEditable.IsOpen)) && !isKeyDown)
                {
                    IsDropDownOpen = true;
                    isKeyDown = true;
                    handled = true;
                }
                else if (count > 0)
                {
                    isKeyDown = true;
                    if (index < count - 1)
                    {
                        this.SelectedIndex = index + 1;
                    }
                    else
                        this.SelectedIndex = 0;
                }
                handled = true;
                break;
            case Windows.System.VirtualKey.Up:
                if (((popupIfEditable is not null && !popupIfEditable.IsOpen) || (popupIfNotEditable is not null && !popupIfNotEditable.IsOpen)) && !isKeyDown)

                {
                    IsDropDownOpen = true;
                    isKeyDown = true;
                    handled = true;
                }
                else if (count > 0)
                {
                    isKeyDown = true;
                    if (index > 0)
                    {
                        this.SelectedIndex = index - 1;
                    }
                    else
                    {
                        this.SelectedIndex = count - 1;
                    }
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
}
