namespace Ecierge.Uno.Controls;

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Globalization.NumberFormatting;
using static CommunityToolkit.WinUI.TextBoxExtensions;

[TemplatePart(Name = PartHeaderContentPresenter, Type = typeof(FrameworkElement))]
[TemplatePart(Name = PartCheckBox, Type = typeof(CheckBox))]
[TemplatePart(Name = PartNumberBox, Type = typeof(NumberBox))]
public sealed partial class NullableNumberBox : Control
{
    private const string PartHeaderContentPresenter = "HeaderContentPresenter";
    private const string PartNumberBox = "NumberBox";
    private const string PartCheckBox = "CheckBox";

    private bool isChecked;
    private NumberBox? numberBox;
    private CheckBox? checkBox;

    public event TypedEventHandler<NullableNumberBox, NumberBoxValueChangedEventArgs>? ValueChanged;

    #region AcceptsExpression

    /// <summary>
    /// Identifies the AcceptsExpression dependency property.
    /// </summary>
    public static readonly DependencyProperty AcceptsExpressionProperty =
        DependencyProperty.Register(nameof(AcceptsExpression), typeof(bool), typeof(NullableNumberBox),
            new(NumberBox.AcceptsExpressionProperty.GetMetadata(typeof(NumberBox)).DefaultValue));

    /// <summary>
    /// Toggles whether the control will accept and evaluate a basic formulaic expression
    /// entered as input.
    /// </summary>
    public bool? AcceptsExpression
    {
        get => (bool?)GetValue(AcceptsExpressionProperty);
        set => SetValue(AcceptsExpressionProperty, value);
    }

    #endregion AcceptsExpression

    #region Header

    /// <summary>
    /// Header Dependency Property
    /// </summary>
    public static readonly DependencyProperty HeaderProperty =
        DependencyProperty.Register(nameof(Header), typeof(object), typeof(NullableNumberBox),
             new PropertyMetadata(NumberBox.HeaderProperty.GetMetadata(typeof(NumberBox)).DefaultValue));
           // new PropertyMetadata((object?)null, new PropertyChangedCallback(OnHeaderChanged)));

    /// <summary>
    /// Gets or sets the Header property. This dependency property
    /// indicates header.
    /// </summary>
    public object? Header
    {
        get => (object?)GetValue(HeaderProperty);
        set => SetValue(HeaderProperty, value);
    }

    #endregion

    #region HeaderTemplate

    /// <summary>
    /// HeaderTemplate Dependency Property
    /// </summary>
    public static readonly DependencyProperty HeaderTemplateProperty =
        DependencyProperty.Register(nameof(HeaderTemplate), typeof(DataTemplate), typeof(NullableNumberBox),
            new PropertyMetadata((DataTemplate?)NumberBox.HeaderTemplateProperty.GetMetadata(typeof(NumberBox)).DefaultValue));

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

    #region Description

    /// <summary>
    /// Description Dependency Property
    /// </summary>
    public static readonly DependencyProperty DescriptionProperty =
       DependencyProperty.Register(nameof(Description), typeof(object), typeof(NullableNumberBox),
           new PropertyMetadata(NumberBox.DescriptionProperty.GetMetadata(typeof(NumberBox)).DefaultValue));

    /// <summary>
    /// Gets or sets the Description property. This dependency property
    /// </summary>
    public object? Description
    {
        get => (object?)GetValue(DescriptionProperty);
        set => SetValue(DescriptionProperty, value);
    }

    #endregion

    #region IsEnabled1

    /// <summary>
    /// IsEnabled Dependency Property
    /// </summary>
    public static readonly DependencyProperty IsEnabled1Property =
        DependencyProperty.Register(nameof(IsEnabled1), typeof(bool), typeof(NullableNumberBox),
            new PropertyMetadata(NumberBox.IsEnabledProperty.GetMetadata(typeof(NumberBox)).DefaultValue));

    /// <summary>
    /// Gets or sets the IsEnabled property. This dependency property
    /// </summary>
    public bool IsEnabled1
    {
        get => (bool)GetValue(IsEnabled1Property);
        set => SetValue(IsEnabled1Property, value);
    }

    #endregion

    #region IsChecked

    /// <summary>
    /// CheckBoxChecked Dependency Property
    /// </summary>
    public static readonly DependencyProperty IsCheckBoxCheckedProperty =
        DependencyProperty.Register(nameof(IsChecked), typeof(bool), typeof(NullableNumberBox),
            new PropertyMetadata(false));

    /// <summary>
    /// Gets or sets the IsCheckBoxChecked property. This dependency property
    /// </summary>
    public bool IsChecked
    {
        get => (bool)GetValue(IsCheckBoxCheckedProperty);
        set => SetValue(IsCheckBoxCheckedProperty, value);
    }

    #endregion

    #region IsWrapEnabled

    /// <summary>
    /// IsWrapEnabled Dependency Property
    /// </summary>
    public static readonly DependencyProperty IsWrapEnabledProperty =
        DependencyProperty.Register(nameof(IsWrapEnabled), typeof(bool), typeof(NullableNumberBox),
            new PropertyMetadata(NumberBox.IsWrapEnabledProperty.GetMetadata(typeof(NumberBox)).DefaultValue));

    /// <summary>
    /// Gets or sets the IsWrapEnabled property. This dependency property
    /// </summary>
    public bool IsWrapEnabled
    {
        get => (bool)GetValue(IsWrapEnabledProperty);
        set => SetValue(IsWrapEnabledProperty, value);
    }

    #endregion

    #region LargeChange

    /// <summary>
    /// LargeChange Dependency Property
    /// </summary>
    public static readonly DependencyProperty LargeChangeProperty =
        DependencyProperty.Register(nameof(LargeChange), typeof(double), typeof(NullableNumberBox),
            new PropertyMetadata(NumberBox.LargeChangeProperty.GetMetadata(typeof(NumberBox)).DefaultValue));

    /// <summary>
    /// Gets or sets the LargeChange property. This dependency property
    /// </summary>
    public double LargeChange
    {
        get => (double)GetValue(LargeChangeProperty);
        set => SetValue(LargeChangeProperty, value);
    }

    #endregion

    #region SmallChange

    /// <summary>
    /// SmallChange Dependency Property
    /// </summary>
    public static readonly DependencyProperty SmallChangeProperty =
        DependencyProperty.Register(nameof(SmallChange), typeof(double), typeof(NullableNumberBox),
            new PropertyMetadata(NumberBox.SmallChangeProperty.GetMetadata(typeof(NumberBox)).DefaultValue));

    /// <summary>
    /// Gets or sets the SmallChange property. This dependency property
    /// </summary>
    public double SmallChange
    {
        get => (double)GetValue(SmallChangeProperty);
        set => SetValue(SmallChangeProperty, value);
    }

    #endregion

    #region Maximum

    /// <summary>
    /// Maximum Dependency Property
    /// </summary>
    public static readonly DependencyProperty MaximumProperty =
        DependencyProperty.Register(nameof(Maximum), typeof(double), typeof(NullableNumberBox),
            new PropertyMetadata(NumberBox.MaximumProperty.GetMetadata(typeof(NumberBox)).DefaultValue));

    /// <summary>
    /// Gets or sets the Maximum property. This dependency property
    /// </summary>
    public double Maximum
    {
        get => (double)GetValue(MaximumProperty);
        set => SetValue(MaximumProperty, value);
    }

    #endregion

    #region Minimum

    /// <summary>
    /// Maximum Dependency Property
    /// </summary>
    public static readonly DependencyProperty MinimumProperty =
        DependencyProperty.Register(nameof(Minimum), typeof(double), typeof(NullableNumberBox),
            new PropertyMetadata(NumberBox.MinimumProperty.GetMetadata(typeof(NumberBox)).DefaultValue));

    /// <summary>
    /// Gets or sets the Minimum property. This dependency property
    /// </summary>
    public double Minimum
    {
        get => (double)GetValue(MinimumProperty);
        set => SetValue(MinimumProperty, value);
    }

    #endregion

    #region NumberFormatter

    /// <summary>
    /// NumberFormatter Dependency Property
    /// </summary>
    public static readonly DependencyProperty NumberFormatterProperty =
        DependencyProperty.Register(nameof(NumberFormatter), typeof(INumberFormatter2), typeof(NullableNumberBox),
            new PropertyMetadata(CreateDefaultFormatter()));

    /// <summary>
    /// Gets or sets the NumberFormatter property. This dependency property
    /// </summary>
    public INumberFormatter2 NumberFormatter
    {
        get => (INumberFormatter2)GetValue(NumberFormatterProperty);
        set => SetValue(NumberFormatterProperty, value);
    }
    private static INumberFormatter2 CreateDefaultFormatter()
    => new DecimalFormatter();

    #endregion

    #region PlaceholderText

    /// <summary>
    /// PlaceholderText Dependency Property
    /// </summary>
    public static readonly DependencyProperty PlaceholderTextProperty =
        DependencyProperty.Register(nameof(PlaceholderText), typeof(string), typeof(NullableNumberBox),
            new PropertyMetadata(NumberBox.PlaceholderTextProperty.GetMetadata(typeof(NumberBox)).DefaultValue));

    /// <summary>
    /// Gets or sets the PlaceholderText property. This dependency property
    /// </summary>
    public string PlaceholderText
    {
        get => (string)GetValue(PlaceholderTextProperty);
        set => SetValue(PlaceholderTextProperty, value);
    }

    #endregion

    #region PreventKeyboardDisplayOnProgrammaticFocus

    /// <summary>
    /// Prevents the keyboard from displaying when the control is focused programmatically.
    /// </summary>
    ///
    public static readonly DependencyProperty PreventKeyboardDisplayOnProgrammaticFocusProperty =
        DependencyProperty.Register(nameof(PreventKeyboardDisplayOnProgrammaticFocus), typeof(bool), typeof(NullableNumberBox),
            new PropertyMetadata(NumberBox.PreventKeyboardDisplayOnProgrammaticFocusProperty.GetMetadata(typeof(NumberBox)).DefaultValue));

    /// <summary>
    /// Gets or sets the PreventKeyboardDisplayOnProgrammaticFocus property. This dependency property
    /// </summary>
    public bool PreventKeyboardDisplayOnProgrammaticFocus
    {
        get => (bool)GetValue(PreventKeyboardDisplayOnProgrammaticFocusProperty);
        set => SetValue(PreventKeyboardDisplayOnProgrammaticFocusProperty, value);
    }

    #endregion

    #region SelectionFlyout

    /// <summary>
    /// SelectionFlyout Dependency Property
    /// </summary>
    public static readonly DependencyProperty SelectionFlyoutProperty =
        DependencyProperty.Register(nameof(SelectionFlyout), typeof(FlyoutBase), typeof(NullableNumberBox),
            new PropertyMetadata(NumberBox.SelectionFlyoutProperty.GetMetadata(typeof(NumberBox)).DefaultValue));

    /// <summary>
    /// Gets or sets the SelectionFlyout property. This dependency property
    /// </summary>
    public FlyoutBase SelectionFlyout
    {
        get => (FlyoutBase)GetValue(SelectionFlyoutProperty);
        set => SetValue(SelectionFlyoutProperty, value);
    }

    #endregion

    #region SelectionHighlightColor

    /// <summary>
    /// SelectionHighlightColor Dependency Property
    /// </summary>
    public static readonly DependencyProperty SelectionHighlightColorProperty =
        DependencyProperty.Register(nameof(SelectionHighlightColor), typeof(SolidColorBrush), typeof(NullableNumberBox),
            new PropertyMetadata(NumberBox.SelectionHighlightColorProperty.GetMetadata(typeof(NumberBox)).DefaultValue));

    /// <summary>
    /// Gets or sets the SelectionHighlightColor property. This dependency property
    /// </summary>
    public SolidColorBrush SelectionHighlightColor
    {
        get => (SolidColorBrush)GetValue(SelectionHighlightColorProperty);
        set => SetValue(SelectionHighlightColorProperty, value);
    }

    #endregion

    #region SpinButtonPlacementMode

    /// <summary>
    /// SpinButtonPlacementMode Dependency Property
    /// </summary>Invoke
    public static readonly DependencyProperty SpinButtonPlacementModeProperty =
        DependencyProperty.Register(nameof(SpinButtonPlacementMode), typeof(NumberBoxSpinButtonPlacementMode), typeof(NullableNumberBox),
            new PropertyMetadata(NumberBox.SpinButtonPlacementModeProperty.GetMetadata(typeof(NumberBox)).DefaultValue));

    /// <summary>
    /// Gets or sets the SpinButtonPlacementMode property. This dependency property
    /// </summary>
    public NumberBoxSpinButtonPlacementMode SpinButtonPlacementMode
    {
        get => (NumberBoxSpinButtonPlacementMode)GetValue(SpinButtonPlacementModeProperty);
        set => SetValue(SpinButtonPlacementModeProperty, value);
    }

    #endregion

    #region Text

    /// <summary>
    /// Text Dependency Property
    /// </summary>
    public static readonly DependencyProperty TextProperty =
        DependencyProperty.Register(nameof(Text), typeof(string), typeof(NullableNumberBox),
            new PropertyMetadata(NumberBox.TextProperty.GetMetadata(typeof(NumberBox)).DefaultValue));

    /// <summary>
    /// Gets or sets the Text property. This dependency property
    /// </summary>
    public string Text
    {
        get => (string)GetValue(TextProperty);
        set => SetValue(TextProperty, value);
    }

    #endregion

    #region TextReadingOrder

    /// <summary>
    /// TextReadingOrder Dependency Property
    /// </summary>
    public static readonly DependencyProperty TextReadingOrderProperty =
        DependencyProperty.Register(nameof(TextReadingOrder), typeof(TextReadingOrder), typeof(NullableNumberBox),
            new PropertyMetadata(NumberBox.TextReadingOrderProperty.GetMetadata(typeof(NumberBox)).DefaultValue));

    /// <summary>
    /// Gets or sets the Text property. This dependency property
    /// </summary>
    public TextReadingOrder TextReadingOrder
    {
        get => (TextReadingOrder)GetValue(TextReadingOrderProperty);
        set => SetValue(TextReadingOrderProperty, value);
    }

    #endregion

    #region ValidationMode

    /// <summary>
    /// ValidationMode Dependency Property
    /// </summary>
    public static readonly DependencyProperty ValidationModeProperty =
        DependencyProperty.Register(nameof(ValidationMode), typeof(NumberBoxValidationMode), typeof(NullableNumberBox),
            new PropertyMetadata(NumberBox.ValidationModeProperty.GetMetadata(typeof(NumberBox)).DefaultValue));

    /// <summary>
    /// Gets or sets the ValidationMode property. This dependency property
    /// </summary>
    public NumberBoxValidationMode ValidationMode
    {
        get => (NumberBoxValidationMode)GetValue(ValidationModeProperty);
        set => SetValue(ValidationModeProperty, value);
    }

    #endregion
    public NullableNumberBox()
    {
        DefaultStyleKey = typeof(NullableNumberBox);
    }

    protected override void OnApplyTemplate()
    {
        base.OnApplyTemplate();
        this.numberBox = GetTemplateChild(PartNumberBox) as NumberBox;
        this.checkBox = GetTemplateChild(PartCheckBox) as CheckBox;
        if (checkBox is not null && numberBox is not null)
        {
            if (Value is not null)
            {
                checkBox.IsChecked = true;
                IsEnabled1 = true;
                numberBox.Value = Value.Value;
            }
            else
            {
                checkBox.IsChecked = false;
                IsEnabled1 = false;
            }
            checkBox.Checked += (s, e) =>
            {
                if (numberBox is not null)
                {
                    IsEnabled1 = true;
                    Value = numberBox.Value;
                }
            };
            checkBox.Unchecked += (s, e) =>
            {
                if (numberBox is not null)
                {
                    IsEnabled1 = false;
                    Value = null;
                }
            };
        }

        if (numberBox != null)
        {
            numberBox.ValueChanged += (s, e) =>
            {
                Value = numberBox.Value;
                ValueChanged?.Invoke(this, e);
            };
        }
        if (GetTemplateChild(PartHeaderContentPresenter) is FrameworkElement headerPresenter && checkBox is not null)
        {

            if (Header is string headerText1 && Description is string descriptionText1)
            {
                checkBox.Margin = new Microsoft.UI.Xaml.Thickness(8, 6, 0, 0); // пример, можно подстроить
            }
            else if (Header is string headerText)
            {
                checkBox.Margin = new Microsoft.UI.Xaml.Thickness(8, 24, 0, 0);
            }
            else if (Description is string descriptionText)
            {
                checkBox.Margin = new Microsoft.UI.Xaml.Thickness(8, 0, 0, 22);
            }
        }
    }
}
