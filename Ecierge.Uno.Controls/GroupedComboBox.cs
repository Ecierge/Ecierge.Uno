namespace Ecierge.Uno.Controls;

using System;
using System.Linq;
using System.Runtime.CompilerServices;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Input;

[TemplatePart(Name = EditableText, Type = typeof(TextBox))]
[TemplatePart(Name = MainGrid, Type = typeof(Grid))]
[TemplatePart(Name = Popup, Type = typeof(Popup))]
[TemplatePart(Name = DropDownIcon, Type = typeof(FontIcon))]
[TemplatePart(Name = ContentPresenter, Type = typeof(ContentPresenter))]
[TemplatePart(Name = PlaceholderTextBlock, Type = typeof(TextBlock))]

public partial class GroupedComboBox : ListViewBase
{
    #region TemplatePartNames

    private const string EditableText = "EditableText";
    private const string MainGrid = "MainGrid";
    private const string Popup = "Popup";
    private const string DropDownIcon = "DropDownIcon";
    private const string ContentPresenter = "ContentPresenter";
    private const string PlaceholderTextBlock = "PlaceholderTextBlock";

    #endregion TemplatePartNames

    private Popup? popup;
    private FontIcon? dropDownIcon;
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
        && dropDownIcon is not null && dropDownIcon.FocusState == FocusState.Unfocused
        && popup is not null && popup.FocusState == FocusState.Unfocused;

    /// <summary>
    /// Lookup dictionary used for fast case-insensitive access to items by their string key.
    /// The key is usually derived from the item's <see cref="DisplayMemberPath"/> or its <see cref="object.ToString()"/> value.
    /// </summary>
    private Dictionary<string, object> itemsLookup = new(StringComparer.OrdinalIgnoreCase);
    /// <summary>
    /// Keeps a reference-based snapshot of the current items collection.
    /// Used to efficiently detect added or removed items without rebuilding the entire lookup.
    /// </summary>
    private HashSet<object> prevItemsSet = new(new ReferenceEqualityComparer());

    /// <summary>
    /// Provides reference equality comparison for objects.
    /// This ensures that object identity (not logical equality) is used when comparing items.
    /// </summary>
    private sealed class ReferenceEqualityComparer : IEqualityComparer<object>
    {
        public new bool Equals(object? x, object? y) => ReferenceEquals(x, y);
        public int GetHashCode(object obj) => RuntimeHelpers.GetHashCode(obj);
    }

    private BindingEvaluator bindingEvaluator = new();


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
            if (isKeyDown && !IsDropDownOpen && !popup.IsOpen)
                IsDropDownOpen = true;
            if (IsEditable && textBox is not null && popup.FocusState != FocusState.Unfocused)
                textBox.Focus(FocusState.Programmatic);
            if (!IsEditable)
                this.Focus(FocusState.Programmatic);
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
        DependencyProperty.Register(nameof(PlaceholderText), typeof(string), typeof(GroupedComboBox), new("Select"));

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
        DefaultStyleKey = typeof(GroupedComboBox);
        this.RegisterPropertyChangedCallback(ItemsControl.DisplayMemberPathProperty, (d, dp) =>
        {
            var control = (GroupedComboBox)d;
            control.OnDisplayMemberPathChanged(d, EventArgs.Empty);
        });
    }

    /// <summary>
    /// Gets or sets a value that indicates whether the ComboBox is editable.
    /// </summary>
    public bool IsEditable
    {

        this.SelectionChanged -= GroupedComboBox_SelectionChanged;
        if (popup is not null)
        {
            popup.Opened -= PopupOpened;
            popup.Closed -= PopupClosed;
        }
        if (dropDownIcon is not null)
            dropDownIcon.Tapped -= ButtonOrContentClick;
        mainGrid?.RemoveHandler(KeyDownEvent, new KeyEventHandler(ItemsHost_KeyDown));

        DetachSpecificEventHandlers();

        base.OnApplyTemplate();

        mainGrid = GetTemplateChild(MainGrid) as Grid;
        popup = GetTemplateChild(Popup) as Popup;
        textBox = GetTemplateChild(EditableText) as TextBox;
        dropDownIcon = GetTemplateChild(DropDownIcon) as FontIcon;
        contentPresenter = GetTemplateChild(ContentPresenter) as ContentPresenter;
        placeholderTextBlock = GetTemplateChild(PlaceholderTextBlock) as TextBlock;

        placeholderTextCache = PlaceholderText;

        if (popup is null || textBox is null || mainGrid is null || contentPresenter is null || dropDownIcon is null)
            return;

        this.SelectionChanged += GroupedComboBox_SelectionChanged;
        mainGrid.AddHandler(KeyDownEvent, new KeyEventHandler(ItemsHost_KeyDown), true);
        mainGrid.Tapped += (s, e) => isKeyDown = false;
        dropDownIcon.Tapped += ButtonOrContentClick;
        textBox.TextChanged += FindItems;
        popup.Opened += PopupOpened;
        popup.Closed += PopupClosed;

        ReAttachEventHandlersOnIsEditableChanged();
    }
    private void OnDisplayMemberPathChanged(object? sender, EventArgs e)
    {
        bindingEvaluator = new();
    }

    public string? GetDisplayMemberValue(object item)
    {
        if (item is null)
            return null;
        if (!string.IsNullOrEmpty(DisplayMemberPath))
        {
            var value = bindingEvaluator.Evaluate(item, DisplayMemberPath);
            if (value is not null)
                return value.ToString();
        }
        return item.ToString();
    }

    private void AddItemToLookup(object item)
    {
        var key = GetDisplayMemberValue(item);
        if (key is not null)
            itemsLookup[key] = item;
    }

    private void RemoveItemFromLookup(object item)
    {
        var key = GetDisplayMemberValue(item);
        if (key is not null)
            itemsLookup.Remove(key);
    }

    private void RebuildLookup()
    {
        itemsLookup.Clear();
        prevItemsSet.Clear();

        var items = Items.Cast<object>().Where(x => x is not null).ToList();
        foreach (var it in items) AddItemToLookup(it);
        foreach (var it in items) prevItemsSet.Add(it);
    }

    protected override void OnItemsChanged(object e)
    {
        if (isDropDownOpenedOnce)
            base.OnItemsChanged(e);

        OnItemsChanged();
    }

    private void OnItemsChanged()
    {
        var currentItems = Items.Cast<object>().Where(x => x is not null).ToList();
        var newSet = new HashSet<object>(currentItems, new ReferenceEqualityComparer());

        if (prevItemsSet.Count == 0)
        {
            RebuildLookup();
            return;
        }

        var added = newSet.Where(it => !prevItemsSet.Contains(it)).ToList();
        var removed = prevItemsSet.Where(it => !newSet.Contains(it)).ToList();

        int changes = added.Count + removed.Count;
        if (changes == 0) return;

        if (changes <= Math.Max(1, prevItemsSet.Count / 2))
        {
            foreach (var r in removed) RemoveItemFromLookup(r);
            foreach (var a in added) AddItemToLookup(a);
            prevItemsSet = newSet;
        }
        else
            RebuildLookup();
    }

    private void GroupedComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (!isDropDownOpenedOnce)
        {
            if (this.SelectedItem != SelectedValue)
                PlaceholderText = string.Empty;
        }
        else if (textBox is not null && textBox.FocusState == FocusState.Unfocused)
        {
            IsDropDownOpen = false;
            if (this.SelectedItem is null)
                textBox.Text = string.Empty;
            else
                SelectedValue = this.SelectedItem;
        }

        SetDisplayForSelectedItem();

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
            string text = senderAsTextBox.Text;

            if (itemsLookup.TryGetValue(text, out var item))
            {
                if (this.SelectedItem != item)
                    SelectedItem = item;
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
        VisualStateManager.GoToState(this, IsEditable ? "FocusedEditable" : "FocusedNotEditable", true);
        mainGrid?.Focus(FocusState.Programmatic);
    }

    protected override void OnLostFocus(RoutedEventArgs e)
    {
        base.OnLostFocus(e);
        VisualStateManager.GoToState(this, "Unfocused", true);

        if (AreAllControlsUnfocused && popup is not null)
            popup.IsOpen = false;
        isKeyDown = false;
    }

    private void ItemsHost_KeyDown(object sender, KeyRoutedEventArgs e)
    {
        int count = this.Items.Count;
        int index = this.SelectedIndex;
        bool handled = false;
        switch (e.Key)
        {
            case Windows.System.VirtualKey.Down:
                if (count > 0)
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
                if (count > 0)
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
    protected void DetachSpecificEventHandlers()
    {
        if (contentPresenter is not null)
            contentPresenter.RemoveHandler(TappedEvent, new TappedEventHandler(ButtonOrContentClick));
        if (placeholderTextBlock is not null)
            placeholderTextBlock.Tapped -= PlaceholderTextBlockTapped;
    }

    protected override void OnPointerPressed(PointerRoutedEventArgs e)
    {
        base.OnPointerPressed(e);
    }

    private void ButtonOrContentClick(object sender, TappedRoutedEventArgs e)
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
        if (contentPresenter is not null)
            contentPresenter.AddHandler(TappedEvent, new TappedEventHandler(ButtonOrContentClick), true);
        SelectedValue = this.SelectedItem;
        SetDisplayForSelectedItem();
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

    protected void SetDisplayForSelectedItem()
    {
        if (this.SelectedItem is not null)
        {
            PlaceholderText = string.Empty;
            contentPresenter?.Opacity = 1;

            if (textBox is not null && IsEditable)
            {
                if (!string.IsNullOrEmpty(DisplayMemberPath))
                {
                    textBox.Text = GetDisplayMemberValue(SelectedItem) ?? string.Empty;
                }
                else
                {
                    textBox.Text = SelectedItem?.ToString() ?? SelectedValueCache;
                }
            }

            if (contentPresenter is not null && !IsEditable)
            {
                if (this.ItemTemplate is not null)
                {
                    contentPresenter.ContentTemplate = this.ItemTemplate;
                    contentPresenter.Content = SelectedItem;
                }
                else if (!string.IsNullOrEmpty(DisplayMemberPath))
                {
                    contentPresenter.ContentTemplate = null;
                    contentPresenter.Content = GetDisplayMemberValue(SelectedItem);
                }
                else
                {
                    contentPresenter.ContentTemplate = null;
                    contentPresenter.Content = SelectedItem;
                }
            }
        }
        else if (textBox is not null && IsEditable && !string.IsNullOrEmpty(SelectedValueCache))
        {
            textBox.Text = SelectedValueCache;
        }
        else if (contentPresenter is not null && !string.IsNullOrEmpty(SelectedValueCache) && !IsEditable)
        {
            contentPresenter.ContentTemplate = null;
            contentPresenter.Content = SelectedValueCache;
        }
        else if (contentPresenter is not null && string.IsNullOrEmpty(SelectedValueCache) && !IsEditable)
        {
            contentPresenter.ContentTemplate = null;
            contentPresenter.Content = placeholderTextCache;
            contentPresenter.Opacity = 0.7;
        }
    }
}
