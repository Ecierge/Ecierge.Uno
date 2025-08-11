using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Foundation.Collections;

namespace Ecierge.Uno.Controls;
public sealed partial class NullableNumberBox : Control
{
    private const string PartHeaderContentPresenter = "HeaderContentPresenter";

    #region Header

    /// <summary>
    /// Header Dependency Property
    /// </summary>
    public static readonly DependencyProperty HeaderProperty =
        DependencyProperty.Register(nameof(Header), typeof(object), typeof(NullableNumberBox),
            new PropertyMetadata((object?)null,
                new PropertyChangedCallback(OnHeaderChanged)));

    /// <summary>
    /// Gets or sets the Header property. This dependency property
    /// indicates header.
    /// </summary>
    public object? Header
    {
        get => (object?)GetValue(HeaderProperty);
        set => SetValue(HeaderProperty, value);
    }

    /// <summary>
    /// Handles changes to the Header property.
    /// </summary>
    private static void OnHeaderChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        NullableNumberBox target = (NullableNumberBox)d;
        target.SetHeaderVisibility();
    }

    #endregion

    #region HeaderTemplate

    /// <summary>
    /// HeaderTemplate Dependency Property
    /// </summary>
    public static readonly DependencyProperty HeaderTemplateProperty =
        DependencyProperty.Register(nameof(HeaderTemplate), typeof(DataTemplate), typeof(NullableNumberBox),
            new PropertyMetadata((DataTemplate?)null));

    /// <summary>
    /// Gets or sets the HeaderTemplate property. This dependency property
    /// indicates header template.
    /// </summary>
    public DataTemplate? HeaderTemplate
    {
        get => (DataTemplate?)GetValue(HeaderTemplateProperty);
        set => SetValue(HeaderTemplateProperty, value);
    }

    #endregion

    #region Value

    /// <summary>
    /// Value Dependency Property
    /// </summary>
    public static readonly DependencyProperty ValueProperty =
        DependencyProperty.Register(nameof(Value), typeof(double?), typeof(NullableNumberBox),
            new PropertyMetadata(null));

    /// <summary>
    /// Gets or sets the Value property. This dependency property
    /// indicates the numeric value.
    /// </summary>
    public double? Value
    {
        get => (double?)GetValue(ValueProperty);
        set => SetValue(ValueProperty, value);
    }

    #endregion

    #region IsValueSet

    /// <summary>
    /// IsValueSet Dependency Property
    /// </summary>

    public static readonly DependencyProperty IsValueSetProperty =
        DependencyProperty.Register(nameof(IsValueSet), typeof(bool), typeof(NullableNumberBox),
            new PropertyMetadata(false));

    /// <summary>
    /// Gets or sets the IsValueSet property. This dependency property
    /// </summary>
    public bool IsValueSet
    {
        get => (bool)GetValue(IsValueSetProperty);
        set => SetValue(IsValueSetProperty, value);
    }

    #endregion

    #region IsNumberBoxEnabled

    /// <summary>
    /// IsNumberBoxEnabled Dependency Property
    /// </summary>

    public static readonly DependencyProperty IsNumberBoxEnabledProperty =
        DependencyProperty.Register(nameof(IsNumberBoxEnabled), typeof(bool), typeof(NullableNumberBox),
            new PropertyMetadata(false));

    /// <summary>
    /// Gets or sets the IsValueSet property. This dependency property
    /// </summary>
    public bool IsNumberBoxEnabled
    {
        get => (bool)GetValue(IsNumberBoxEnabledProperty);
        set => SetValue(IsNumberBoxEnabledProperty, value);
    }

    #endregion

    #region CheckBoxCheckChanged

    /// <summary>
    /// CheckBoxCheckChanged Dependency Property
    /// </summary>
    public static readonly DependencyProperty IsCheckBoxCheckedProperty =
        DependencyProperty.Register(nameof(IsCheckBoxChecked), typeof(bool), typeof(NullableNumberBox),
            new PropertyMetadata(IsValueSetProperty, OnIsCheckBoxCheckedChanged));

    /// <summary>
    /// Gets or sets the IsCheckBoxChecked property. This dependency property
    /// </summary>
    public bool IsCheckBoxChecked
    {
        get => (bool)GetValue(IsCheckBoxCheckedProperty);
        set => SetValue(IsCheckBoxCheckedProperty, value);
    }

    /// <summary>
    /// Handles the logic to automatically set the Value property to 0.0 when the checkbox is checked.
    /// </summary>
    private static void OnIsCheckBoxCheckedChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is NullableNumberBox nullableNumberBox)
        {
            if (!nullableNumberBox.IsValueSet)
                nullableNumberBox.Value = (bool)e.NewValue ? 0.0 : nullableNumberBox.Value;
            nullableNumberBox.IsNumberBoxEnabled = (bool)e.NewValue;
        }
    }

    #endregion

    #region ResetCommand

    /// <summary>
    /// ResetCommand Dependency Property
    /// </summary>
    public static readonly DependencyProperty ResetCommandProperty =
        DependencyProperty.Register(nameof(ResetCommand), typeof(ICommand), typeof(NullableNumberBox),
            new PropertyMetadata((ICommand?)null));

    /// <summary>
    /// Gets or sets the ResetCommand property. This dependency property
    /// indicates command to reset the number box.
    /// </summary>
    public ICommand ResetCommand
    {
        get => (ICommand)GetValue(ResetCommandProperty);
        set => SetValue(ResetCommandProperty, value);
    }

    #endregion

    public NullableNumberBox()
    {
        DefaultStyleKey = typeof(NullableNumberBox);
    }

    protected override void OnApplyTemplate()
    {
        base.OnApplyTemplate();
        SetHeaderVisibility();
        if (Value != null)
        {
            IsValueSet = true;
            IsCheckBoxChecked = true;
        }
    }

    private void SetHeaderVisibility()
    {
        if (GetTemplateChild(PartHeaderContentPresenter) is FrameworkElement headerPresenter)
        {
            if (Header is string headerText)
            {
                headerPresenter.Visibility = string.IsNullOrEmpty(headerText)
                    ? Visibility.Collapsed
                    : Visibility.Visible;
            }
            else
            {
                headerPresenter.Visibility = Header != null
                    ? Visibility.Visible
                    : Visibility.Collapsed;
            }
        }
    }

}
