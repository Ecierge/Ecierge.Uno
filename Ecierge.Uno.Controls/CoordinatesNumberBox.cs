namespace Ecierge.Uno.Controls;

public sealed partial class CoordinatesNumberBox : Control
{
    private const string PartHeaderContentPresenter = "HeaderContentPresenter";

    #region Header

    /// <summary>
    /// Header Dependency Property
    /// </summary>
    public static readonly DependencyProperty HeaderProperty =
        DependencyProperty.Register(nameof(Header), typeof(object), typeof(CoordinatesNumberBox),
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
        CoordinatesNumberBox target = (CoordinatesNumberBox)d;
        //object? oldHeader = (object?)e.OldValue;
        //object? newHeader = (object?)e.NewValue;
        target.SetHeaderVisibility();
        //target.OnHeaderChanged(oldHeader, newHeader);
    }

    ///// <summary>
    ///// Provides derived classes an opportunity to handle changes to the Header property.
    ///// </summary>
    //protected virtual void OnHeaderChanged(object? oldHeader, object? newHeader)
    //{
    //}

    #endregion

    #region HeaderTemplate

    /// <summary>
    /// HeaderTemplate Dependency Property
    /// </summary>
    public static readonly DependencyProperty HeaderTemplateProperty =
        DependencyProperty.Register(nameof(HeaderTemplate), typeof(DataTemplate), typeof(CoordinatesNumberBox),
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

    #region SetCurrentPositionCommand

    /// <summary>
    /// SetCurrentPositionCommand Dependency Property
    /// </summary>
    public static readonly DependencyProperty SetCurrentPositionCommandProperty =
        DependencyProperty.Register(nameof(SetCurrentPositionCommand), typeof(ICommand), typeof(CoordinatesNumberBox),
            new PropertyMetadata((ICommand?)null));

    /// <summary>
    /// Gets or sets the SetCurrentPositionCommand property. This dependency property
    /// indicates set current position command.
    /// </summary>
    public ICommand SetCurrentPositionCommand
    {
        get => (ICommand)GetValue(SetCurrentPositionCommandProperty);
        set => SetValue(SetCurrentPositionCommandProperty, value);
    }

    #endregion

    #region SelectPositionCommand

    /// <summary>
    /// SelectPositionCommand Dependency Property
    /// </summary>
    public static readonly DependencyProperty SelectPositionCommandProperty =
        DependencyProperty.Register(nameof(SelectPositionCommand), typeof(ICommand), typeof(CoordinatesNumberBox),
            new PropertyMetadata((ICommand?)null));

    /// <summary>
    /// Gets or sets the SelectPositionCommand property. This dependency property
    /// indicates command to select position.
    /// </summary>
    public ICommand SelectPositionCommand
    {
        get => (ICommand)GetValue(SelectPositionCommandProperty);
        set => SetValue(SelectPositionCommandProperty, value);
    }

    #endregion

    #region Latitude

    /// <summary>
    /// Latitude Dependency Property
    /// </summary>
    public static readonly DependencyProperty LatitudeProperty =
        DependencyProperty.Register(nameof(Latitude), typeof(double), typeof(CoordinatesNumberBox),
            new PropertyMetadata((double)0));

    /// <summary>
    /// Gets or sets the Lat property. This dependency property
    /// indicates north-south position on the Earth's surface.
    /// </summary>
    public double Latitude
    {
        get => (double)GetValue(LatitudeProperty);
        set => SetValue(LatitudeProperty, value);
    }

    #endregion

    #region Longitude

    /// <summary>
    /// Longitude Dependency Property
    /// </summary>
    public static readonly DependencyProperty LongitudeProperty =
        DependencyProperty.Register(nameof(Longitude), typeof(double), typeof(CoordinatesNumberBox),
            new PropertyMetadata((double)0.0));

    /// <summary>
    /// Gets or sets the Long property. This dependency property
    /// indicates east-west position on the Earth's surface.
    /// </summary>
    public double Longitude
    {
        get => (double)GetValue(LongitudeProperty);
        set => SetValue(LongitudeProperty, value);
    }

    #endregion

    public CoordinatesNumberBox()
    {
        DefaultStyleKey = typeof(CoordinatesNumberBox);
    }

    protected override void OnApplyTemplate()
    {
        base.OnApplyTemplate();
        SetHeaderVisibility();
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
