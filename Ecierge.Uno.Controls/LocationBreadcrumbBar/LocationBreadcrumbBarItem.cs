// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference BreadcrumbBarItem.cpp, tag winui3/release/1.5.3, commit 2a60e27

#nullable enable


using System.Collections.ObjectModel;
using Uno.Disposables;
using VirtualKey = Windows.System.VirtualKey;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft/* UWP don't rename */.UI.Xaml.Automation.Peers;
using Microsoft.UI.Dispatching;
#if !HAS_UNO_WINUI // Avoid duplicate using for WinUI build
using Microsoft.UI.Xaml.Automation.Peers;
#endif
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Content;

#if HAS_UNO_WINUI
using Microsoft.UI.Input;
using PointerDeviceType = Microsoft.UI.Input.PointerDeviceType;
using System;

#else
using PointerDeviceType = Windows.Devices.Input.PointerDeviceType;
#endif

#if !HAS_UNO
using IElementFactoryShim = Microsoft.UI.Xaml.IElementFactory;
#else
using Uno.UI.DataBinding;
#endif

namespace Ecierge.Uno.Controls.LocationBreadcrumbBar;

/// <summary>
/// Represents an item in a BreadcrumbBar control.
/// </summary>
public partial class LocationBreadcrumbBarItem : ComboBox
{
    /// <summary>
    /// Initializes a new instance of the BreadcrumbBarItem class.
    /// </summary>
    public LocationBreadcrumbBarItem()
    {
        //__RP_Marker_ClassById(RuntimeProfiler.ProfId_BreadcrumbBarItem);

        DefaultStyleKey = typeof(LocationBreadcrumbBarItem);

#if HAS_UNO
        // Uno specific: We use Unloaded instead of destructor
        Loaded += LocationBreadcrumbBarItem_Loaded;
        Unloaded += LocationBreadcrumbBarItem_Unloaded;
#endif
    }

#if HAS_UNO
    private void LocationBreadcrumbBarItem_Loaded(object sender, RoutedEventArgs e)
    {
        HookListeners(m_isEllipsisDropDownItem);
    }

    private void LocationBreadcrumbBarItem_Unloaded(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        RevokeListeners();
        m_ellipsisRepeaterElementPreparedRevoker.Disposable = null;
        m_ellipsisRepeaterElementIndexChangedRevoker.Disposable = null;
    }
#endif

    private void HookListeners(bool forEllipsisDropDownItem)
    {
#if HAS_UNO
        RevokeListeners();
#endif
        if (m_childPreviewKeyDownToken.Disposable == null)
        {
            PreviewKeyDown += OnChildPreviewKeyDown;
            PreviewKeyUp += OnChildPreviewKeyUp;
            m_childPreviewKeyDownToken.Disposable = Disposable.Create(() =>
            {
                PreviewKeyDown += OnChildPreviewKeyDown;
                PreviewKeyUp += OnChildPreviewKeyUp;
            });
        }

        if (forEllipsisDropDownItem)
        {
            if (m_isEnabledChangedRevoker.Disposable == null)
            {
                m_isEnabledChangedRevoker.Disposable = Disposable.Create(() =>
                {
                    IsEnabledChanged -= OnIsEnabledChanged;
                });
                IsEnabledChanged += OnIsEnabledChanged;
            }
        }
        else if (m_flowDirectionChangedToken == null)
        {
            m_flowDirectionChangedToken = RegisterPropertyChangedCallback(FrameworkElement.FlowDirectionProperty, OnFlowDirectionChanged);
        }
    }

    private void RevokeListeners()
    {
        if (m_flowDirectionChangedToken != null)
        {
            UnregisterPropertyChangedCallback(FrameworkElement.FlowDirectionProperty, m_flowDirectionChangedToken.Value);
            m_flowDirectionChangedToken = null;
        }

        if (m_childPreviewKeyDownToken.Disposable != null)
        {
            m_childPreviewKeyDownToken.Disposable = null;
        }

        m_keyDownRevoker.Disposable = null;
    }

    private void RevokePartsListeners()
    {
        m_buttonLoadedRevoker.Disposable = null;
        m_buttonClickRevoker.Disposable = null;
        m_ellipsisRepeaterElementPreparedRevoker.Disposable = null;
        m_ellipsisRepeaterElementIndexChangedRevoker.Disposable = null;
        m_isPressedButtonRevoker.Disposable = null;
        m_isPointerOverButtonRevoker.Disposable = null;
        m_isEnabledButtonRevoker.Disposable = null;
    }

    protected override void OnApplyTemplate()
    {
        base.OnApplyTemplate();

        // Custom ComboBox creating
        if (m_isEllipsisDropDownItem)
        {
            UpdateEllipsisDropDownItemCommonVisualState(false /*useTransitions*/);
        }
        else
        {
            RevokePartsListeners();
            //IControlProtected controlProtected{ this };

            if (m_isEllipsisItem)
            {
#if !HAS_UNO
				m_ellipsisFlyout = (Flyout)GetTemplateChild(s_itemEllipsisFlyoutPartName);
#else
                var rootGrid = GetTemplateChild("PART_LayoutRoot") as FrameworkElement;
                if (rootGrid is not null)
                {
                    m_ellipsisFlyout = (Flyout)rootGrid.Resources[s_itemEllipsisFlyoutPartName];
                }
                m_ellipsisFlyout = (Flyout)GetTemplateChild(s_itemEllipsisFlyoutPartName);

#endif
            }
            else
            {
                m_comboBox = GetTemplateChild("PART_ItemComboBox") as ComboBox;
            }

            m_button = (Button)GetTemplateChild(s_itemButtonPartName);


            if (m_button is { } button)
            {
                m_buttonLoadedRevoker.Disposable = Disposable.Create(() =>
                {
                    button.Loaded -= OnLoadedEvent;
                });
                button.Loaded += OnLoadedEvent;

                var isPressedToken = RegisterPropertyChangedCallback(ButtonBase.IsPressedProperty, OnVisualPropertyChanged);
                m_isPressedButtonRevoker.Disposable = Disposable.Create(() =>
                {
                    UnregisterPropertyChangedCallback(ButtonBase.IsPressedProperty, isPressedToken);
                });

                var isPointerOverToken = RegisterPropertyChangedCallback(ButtonBase.IsPointerOverProperty, OnVisualPropertyChanged);
                m_isPointerOverButtonRevoker.Disposable = Disposable.Create(() =>
                {
                    UnregisterPropertyChangedCallback(ButtonBase.IsPointerOverProperty, isPointerOverToken);
                });

                var isEnabledToken = RegisterPropertyChangedCallback(Control.IsEnabledProperty, OnVisualPropertyChanged);
                m_isEnabledButtonRevoker.Disposable = Disposable.Create(() =>
                {
                    UnregisterPropertyChangedCallback(Control.IsEnabledProperty, isEnabledToken);
                });
            }

            //m_comboBox!.SelectedItem = "FolderFolder";

            UpdateButtonCommonVisualState(false /*useTransitions*/);
            UpdateInlineItemTypeVisualState(false /*useTransitions*/);
        }

        UpdateItemTypeVisualState();

#if HAS_UNO
        _fullyInitialized = true;
#endif

    }

    private void OnLoadedEvent(object sender, RoutedEventArgs args)
    {
        if (m_isEllipsisDropDownItem)
        {
            throw new InvalidOperationException("m_isEllipsisDropDownItem must be false");
        }

        m_buttonLoadedRevoker.Disposable = null;

        if (m_button is { } button)
        {
            m_buttonClickRevoker.Disposable = null;
            if (m_isEllipsisItem)
            {
                m_buttonClickRevoker.Disposable = Disposable.Create(() =>
                {
                    button.Click -= OnEllipsisItemClick;
                });
                button.Click += OnEllipsisItemClick;
            }
            else
            {
                m_buttonClickRevoker.Disposable = Disposable.Create(() =>
                {
                    button.Click -= OnLocationBreadcrumbBarItemClick;
                });
                button.Click += OnLocationBreadcrumbBarItemClick;
            }
        }

        if (m_isEllipsisItem)
        {
            SetPropertiesForEllipsisItem();
        }
        else if (m_isLastItem)
        {
            SetPropertiesForLastItem();
        }
        else
        {
            ResetVisualProperties();
        }
    }


    internal void SetParentBreadcrumb(LocationBreadcrumbBar parent)
    {
        if (m_isEllipsisDropDownItem)
        {
            throw new InvalidOperationException("m_isEllipsisDropDownItem must be false");
        }
#if HAS_UNO
        m_parentBreadcrumb = WeakReferencePool.RentWeakReference(this, parent);
#else
        m_parentBreadcrumb = new WeakReference(parent);
#endif
    }

    internal void SetEllipsisDropDownItemDataTemplate(object? newDataTemplate)
    {
        if (newDataTemplate is IElementFactoryShim dataTemplate)
        {
            m_ellipsisDropDownItemDataTemplate = dataTemplate;
        }
        else if (newDataTemplate == null)
        {
            m_ellipsisDropDownItemDataTemplate = null;
        }
    }

    internal void SetIndex(int index)
    {
        m_index = index;
    }

    internal void SetIsEllipsisDropDownItem(bool isEllipsisDropDownItem)
    {
        m_isEllipsisDropDownItem = isEllipsisDropDownItem;

        HookListeners(m_isEllipsisDropDownItem);

        UpdateItemTypeVisualState();
    }

    private void RaiseItemClickedEvent(object content, int index)
    {
        if (m_parentBreadcrumb?.Target is LocationBreadcrumbBar breadcrumb)
        {
            var breadcrumbImpl = breadcrumb;
            breadcrumbImpl.RaiseItemClickedEvent(content, index);
        }
    }

    private void OnLocationBreadcrumbBarItemClick(object? sender, RoutedEventArgs? args)
    {
        RaiseItemClickedEvent(SelectedItem, m_index - 1);
    }

    void OnFlyoutElementPreparedEvent(ItemsRepeater sender, ItemsRepeaterElementPreparedEventArgs args)
    {
        if (m_isEllipsisDropDownItem)
        {
            throw new InvalidOperationException("m_isEllipsisDropDownItem must be false");
        }
        if (args.Element is LocationBreadcrumbBarItem ellipsisDropDownItem)
        {
            DispatcherQueue.TryEnqueue(DispatcherQueuePriority.Normal, () => VisualStateManager.GoToState(ellipsisDropDownItem, s_lastItemStateName, useTransitions: false));
        }

        UpdateFlyoutIndex(args.Element, args.Index);
    }

    void OnFlyoutElementIndexChangedEvent(ItemsRepeater itemsRepeater, ItemsRepeaterElementIndexChangedEventArgs args)
    {
        if (m_isEllipsisDropDownItem)
        {
            throw new InvalidOperationException("m_isEllipsisDropDownItem must be false");
        }

        UpdateFlyoutIndex(args.Element, args.NewIndex);
    }

    void OnFlowDirectionChanged(DependencyObject sender, DependencyProperty property)
    {
        if (m_isEllipsisDropDownItem)
        {
            throw new InvalidOperationException("m_isEllipsisDropDownItem must be false");
        }

        UpdateInlineItemTypeVisualState(true /*useTransitions*/);
    }

    private T FindChild<T>(DependencyObject parent, string childName) where T : DependencyObject
    {
        if (parent == null) return default;

        for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
        {
            var child = VisualTreeHelper.GetChild(parent, i);
            if (child is T typedChild && ((FrameworkElement)child).Name == childName)
            {
                return typedChild;
            }

            var result = FindChild<T>(child, childName);
            if (result != null) return result;
        }

        return default;
    }
    private void ComboBox_DropDownClosed(object sender, object e)
    {
        var comboBox = sender as ComboBox;
        if (comboBox != null)
        {
            comboBox.DropDownClosed -= ComboBox_DropDownClosed;

            _lastFocusedElement?.Focus(FocusState.Programmatic);
        }
    }

    private void OnChildPreviewKeyDown(object sender, KeyRoutedEventArgs args)
    {
        var comboBox = FindChild<ComboBox>(this, "PART_ItemComboBox");

        if (_isKeyPressed) return;

        if (args.Key == VirtualKey.F4 || args.Key == VirtualKey.Enter || args.Key == VirtualKey.Space)
        {
            
            _isKeyPressed = true;
            var focusedElement = sender as FrameworkElement;
            if (focusedElement != null)
            {
                System.Diagnostics.Debug.WriteLine($"Element with name '{focusedElement.Name}' got focus.");
            }

            if (m_isEllipsisDropDownItem)
            {
                if (!comboBox.IsDropDownOpen)
                {
                    this.OnClickEvent(sender, null);
                    args.Handled = true;
                }
            }
            else
            {
                _lastFocusedElement = sender as FrameworkElement;

                if (m_isEllipsisItem)
                {
                    OnEllipsisItemClick(null, null);
                    if (args.Key == VirtualKey.Tab) throw new Exception();
                }
                else if (comboBox != null)
                {
                    if (!comboBox.IsDropDownOpen)
                    {
                        comboBox.Focus(FocusState.Programmatic);
                        comboBox.IsDropDownOpen = true;
                        comboBox.DropDownClosed += ComboBox_DropDownClosed;
                    }
                    else
                    {
                        comboBox.IsDropDownOpen = false;
                    }
                    if (args.Key == VirtualKey.Tab) throw new Exception();
                    args.Handled = true;
                }
            }
        }
    }

    private void OnChildPreviewKeyUp(object sender, KeyRoutedEventArgs args)
    {
        if (args.Key == VirtualKey.F4 || args.Key == VirtualKey.Enter || args.Key == VirtualKey.Space)
        {
            _isKeyPressed = false; 
            args.Handled = true;   
        }
    }

    private void OnIsEnabledChanged(
         object sender,
         DependencyPropertyChangedEventArgs args)
    {
        if (m_isEllipsisDropDownItem)
        {
            throw new InvalidOperationException("m_isEllipsisDropDownItem must be false");
        }

        UpdateEllipsisDropDownItemCommonVisualState(true /*useTransitions*/);
    }

    private void UpdateFlyoutIndex(UIElement element, int index)
    {
        if (m_isEllipsisDropDownItem)
        {
            throw new InvalidOperationException("m_isEllipsisDropDownItem must be false");
        }

        if (m_ellipsisItemsRepeater is { } ellipsisItemsRepeater)
        {
            if (ellipsisItemsRepeater.ItemsSourceView is { } itemSourceView)
            {
                int itemCount = itemSourceView.Count;

                if (element is LocationBreadcrumbBarItem ellipsisDropDownItemImpl)
                {
                    ellipsisDropDownItemImpl.SetEllipsisItem(this);
                    ellipsisDropDownItemImpl.SetIndex(itemCount - index);
                }

                element.SetValue(AutomationProperties.PositionInSetProperty, index + 1);
                element.SetValue(AutomationProperties.SizeOfSetProperty, itemCount);
            }
        }
    }

    private object CloneEllipsisItemSource(ObservableCollection<object> ellipsisItemsSource)
    {
        if (m_isEllipsisDropDownItem)
        {
            throw new InvalidOperationException("m_isEllipsisDropDownItem must be false");
        }

        // A copy of the hidden elements array in BreadcrumbLayout is created
        // to avoid getting a Layout cycle exception
        var newItemsSource = new ObservableCollection<object>();

        // The new list contains all the elements in reverse order
        int itemsSourceSize = ellipsisItemsSource.Count;
        if (itemsSourceSize > 0)
        {
            for (int i = itemsSourceSize - 1; i >= 0; --i)
            {
                var item = ellipsisItemsSource[i];
                newItemsSource.Add(item);
            }
        }

        return newItemsSource;
    }

    private void OpenFlyout()
    {
        if (m_isEllipsisDropDownItem)
        {
            throw new InvalidOperationException("m_isEllipsisDropDownItem must be false");
        }

        if (m_ellipsisFlyout is { } flyout)
        {
            FlyoutShowOptions options = new();
            flyout.ShowAt(this, options);
        }

    }

    private void CloseFlyout()
    {
        if (m_isEllipsisDropDownItem)
        {
            throw new InvalidOperationException("m_isEllipsisDropDownItem must be false");
        }

        if (m_ellipsisFlyout is { } flyout)
        {
            flyout.Hide();
        }
    }

    private void OnVisualPropertyChanged(DependencyObject sender, DependencyProperty property)
    {
        if (m_isEllipsisDropDownItem)
        {
            throw new InvalidOperationException("m_isEllipsisDropDownItem must be false");
        }

        UpdateButtonCommonVisualState(true /*useTransitions*/);
    }

    private void UpdateItemTypeVisualState()
    {
        VisualStateManager.GoToState(this, m_isEllipsisDropDownItem ? s_lastItemStateName : s_lastItemStateName, false /*useTransitions*/);
    }

    private void UpdateEllipsisDropDownItemCommonVisualState(bool useTransitions)
    {
        //if (m_isEllipsisDropDownItem)
        //{
        //    throw new InvalidOperationException("m_isEllipsisDropDownItem must be false");
        //}

        string commonVisualStateName = s_currentStateName;

        if (!IsEnabled)
        {
            commonVisualStateName = s_disabledStateName;
        }
        else if (m_isPressed)
        {
            commonVisualStateName = s_pressedStateName;
        }
        else if (m_isPointerOver)
        {
            commonVisualStateName = s_pointerOverStateName;
        }
        else
        {
            commonVisualStateName = s_normalStateName;
        }

        VisualStateManager.GoToState(this, commonVisualStateName, useTransitions);
    }

    private void UpdateInlineItemTypeVisualState(bool useTransitions)
    {
        if (m_isEllipsisDropDownItem)
        {
            throw new InvalidOperationException("m_isEllipsisDropDownItem must be false");
        }

        bool isLeftToRight = (FlowDirection == FlowDirection.LeftToRight);
        string visualStateName;

        if (m_isEllipsisItem)
        {
            if (isLeftToRight)
            {
                visualStateName = s_ellipsisStateName;
            }
            else
            {
                visualStateName = s_ellipsisRTLStateName;
            }
        }
        else if (m_isLastItem)
        {
            visualStateName = s_lastItemStateName;
        }
        else if (isLeftToRight)
        {
            visualStateName = s_defaultStateName;
        }
        else
        {
            visualStateName = s_defaultRTLStateName;
        }

        VisualStateManager.GoToState(this, visualStateName, useTransitions);
    }

    private void UpdateButtonCommonVisualState(bool useTransitions)
    {
        if (m_isEllipsisDropDownItem)
        {
            throw new InvalidOperationException("m_isEllipsisDropDownItem must be false");
        }

        if (m_button is { } button)
        {
            string commonVisualStateName = "";

            // If is last item: place Current as prefix for visual state
            if (m_isLastItem)
            {
                commonVisualStateName = s_currentStateName;
            }

            if (!button.IsEnabled)
            {
                commonVisualStateName = commonVisualStateName + s_disabledStateName;
            }
            else if (button.IsPressed)
            {
                commonVisualStateName = commonVisualStateName + s_pressedStateName;
            }
            else if (button.IsPointerOver)
            {
                commonVisualStateName = commonVisualStateName + s_pointerOverStateName;
            }
            else
            {
                commonVisualStateName = commonVisualStateName + s_normalStateName;
            }

            VisualStateManager.GoToState(button, commonVisualStateName, useTransitions);
        }
    }

    private void OnEllipsisItemClick(object? sender, RoutedEventArgs? args)
    {
        if (m_isEllipsisDropDownItem)
        {
            throw new InvalidOperationException("m_isEllipsisDropDownItem must be false");
        }

        if (m_parentBreadcrumb?.Target is { } breadcrumb)
        {
            if (breadcrumb is LocationBreadcrumbBar breadcrumbImpl)
            {
                var hiddenElements = CloneEllipsisItemSource(breadcrumbImpl.HiddenElements());

                if (m_ellipsisDropDownItemDataTemplate is { } dataTemplate)
                {
                    m_ellipsisElementFactory?.UserElementFactory(dataTemplate);
                }

                if (m_ellipsisItemsRepeater is { } flyoutRepeater)
                {
                    flyoutRepeater.ItemsSource = hiddenElements;
                }
                OpenFlyout();
            }
        }
    }

    internal void SetPropertiesForLastItem()
    {
        if (m_isEllipsisDropDownItem)
        {
            throw new InvalidOperationException("m_isEllipsisDropDownItem must be false");
        }

        m_isEllipsisItem = false;
        m_isLastItem = true;
        
        UpdateButtonCommonVisualState(false /*useTransitions*/);
        UpdateInlineItemTypeVisualState(false /*useTransitions*/);
    }

    internal void ResetVisualProperties()
    {
        if (m_isEllipsisDropDownItem)
        {
            UpdateEllipsisDropDownItemCommonVisualState(false /*useTransitions*/);
        }
        else
        {
            m_isEllipsisItem = false;
            m_isLastItem = false;

            if (m_button is { } button)
            {
                button.Flyout = null;
            }
            m_ellipsisFlyout = null;
            m_ellipsisItemsRepeater = null;
            m_ellipsisElementFactory = null;

            UpdateButtonCommonVisualState(false /*useTransitions*/);
            UpdateInlineItemTypeVisualState(false /*useTransitions*/);
        }
    }

    private void InstantiateFlyout()
    {
        if (m_isEllipsisDropDownItem)
        {
            throw new InvalidOperationException("m_isEllipsisDropDownItem must be false");
        }

        // Only if the element has been created visually, instantiate the flyout
        if (m_button is { } button)
        {
            if (m_ellipsisFlyout is { } ellipsisFlyout)
            {
                // Create ItemsRepeater and set the DataTemplate
                var ellipsisItemsRepeater = new ItemsRepeater();
                ellipsisItemsRepeater.Name = s_ellipsisItemsRepeaterPartName;
                AutomationProperties.SetName(ellipsisItemsRepeater, s_ellipsisItemsRepeaterAutomationName);
                ellipsisItemsRepeater.HorizontalAlignment = HorizontalAlignment.Stretch;

                ellipsisItemsRepeater.Layout = new StackLayout();

                m_ellipsisElementFactory = new LocationBreadcrumbElementFactory();
                ellipsisItemsRepeater.ItemTemplate = m_ellipsisElementFactory;

                if (m_ellipsisDropDownItemDataTemplate is { } dataTemplate)
                {
                    m_ellipsisElementFactory.UserElementFactory(dataTemplate);
                }
                
                m_ellipsisRepeaterElementPreparedRevoker.Disposable = Disposable.Create(() =>
                {
                    ellipsisItemsRepeater.ElementPrepared -= OnFlyoutElementPreparedEvent;
                });
                ellipsisItemsRepeater.ElementPrepared += OnFlyoutElementPreparedEvent;

                m_ellipsisRepeaterElementIndexChangedRevoker.Disposable = Disposable.Create(() =>
                {
                    ellipsisItemsRepeater.ElementIndexChanged -= OnFlyoutElementIndexChangedEvent;
                });
                ellipsisItemsRepeater.ElementIndexChanged += OnFlyoutElementIndexChangedEvent;

                m_ellipsisItemsRepeater = ellipsisItemsRepeater;

                // Set the repeater as the content.
                AutomationProperties.SetName(ellipsisFlyout, s_ellipsisFlyoutAutomationName);
                ellipsisFlyout.Content = ellipsisItemsRepeater;
                ellipsisFlyout.Placement = FlyoutPlacementMode.Bottom;


                m_ellipsisFlyout = ellipsisFlyout;
            }
        }
    }

    internal void SetPropertiesForEllipsisItem()
    {
        if (m_isEllipsisDropDownItem)
        {
            throw new InvalidOperationException("m_isEllipsisDropDownItem must be false");
        }

        m_isEllipsisItem = true;
        m_isLastItem = false;

        InstantiateFlyout();

        UpdateButtonCommonVisualState(false /*useTransitions*/);
        UpdateInlineItemTypeVisualState(false /*useTransitions*/);
    }

    private void SetEllipsisItem(LocationBreadcrumbBarItem ellipsisItem)
    {
        if (m_isEllipsisDropDownItem)
        {
            throw new InvalidOperationException("m_isEllipsisDropDownItem must be false");
        }
        m_ellipsisItem = ellipsisItem;
    }

    protected override AutomationPeer OnCreateAutomationPeer() =>
        new LocationBreadcrumbBarItemAutomationPeer(this);

    protected override void OnPointerEntered(PointerRoutedEventArgs args)
    {
        base.OnPointerEntered(args);

        if (m_isEllipsisDropDownItem)
        {
            ProcessPointerOver(args);
        }
    }

    protected override void OnPointerMoved(PointerRoutedEventArgs args)
    {
        base.OnPointerMoved(args);

        if (m_isEllipsisDropDownItem)
        {
            ProcessPointerOver(args);
        }
    }

    protected override void OnPointerExited(PointerRoutedEventArgs args)
    {
        base.OnPointerExited(args);

        if (m_isEllipsisDropDownItem)
        {
            ProcessPointerCanceled(args);
        }
    }

    protected override void OnPointerPressed(PointerRoutedEventArgs args)
    {
        base.OnPointerPressed(args);

        if (m_isEllipsisDropDownItem)
        {
            if (IgnorePointerId(args))
            {
                return;
            }
            if (m_isPressed)
            {
                throw new InvalidOperationException("m_isPressed must be false");
            }

            if (args.Pointer.PointerDeviceType == Microsoft.UI.Input.PointerDeviceType.Mouse)
            {
                var pointerProperties = args.GetCurrentPoint(this).Properties;
                m_isPressed = pointerProperties.IsLeftButtonPressed;
            }
            else
            {
                m_isPressed = true;
            }

            if (m_isPressed)
            {
                UpdateEllipsisDropDownItemCommonVisualState(true /*useTransitions*/);
            }
        }
    }

    protected override void OnPointerReleased(PointerRoutedEventArgs args)
    {
        base.OnPointerReleased(args);

        if (m_isEllipsisDropDownItem)
        {
            if (IgnorePointerId(args))
            {
                return;
            }

            if (m_isPressed)
            {
                m_isPressed = false;
                UpdateEllipsisDropDownItemCommonVisualState(true /*useTransitions*/);
                OnClickEvent(null, null);
            }
        }
    }

    protected override void OnPointerCanceled(PointerRoutedEventArgs args)
    {
        base.OnPointerCanceled(args);

        if (m_isEllipsisDropDownItem)
        {
            ProcessPointerCanceled(args);
        }
    }

    protected override void OnPointerCaptureLost(PointerRoutedEventArgs args)
    {
        base.OnPointerCaptureLost(args);

        if (m_isEllipsisDropDownItem)
        {
            ProcessPointerCanceled(args);
        }
    }

    private void ProcessPointerOver(PointerRoutedEventArgs args)
    {
        if (m_isEllipsisDropDownItem)
        {
            throw new InvalidOperationException("m_isEllipsisDropDownItem must be false");
        }

        if (IgnorePointerId(args))
        {
            return;
        }

        if (!m_isPointerOver)
        {
            m_isPointerOver = true;
            UpdateEllipsisDropDownItemCommonVisualState(true /*useTransitions*/);
        }
    }

    private void ProcessPointerCanceled(PointerRoutedEventArgs args)
    {
        if (m_isEllipsisDropDownItem)
        {
            throw new InvalidOperationException("m_isEllipsisDropDownItem must be false");
        }
        
        if (IgnorePointerId(args))
        {
            return;
        }

        m_isPressed = false;
        m_isPointerOver = false;
        ResetTrackedPointerId();
        UpdateEllipsisDropDownItemCommonVisualState(true /*useTransitions*/);
    }

    void ResetTrackedPointerId()
    {
        if (m_isEllipsisDropDownItem)
        {
            throw new InvalidOperationException("m_isEllipsisDropDownItem must be false");
        }

        m_trackedPointerId = 0;
    }

    // Returns False when the provided pointer Id matches the currently tracked Id.
    // When there is no currently tracked Id, sets the tracked Id to the provided Id and returns False.
    // Returns True when the provided pointer Id does not match the currently tracked Id.
    private bool IgnorePointerId(PointerRoutedEventArgs args)
    {
        if (m_isEllipsisDropDownItem)
        {
            throw new InvalidOperationException("m_isEllipsisDropDownItem must be false");
        }

        uint pointerId = args.Pointer.PointerId;

        if (m_trackedPointerId == 0)
        {
            m_trackedPointerId = pointerId;
        }
        else if (m_trackedPointerId != pointerId)
        {
            return true;
        }
        return false;
    }

    internal void OnClickEvent(object? sender, RoutedEventArgs? args)
    {
        if (m_isEllipsisDropDownItem)
        {
            if (m_ellipsisItem is { } ellipsisItem)
            {
                // Once an element has been clicked, close the flyout
                if (ellipsisItem is { } ellipsisItemImpl)
                {
                    ellipsisItemImpl.CloseFlyout();
                    ellipsisItemImpl.RaiseItemClickedEvent(SelectedItem, m_index - 1);
                }
            }
        }
        else if (m_isEllipsisItem)
        {
            OnEllipsisItemClick(null, null);
        }
        else
        {
            OnLocationBreadcrumbBarItemClick(null, null);
        }
    }

#if HAS_UNO
	// TODO: Uno specific: Remove when #4689 is fixed

	protected bool _fullyInitialized = false;

	internal void Reinitialize()
	{
		OnApplyTemplate();
		//UpdateVisualState(false);
		OnLoadedEvent(this, new RoutedEventArgs());
	}
#endif
}
