using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Input;

namespace Ecierge.Uno.Controls;

[TemplatePart(Name = PartTitle, Type = typeof(ContentPresenter))]
[TemplatePart(Name = ScrollViewer, Type = typeof(ScrollViewer))]
[TemplatePart(Name = ContentPresenter, Type = typeof(ContentPresenter))]
[TemplatePart(Name = ScrollBar, Type = typeof(ScrollBar))]
[TemplatePart(Name = Header, Type = typeof(FrameworkElement))]
[TemplatePart(Name = Footer, Type = typeof(FrameworkElement))]

public sealed partial class PageContentControl : Control
{
    #region TemplatePartNames

    private const string PartTitle = "Title";
    private const string ScrollViewer = "ScrollViewer";
    private const string ContentPresenter = "ContentPresenter";
    private const string ScrollBar = "VerticalScrollBar";
    private const string Header = "Header";
    private const string Footer = "Footer";

    #endregion

    private ScrollViewer? scrollViewer;
    private ScrollBar? scrollBar;
    private ContentPresenter? contentPresenter;
    private FrameworkElement? header;
    private FrameworkElement? footer;

    private bool isSyncing;

    #region Title

    /// <summary>
    /// Header Dependency Property
    /// </summary>
    public static readonly DependencyProperty TitleProperty =
        DependencyProperty.Register(nameof(Title), typeof(object), typeof(PageContentControl),
            new PropertyMetadata((object?)null));

    /// <summary>
    /// Gets or sets the Header property. This dependency property
    /// indicates header.
    /// </summary>
    public object? Title
    {
        get => (object?)GetValue(TitleProperty);
        set => SetValue(TitleProperty, value);
    }

    #endregion

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

    #region Content

    /// <summary>
    /// Content Dependency Property
    /// </summary>
    public static readonly DependencyProperty ContentProperty =
        DependencyProperty.Register(nameof(Content), typeof(object), typeof(PageContentControl),
            new PropertyMetadata((object?)null));

    /// <summary>
    /// Gets or sets the Content property. This dependency property
    /// indicates content.
    /// </summary>
    public object? Content
    {
        get => (object?)GetValue(ContentProperty);
        set => SetValue(ContentProperty, value);
    }

    #endregion

    #region HeaderContent

    /// <summary>
    /// HeaderContent Dependency Property
    /// </summary>
    public object HeaderContent
    {
        get => GetValue(HeaderContentProperty);
        set => SetValue(HeaderContentProperty, value);
    }

    /// <summary>
    /// Gets or sets the  HeaderContent property. This dependency property
    /// </summary>
    public static readonly DependencyProperty HeaderContentProperty =
        DependencyProperty.Register(nameof(HeaderContent), typeof(object), typeof(PageContentControl), new PropertyMetadata(null));

    #endregion

    #region FooterContent

    /// <summary>
    /// FooterContent Dependency Property
    /// </summary>
    public object FooterContent
    {
        get => GetValue(FooterContentProperty);
        set => SetValue(FooterContentProperty, value);
    }

    /// <summary>
    /// Gets or sets the  FooterContent property. This dependency property
    /// </summary>
    public static readonly DependencyProperty FooterContentProperty =
        DependencyProperty.Register(nameof(FooterContent), typeof(object), typeof(PageContentControl), new PropertyMetadata(null));

    #endregion

    #region FooterCornerRadius

    /// <summary>
    /// FooterCornerRadius Dependency Property
    /// </summary>
    public CornerRadius FooterCornerRadius
    {
        get => (CornerRadius)GetValue(FooterCornerRadiusProperty);
        set => SetValue(FooterCornerRadiusProperty, value);
    }

    /// <summary>
    /// Gets or sets the FooterCornerRadius property. This dependency property
    /// </summary>
    public static readonly DependencyProperty FooterCornerRadiusProperty =
        DependencyProperty.Register(
            nameof(FooterCornerRadius),
            typeof(CornerRadius),
            typeof(PageContentControl),
            new PropertyMetadata(new CornerRadius(0)));

    #endregion

    #region HeaderCornerRadius

    /// <summary>
    /// HeaderCornerRadius Dependency Property
    /// </summary>
    public CornerRadius HeaderCornerRadius
    {
        get => (CornerRadius)GetValue(HeaderCornerRadiusProperty);
        set => SetValue(HeaderCornerRadiusProperty, value);
    }

    /// <summary>
    /// Gets or sets the HeaderCornerRadius property. This dependency property
    /// </summary>
    public static readonly DependencyProperty HeaderCornerRadiusProperty =
        DependencyProperty.Register(
            nameof(HeaderCornerRadius),
            typeof(CornerRadius),
            typeof(PageContentControl),
            new PropertyMetadata(new CornerRadius(0)));

    #endregion

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
        header = GetTemplateChild(Header) as FrameworkElement;
        footer = GetTemplateChild(Footer) as FrameworkElement;
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

        double scrollable = scrollViewer.ScrollableHeight;
        double viewport = scrollViewer.ViewportHeight;
        double offset = scrollViewer.VerticalOffset;

        scrollBar.Minimum = 0;
        scrollBar.Maximum = scrollable > 0 ? scrollable : 0;
        scrollBar.ViewportSize = viewport;
        scrollBar.SmallChange = 16;
        scrollBar.LargeChange = Math.Max(32, viewport * 0.9);
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
