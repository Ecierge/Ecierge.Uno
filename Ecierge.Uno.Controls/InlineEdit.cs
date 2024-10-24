namespace Ecierge.Uno.Controls;

using System;

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

#if WINDOWS
#endif

using Windows.Foundation.Collections;

[TemplatePart(Name = PART_ItemsControl, Type = typeof(ItemsControl))]
public sealed partial class InlineEdit : ComboBox
{
#pragma warning disable CA1707 // Identifiers should not contain underscores
    public const string PART_ItemsControl = "PART_ItemsControl";
#pragma warning restore CA1707 // Identifiers should not contain underscores

    bool skipLostFocus = false;

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
            skipLostFocus = true;
            if (GetTemplateChild(PART_ItemsControl) is ComboBox comboBox)
                comboBox.Focus(FocusState.Programmatic);
            else
                this.Focus(FocusState.Programmatic);
        }
    }

    #endregion

    #region CommitCommand

    /// <summary>
    /// CommitCommand Dependency Property
    /// </summary>
    public static readonly DependencyProperty CommitCommandProperty =
        DependencyProperty.Register(nameof(CommitCommand), typeof(ICommand), typeof(InlineEdit),
            new PropertyMetadata((ICommand?)null));

    /// <summary>
    /// Gets or sets the CommitCommand property. This dependency property
    /// indicates the command executed when change committed.
    /// </summary>
    public ICommand? CommitCommand
    {
        get => (ICommand?)GetValue(CommitCommandProperty);
        set => SetValue(CommitCommandProperty, value);
    }

    #endregion

    #region CancelCommand

    /// <summary>
    /// CancelCommand Dependency Property
    /// </summary>
    public static readonly DependencyProperty CancelCommandProperty =
        DependencyProperty.Register(nameof(CancelCommand), typeof(ICommand), typeof(InlineEdit),
            new PropertyMetadata((ICommand?)null));

    /// <summary>
    /// Gets or sets the CancelCommand property. This dependency property
    /// indicates the command executed when change cancelled.
    /// </summary>
    public ICommand? CancelCommand
    {
        get => (ICommand?)GetValue(CancelCommandProperty);
        set => SetValue(CancelCommandProperty, value);
    }

    #endregion

    #region DeleteCommand

    /// <summary>
    /// DeleteCommand Dependency Property
    /// </summary>
    public static readonly DependencyProperty DeleteCommandProperty =
        DependencyProperty.Register(nameof(DeleteCommand), typeof(ICommand), typeof(InlineEdit),
            new PropertyMetadata((ICommand?)null));

    /// <summary>
    /// Gets or sets the DeleteCommand property. This dependency property
    /// indicates the command to invoke when delete requested.
    /// </summary>
    public ICommand? DeleteCommand
    {
        get => (ICommand?)GetValue(DeleteCommandProperty);
        set => SetValue(DeleteCommandProperty, value);
    }

    #endregion

    public InlineEdit()
    {
        this.DefaultStyleKey = typeof(InlineEdit);
        this.GroupStyle.VectorChanged += OnGroupStyleVectorChanged;
    }

    protected override void OnLostFocus(RoutedEventArgs e)
    {
        base.OnLostFocus(e);
        if (skipLostFocus)
        {
            skipLostFocus = false;
            return;
        }
        if (!IsDropDownOpen)
        {
            IsEditing = false;
            CommitCommand?.Execute(null);
        }
        //DispatcherQueue.TryEnqueue(() =>
        //{
        //});
    }

    //#if WINDOWS
    //    private DropDownListBase? itemsControl;
    //#else
    private ItemsControl? itemsControl;
    //#endif

    private void OnGroupStyleVectorChanged(IObservableVector<GroupStyle> s, IVectorChangedEventArgs e)
    {
        var index = (int)e.Index;
        switch (e.CollectionChange)
        {
            case CollectionChange.Reset:
                itemsControl?.GroupStyle!.Clear();
                break;
            case CollectionChange.ItemInserted:
                itemsControl?.GroupStyle!.Insert(index, s[index]);
                break;
            case CollectionChange.ItemRemoved:
                itemsControl?.GroupStyle!.RemoveAt(index);
                break;
            case CollectionChange.ItemChanged:
                if (itemsControl is not null)
                    itemsControl.GroupStyle![index] = s[index];
                break;
            default:
                throw new NotSupportedException();
        }
    }

    protected override void OnApplyTemplate()
    {
        base.OnApplyTemplate();
        //#if WINDOWS
        //        itemsControl = GetTemplateChild(PART_ItemsControl) as DropDownListBase;
        //#else
        itemsControl = GetTemplateChild(PART_ItemsControl) as ItemsControl;
        //#endif

        if (itemsControl is not null)
        {
            foreach (var groupStyle in this.GroupStyle)
            {
                itemsControl.GroupStyle?.Add(groupStyle);
            }
            //ctrl.FocusDisengaged += OnItemsControlFocusDisengaged;
            //ctrl.LostFocus += OnItemsControlLostFocus;
        }
    }

    private void OnItemsControlFocusDisengaged(Control sender, FocusDisengagedEventArgs args)
    {
        this.IsEditing = false;
        this.CommitCommand?.Execute(null);
    }

    private void OnItemsControlLostFocus(object sender, RoutedEventArgs e)
    {
        this.IsEditing = false;
        this.CommitCommand?.Execute(null);
    }
}
