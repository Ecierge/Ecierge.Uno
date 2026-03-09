using Microsoft.UI.Xaml.Controls.Primitives;
using CommunityToolkit.WinUI.Controls;

namespace Ecierge.Uno.Controls;


[TemplatePart(Name = PartTitle, Type = typeof(ContentPresenter))]
[TemplatePart(Name = ScrollViewer, Type = typeof(ScrollViewer))]
[TemplatePart(Name = ContentPresenter, Type = typeof(ContentPresenter))]
[TemplatePart(Name = ScrollBar, Type = typeof(ScrollBar))]
[TemplatePart(Name = TitleBarPlaceholder, Type = typeof(Border))]
[TemplatePart(Name = HeaderContent, Type = typeof(FrameworkElement))]
[TemplatePart(Name = FooterContent, Type = typeof(FrameworkElement))]

public sealed partial class PageContentControl : HeaderedContentControl
{
    #region TemplatePartNames

    private const string PartTitle = "Title";
    private const string ScrollViewer = "ScrollViewer";
    private const string ContentPresenter = "ContentPresenter";
    private const string ScrollBar = "VerticalScrollBar";
    private const string TitleBarPlaceholder = "TitleBarPlaceholder";
    private const string HeaderContent = "Header";
    private const string FooterContent = "Footer";

    #endregion TemplatePartNames

    private ScrollViewer? scrollViewer;
    private ScrollBar? scrollBar;
    private ContentPresenter? contentPresenter;
    private FrameworkElement? header;
    private FrameworkElement? footer;

    private bool isSyncing;

    #region Title

    /// <summary>
    /// Title Dependency Property
    /// </summary>
    public static readonly DependencyProperty TitleProperty =
        DependencyProperty.Register(nameof(Title), typeof(object), typeof(PageContentControl),
            new PropertyMetadata((object?)null));

    /// <summary>
    /// Gets or sets the Title property. This dependency property
    /// indicates title.
    /// </summary>
    public object? Title
    {
        get => (object?)GetValue(TitleProperty);
        set => SetValue(TitleProperty, value);
    }

    #endregion Title

    #region TitleTemplate

    /// <summary>
    /// TitleTemplate Dependency Property
    /// </summary>
    public static readonly DependencyProperty TitleTemplateProperty =
        DependencyProperty.Register(nameof(TitleTemplate), typeof(DataTemplate), typeof(PageContentControl),
            new PropertyMetadata((DataTemplate?)null));

    /// <summary>
    /// Gets or sets the TitleTemplate property. This dependency property
    /// indicates title template.
    /// </summary>
    public DataTemplate? TitleTemplate
    {
        get => (DataTemplate?)GetValue(TitleTemplateProperty);
        set => SetValue(TitleTemplateProperty, value);
    }

    #endregion

    //#region Content

    ///// <summary>
    ///// Content Dependency Property
    ///// </summary>
    //public static readonly DependencyProperty ContentProperty =
    //    DependencyProperty.Register(nameof(Content), typeof(object), typeof(PageContentControl),
    //        new PropertyMetadata((object?)null));

    ///// <summary>
    ///// Gets or sets the Content property. This dependency property
    ///// indicates content.
    ///// </summary>
    //public object? Content
    //{
    //    get => (object?)GetValue(ContentProperty);
    //    set => SetValue(ContentProperty, value);
    //}

    //#endregion Content

    //#region ContentTemplate

    ///// <summary>
    ///// ContentTemplate Dependency Property
    ///// </summary>
    //public static readonly DependencyProperty ContentTemplateProperty =
    //    DependencyProperty.Register(nameof(ContentTemplate), typeof(DataTemplate), typeof(PageContentControl),
    //        new PropertyMetadata((DataTemplate?)null));

    ///// <summary>
    ///// Gets or sets the ContentTemplate property. This dependency property
    ///// indicates title template.
    ///// </summary>
    //public DataTemplate? ContentTemplate
    //{
    //    get => (DataTemplate?)GetValue(ContentTemplateProperty);
    //    set => SetValue(ContentTemplateProperty, value);
    //}

    //#endregion ContentTemplate

    #region Header

    /// <summary>
    /// Header Dependency Property
    /// </summary>
    public object? Header
    {
        get => GetValue(HeaderProperty);
        set => SetValue(HeaderProperty, value);
    }

    /// <summary>
    /// Gets or sets the  Header property. This dependency property
    /// </summary>
    public static readonly DependencyProperty HeaderProperty =
        DependencyProperty.Register(nameof(Header), typeof(object), typeof(PageContentControl), new PropertyMetadata(null));

    #endregion Header

    #region HeaderTemplate

    /// <summary>
    /// HeaderTemplate Dependency Property
    /// </summary>
    public static readonly DependencyProperty HeaderTemplateProperty =
        DependencyProperty.Register(nameof(HeaderTemplate), typeof(DataTemplate), typeof(PageContentControl),
            new PropertyMetadata((DataTemplate?)null));

    /// <summary>
    /// Gets or sets the HeaderTemplate property. This dependency property
    /// indicates title template.
    /// </summary>
    public DataTemplate? HeaderTemplate
    {
        get => (DataTemplate?)GetValue(HeaderTemplateProperty);
        set => SetValue(HeaderTemplateProperty, value);
    }

    #endregion HeaderTemplate

    #region Footer

    /// <summary>
    /// Footer Dependency Property
    /// </summary>
    public object? Footer
    {
        get => GetValue(FooterProperty);
        set => SetValue(FooterProperty, value);
    }

    /// <summary>
    /// Gets or sets the  Footer property. This dependency property
    /// </summary>
    public static readonly DependencyProperty FooterProperty =
        DependencyProperty.Register(nameof(Footer), typeof(object), typeof(PageContentControl), new PropertyMetadata(null));

    #endregion Footer

    #region FooterTemplate

    /// <summary>
    /// FooterTemplate Dependency Property
    /// </summary>
    public static readonly DependencyProperty FooterTemplateProperty =
        DependencyProperty.Register(nameof(FooterTemplate), typeof(DataTemplate), typeof(PageContentControl),
            new PropertyMetadata((DataTemplate?)null));

    /// <summary>
    /// Gets or sets the FooterTemplate property. This dependency property
    /// indicates title template.
    /// </summary>
    public DataTemplate? FooterTemplate
    {
        get => (DataTemplate?)GetValue(FooterTemplateProperty);
        set => SetValue(FooterTemplateProperty, value);
    }

    #endregion FooterTemplate

    #region ScrollBarEnabled
    /// <summary>
    /// Gets or sets a value indicating whether the vertical ScrollBar is enabled.
    /// </summary>
    public bool IsScrollBarEnabled
    {
        get => (bool)GetValue(IsScrollBarEnabledProperty);
        set => SetValue(IsScrollBarEnabledProperty, value);
    }

    /// <summary>
    /// Identifies the ScrollBarEnabled dependency property.
    /// </summary>
    public static readonly DependencyProperty IsScrollBarEnabledProperty =
        DependencyProperty.Register(
            nameof(IsScrollBarEnabled),
            typeof(bool),
            typeof(PageContentControl),
            new PropertyMetadata(true, OnScrollBarEnabledChanged));

    private static void OnScrollBarEnabledChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is PageContentControl control && control.scrollViewer is not null && control.scrollBar is not null)
        {
            // control.scrollBar.IsEnabled = (bool)e.NewValue;
            control.scrollBar.Visibility = (bool)e.NewValue == true
                ? Visibility.Visible
                : Visibility.Collapsed;
            //control.scrollViewer.VerticalScrollMode = (bool)e.NewValue == true
            //    ? ScrollMode.Auto
            //    : ScrollMode.Disabled;
            //control.scrollViewer.VerticalScrollBarVisibility = (bool)e.NewValue == true
            //    ? ScrollBarVisibility.Visible
            //    : ScrollBarVisibility.Disabled;
        }
    }
    #endregion ScrollBarEnabled

    #region TopBorderHeight

    /// <summary>
    /// BorderHeight Dependency Property
    /// </summary>
    public double TopBorderHeight
{
    get => (double)GetValue(TopBorderHeightProperty);
    set => SetValue(TopBorderHeightProperty, value);
}

/// <summary>
/// Gets or sets the TopBorderHeight property. This dependency property
/// indicates the height of the border on the Top of the Page.
/// </summary>
public static readonly DependencyProperty TopBorderHeightProperty =
    DependencyProperty.Register(
        nameof(TopBorderHeight),
        typeof(double),
        typeof(PageContentControl),
        new PropertyMetadata(38.0));

#endregion TopBorderHeight

    public PageContentControl()
    {
        DefaultStyleKey = typeof(PageContentControl);
    }

    protected override void OnApplyTemplate()
    {
        base.OnApplyTemplate();

        DetachHandlers();
        scrollViewer = GetTemplateChild(ScrollViewer) as ScrollViewer;
        scrollBar = GetTemplateChild(ScrollBar) as ScrollBar;
        header = GetTemplateChild(HeaderContent) as FrameworkElement;
        footer = GetTemplateChild(FooterContent) as FrameworkElement;
        contentPresenter = GetTemplateChild("ContentPresenter") as ContentPresenter;

        if (scrollViewer != null && scrollBar != null)
        {
            scrollViewer.ViewChanged += OnScrollViewerViewChanged;
            scrollBar.ValueChanged += OnScrollBarValueChanged;
            scrollViewer.LayoutUpdated += OnScrollViewerLayoutUpdated;
        }

        if (header is not null)
            header.SizeChanged += OnHeaderFooterSizeChanged;
        if (footer is not null)
            footer.SizeChanged += OnHeaderFooterSizeChanged;

        UpdateScrollBarMargin();
        UpdateContentPresenterPadding();
        UpdateScrollBar();
    }

    private void OnHeaderFooterSizeChanged(object? sender, SizeChangedEventArgs e)
    {
        UpdateScrollBarMargin();
    }

    private void OnScrollViewerLayoutUpdated(object? sender, object e)
    {
        UpdateScrollBar();
        UpdateScrollBarMargin();
        UpdateContentPresenterPadding();
    }

    private void OnScrollViewerViewChanged(object? sender, ScrollViewerViewChangedEventArgs e)
    {
        if (isSyncing) return;
        isSyncing = true;

        UpdateScrollBar();

        if (scrollBar is not null && scrollViewer is not null && Math.Abs(scrollBar.Value - scrollViewer.VerticalOffset) > 0.5)
            scrollBar.Value = scrollViewer.VerticalOffset;

        isSyncing = false;
    }

    private void OnScrollBarValueChanged(object? sender, RangeBaseValueChangedEventArgs e)
    {
        if (isSyncing) return;
        isSyncing = true;

        if (scrollViewer is not null && Math.Abs(e.NewValue - scrollViewer.VerticalOffset) > 0.5)
            scrollViewer.ChangeView(null, e.NewValue, null, disableAnimation: true);

        isSyncing = false;
    }

    private void UpdateScrollBar()
    {
        if (scrollViewer is null || scrollBar is null)
            return;

        double extent = scrollViewer.ExtentHeight;
        double viewport = scrollViewer.ViewportHeight;
        double offset = scrollViewer.VerticalOffset;
        double scrollable = scrollViewer.ScrollableHeight;

        scrollBar.Minimum = 0;
        scrollBar.Maximum = extent;

        scrollBar.ViewportSize = viewport;

        scrollBar.SmallChange = viewport * 0.1;
        scrollBar.LargeChange = viewport;

        scrollBar.Value = offset;

        scrollBar.Visibility = scrollable > 0 ? Visibility.Visible : Visibility.Collapsed;
        scrollBar.IsEnabled = scrollable > 0;
    }

    private void UpdateScrollBarMargin()
    {
        if (scrollBar is null)
            return;

        double topOffset = 0;
        double bottomOffset = 0;

        if (header is not null && header.ActualHeight > 0)
            topOffset = header.ActualHeight;

        if (footer is not null && footer.ActualHeight > 0)
            bottomOffset = footer.ActualHeight;

        scrollBar.Margin = new Thickness(0, topOffset, 0, bottomOffset);
    }

    private void UpdateContentPresenterPadding()
    {
        if (contentPresenter == null)
            return;

        double topOffset = 0;
        double bottomOffset = 0;

        if (header is not null && header.ActualHeight > 0)
            topOffset = header.ActualHeight;

        if (footer is not null && footer.ActualHeight > 0)
            bottomOffset = footer.ActualHeight;

        contentPresenter.Padding = new Thickness(0, topOffset, 0, bottomOffset);

        if (scrollViewer is null || scrollBar is null)
            return;

        scrollBar.Maximum = scrollViewer.ScrollableHeight;
        scrollBar.ViewportSize = scrollViewer.ViewportHeight;
        scrollBar.Value = scrollViewer.VerticalOffset;
    }

    private void DetachHandlers()
    {
        if (scrollViewer is not null)
        {
            scrollViewer.ViewChanged -= OnScrollViewerViewChanged;
            scrollViewer.LayoutUpdated -= OnScrollViewerLayoutUpdated;
        }
        if (scrollBar is not null)
            scrollBar.ValueChanged -= OnScrollBarValueChanged;
        if (header is not null)
            header.SizeChanged -= OnHeaderFooterSizeChanged;
        if (footer is not null)
            footer.SizeChanged -= OnHeaderFooterSizeChanged;
    }
}
