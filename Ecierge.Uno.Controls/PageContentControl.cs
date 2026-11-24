using CommunityToolkit.WinUI.Controls;

using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Input;

using Windows.Foundation;

namespace Ecierge.Uno.Controls;

/// <summary>
/// Lays out the content of a page under an optional <see cref="Title"/>, with a <see cref="HeaderedContentControl.Header"/> and a <see cref="Footer"/> that float over the scrolling content.
/// </summary>
/// <remarks>
/// <para>The content scrolls under the header and the footer, so that their translucent backgrounds (<see cref="HeaderBackground"/> and <see cref="FooterBackground"/>) show it through. The control pads the content by the heights of the header and the footer, keeps its scroll bar between them and enlarges every request to bring an element of the content into view by the same amounts, so that a focused element does not stay under them.</para>
/// <para>The title, the header and the footer collapse while their values are <see langword="null"/> or empty strings, so any of them can be left unset.</para>
/// <para>Set <c>ScrollViewer.VerticalScrollBarVisibility</c> to <see cref="ScrollBarVisibility.Disabled"/> on the control when the content scrolls itself, for example a <see cref="ListView"/>: the content is then limited to the height of the viewport instead of being measured with an unlimited one, and keeps its virtualization.</para>
/// </remarks>
[TemplatePart(Name = PartTitlePresenter, Type = typeof(FrameworkElement))]
[TemplatePart(Name = PartHeaderPanel, Type = typeof(FrameworkElement))]
[TemplatePart(Name = PartHeaderPresenter, Type = typeof(FrameworkElement))]
[TemplatePart(Name = PartScrollViewer, Type = typeof(ScrollViewer))]
[TemplatePart(Name = PartContentPresenter, Type = typeof(ContentPresenter))]
[TemplatePart(Name = PartFooterPanel, Type = typeof(FrameworkElement))]
[TemplatePart(Name = PartFooterPresenter, Type = typeof(FrameworkElement))]
[TemplatePart(Name = PartVerticalScrollBar, Type = typeof(ScrollBar))]
public sealed partial class PageContentControl : HeaderedContentControl
{
    #region TemplatePartNames

    private const string PartTitlePresenter = "TitlePresenter";
    private const string PartHeaderPanel = "HeaderPanel";
    // HeaderedContentControl looks the header presenter up by this name to collapse it while Header is empty.
    private const string PartHeaderPresenter = "HeaderPresenter";
    private const string PartScrollViewer = "ScrollViewer";
    private const string PartContentPresenter = "ContentPresenter";
    private const string PartFooterPanel = "FooterPanel";
    private const string PartFooterPresenter = "FooterPresenter";
    private const string PartVerticalScrollBar = "VerticalScrollBar";

    #endregion TemplatePartNames

    // The scroll bar steps by lines of the same height as the ones of the stock ScrollViewer, and a mouse wheel notch over it scrolls by three of them, the system default.
    private const double LineHeight = 16;
    private const double LinesPerWheelNotch = 3;
    private const double WheelDeltaPerNotch = 120;

    private FrameworkElement? titlePresenter;
    private FrameworkElement? headerPanel;
    private ScrollViewer? scrollViewer;
    private ContentPresenter? contentPresenter;
    private FrameworkElement? footerPanel;
    private FrameworkElement? footerPresenter;
    private ScrollBar? verticalScrollBar;

    // Set while the scroll bar is updated from the scroll viewer, so that its ValueChanged does not scroll the content back.
    private bool isSyncing;

    // Set while the content presenter is limited to the viewport because the scrolling of the content is off.
    private bool isContentHeightLimited;

    #region Title

    /// <summary>
    /// Identifies the <see cref="Title"/> dependency property.
    /// </summary>
    public static readonly DependencyProperty TitleProperty =
        DependencyProperty.Register(nameof(Title), typeof(object), typeof(PageContentControl),
            new PropertyMetadata(null, OnTitleChanged));

    /// <summary>
    /// Gets or sets the title shown above the header and the content. The title row collapses while the value is <see langword="null"/> or an empty string.
    /// </summary>
    public object? Title
    {
        get => GetValue(TitleProperty);
        set => SetValue(TitleProperty, value);
    }

    /// <summary>
    /// Collapses or shows the title presenter when <see cref="Title"/> changes.
    /// </summary>
    private static void OnTitleChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is PageContentControl control)
        {
            UpdatePresenterVisibility(control.titlePresenter, e.NewValue);
        }
    }

    #endregion Title

    #region TitleTemplate

    /// <summary>
    /// Identifies the <see cref="TitleTemplate"/> dependency property.
    /// </summary>
    public static readonly DependencyProperty TitleTemplateProperty =
        DependencyProperty.Register(nameof(TitleTemplate), typeof(DataTemplate), typeof(PageContentControl),
            new PropertyMetadata(null));

    /// <summary>
    /// Gets or sets the template used to display the <see cref="Title"/>.
    /// </summary>
    public DataTemplate? TitleTemplate
    {
        get => (DataTemplate?)GetValue(TitleTemplateProperty);
        set => SetValue(TitleTemplateProperty, value);
    }

    #endregion TitleTemplate

    #region Footer

    /// <summary>
    /// Identifies the <see cref="Footer"/> dependency property.
    /// </summary>
    public static readonly DependencyProperty FooterProperty =
        DependencyProperty.Register(nameof(Footer), typeof(object), typeof(PageContentControl),
            new PropertyMetadata(null, OnFooterChanged));

    /// <summary>
    /// Gets or sets the footer shown over the bottom of the scrolling content. The footer collapses while the value is <see langword="null"/> or an empty string.
    /// </summary>
    public object? Footer
    {
        get => GetValue(FooterProperty);
        set => SetValue(FooterProperty, value);
    }

    /// <summary>
    /// Collapses or shows the footer presenter when <see cref="Footer"/> changes.
    /// </summary>
    private static void OnFooterChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is PageContentControl control)
        {
            UpdatePresenterVisibility(control.footerPresenter, e.NewValue);
        }
    }

    #endregion Footer

    #region FooterTemplate

    /// <summary>
    /// Identifies the <see cref="FooterTemplate"/> dependency property.
    /// </summary>
    public static readonly DependencyProperty FooterTemplateProperty =
        DependencyProperty.Register(nameof(FooterTemplate), typeof(DataTemplate), typeof(PageContentControl),
            new PropertyMetadata(null));

    /// <summary>
    /// Gets or sets the template used to display the <see cref="Footer"/>.
    /// </summary>
    public DataTemplate? FooterTemplate
    {
        get => (DataTemplate?)GetValue(FooterTemplateProperty);
        set => SetValue(FooterTemplateProperty, value);
    }

    #endregion FooterTemplate

    #region HeaderBackground

    /// <summary>
    /// Identifies the <see cref="HeaderBackground"/> dependency property.
    /// </summary>
    public static readonly DependencyProperty HeaderBackgroundProperty =
        DependencyProperty.Register(nameof(HeaderBackground), typeof(Brush), typeof(PageContentControl),
            new PropertyMetadata(null));

    /// <summary>
    /// Gets or sets the brush behind the header and the <see cref="TopBorderHeight"/> band above it. The default style uses the themed in-app acrylic (<c>AcrylicInAppFillColorDefaultBrush</c>), which falls back to an opaque color where acrylic is not rendered.
    /// </summary>
    public Brush? HeaderBackground
    {
        get => (Brush?)GetValue(HeaderBackgroundProperty);
        set => SetValue(HeaderBackgroundProperty, value);
    }

    #endregion HeaderBackground

    #region FooterBackground

    /// <summary>
    /// Identifies the <see cref="FooterBackground"/> dependency property.
    /// </summary>
    public static readonly DependencyProperty FooterBackgroundProperty =
        DependencyProperty.Register(nameof(FooterBackground), typeof(Brush), typeof(PageContentControl),
            new PropertyMetadata(null));

    /// <summary>
    /// Gets or sets the brush behind the footer. The default style uses the themed in-app acrylic (<c>AcrylicInAppFillColorDefaultBrush</c>), which falls back to an opaque color where acrylic is not rendered.
    /// </summary>
    public Brush? FooterBackground
    {
        get => (Brush?)GetValue(FooterBackgroundProperty);
        set => SetValue(FooterBackgroundProperty, value);
    }

    #endregion FooterBackground

    #region IsScrollBarEnabled

    /// <summary>
    /// Identifies the <see cref="IsScrollBarEnabled"/> dependency property.
    /// </summary>
    public static readonly DependencyProperty IsScrollBarEnabledProperty =
        DependencyProperty.Register(nameof(IsScrollBarEnabled), typeof(bool), typeof(PageContentControl),
            new PropertyMetadata(true, OnIsScrollBarEnabledChanged));

    /// <summary>
    /// Gets or sets a value indicating whether the scroll bar is shown while the content is taller than the control. The content scrolls either way.
    /// </summary>
    public bool IsScrollBarEnabled
    {
        get => (bool)GetValue(IsScrollBarEnabledProperty);
        set => SetValue(IsScrollBarEnabledProperty, value);
    }

    /// <summary>
    /// Shows or hides the scroll bar when <see cref="IsScrollBarEnabled"/> changes.
    /// </summary>
    private static void OnIsScrollBarEnabledChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is PageContentControl control)
        {
            control.UpdateScrollBar();
        }
    }

    #endregion IsScrollBarEnabled

    #region TopBorderHeight

    /// <summary>
    /// Identifies the <see cref="TopBorderHeight"/> dependency property.
    /// </summary>
    public static readonly DependencyProperty TopBorderHeightProperty =
        DependencyProperty.Register(nameof(TopBorderHeight), typeof(double), typeof(PageContentControl),
            new PropertyMetadata(0.0));

    /// <summary>
    /// Gets or sets the height of an empty band at the top of the header background, for example to keep the header clear of the custom title bar of a window. The default is 0.
    /// </summary>
    public double TopBorderHeight
    {
        get => (double)GetValue(TopBorderHeightProperty);
        set => SetValue(TopBorderHeightProperty, value);
    }

    #endregion TopBorderHeight

    /// <summary>
    /// Initializes a new instance of the <see cref="PageContentControl"/> class.
    /// </summary>
    public PageContentControl()
    {
        DefaultStyleKey = typeof(PageContentControl);

        // Padding and the attached ScrollViewer.VerticalScrollBarVisibility are declared by other classes, so their changes are observed here.
        RegisterPropertyChangedCallback(PaddingProperty, OnPaddingChanged);
        RegisterPropertyChangedCallback(ScrollViewer.VerticalScrollBarVisibilityProperty, OnVerticalScrollBarVisibilityChanged);
    }

    /// <inheritdoc />
    protected override void OnApplyTemplate()
    {
        // HeaderedContentControl collapses the header presenter here while Header is empty.
        base.OnApplyTemplate();

        DetachTemplateParts();

        titlePresenter = GetTemplateChild(PartTitlePresenter) as FrameworkElement;
        headerPanel = GetTemplateChild(PartHeaderPanel) as FrameworkElement;
        scrollViewer = GetTemplateChild(PartScrollViewer) as ScrollViewer;
        contentPresenter = GetTemplateChild(PartContentPresenter) as ContentPresenter;
        footerPanel = GetTemplateChild(PartFooterPanel) as FrameworkElement;
        footerPresenter = GetTemplateChild(PartFooterPresenter) as FrameworkElement;
        verticalScrollBar = GetTemplateChild(PartVerticalScrollBar) as ScrollBar;

        AttachTemplateParts();

        UpdatePresenterVisibility(titlePresenter, Title);
        UpdatePresenterVisibility(footerPresenter, Footer);
        UpdateContentHeightLimit();
        UpdateInsets();
    }

    /// <summary>
    /// Subscribes to the template parts. Only events of the parts are used, never the application-wide <see cref="FrameworkElement.LayoutUpdated"/>, so an instance that is not on the screen costs nothing.
    /// </summary>
    private void AttachTemplateParts()
    {
        if (headerPanel is not null)
        {
            headerPanel.SizeChanged += OnHeaderOrFooterSizeChanged;
        }

        if (footerPanel is not null)
        {
            footerPanel.SizeChanged += OnHeaderOrFooterSizeChanged;
        }

        if (scrollViewer is not null)
        {
            scrollViewer.ViewChanged += OnScrollViewerViewChanged;
            scrollViewer.SizeChanged += OnScrollViewerSizeChanged;
        }

        if (contentPresenter is not null)
        {
            // The content presenter is the only child of the scroll viewer, so its size is the extent.
            contentPresenter.SizeChanged += OnContentSizeChanged;
            contentPresenter.BringIntoViewRequested += OnContentBringIntoViewRequested;
        }

        if (verticalScrollBar is not null)
        {
            verticalScrollBar.ValueChanged += OnScrollBarValueChanged;
            verticalScrollBar.PointerWheelChanged += OnScrollBarPointerWheelChanged;
        }
    }

    /// <summary>
    /// Unsubscribes from the parts of the previous template.
    /// </summary>
    private void DetachTemplateParts()
    {
        if (headerPanel is not null)
        {
            headerPanel.SizeChanged -= OnHeaderOrFooterSizeChanged;
        }

        if (footerPanel is not null)
        {
            footerPanel.SizeChanged -= OnHeaderOrFooterSizeChanged;
        }

        if (scrollViewer is not null)
        {
            scrollViewer.ViewChanged -= OnScrollViewerViewChanged;
            scrollViewer.SizeChanged -= OnScrollViewerSizeChanged;
        }

        if (contentPresenter is not null)
        {
            contentPresenter.SizeChanged -= OnContentSizeChanged;
            contentPresenter.BringIntoViewRequested -= OnContentBringIntoViewRequested;
        }

        if (verticalScrollBar is not null)
        {
            verticalScrollBar.ValueChanged -= OnScrollBarValueChanged;
            verticalScrollBar.PointerWheelChanged -= OnScrollBarPointerWheelChanged;
        }

        // The limit lived on the previous content presenter.
        isContentHeightLimited = false;
    }

    /// <summary>
    /// Collapses a presenter while its value is <see langword="null"/> or an empty string, the way <see cref="HeaderedContentControl"/> treats the header.
    /// </summary>
    private static void UpdatePresenterVisibility(FrameworkElement? presenter, object? value)
    {
        if (presenter is null)
        {
            return;
        }

        presenter.Visibility = value is null or string { Length: 0 } ? Visibility.Collapsed : Visibility.Visible;
    }

    /// <summary>
    /// Gets a value indicating whether the scrolling of the content is off: <c>ScrollViewer.VerticalScrollBarVisibility</c> of the control is <see cref="ScrollBarVisibility.Disabled"/>.
    /// </summary>
    private bool IsContentScrollingDisabled => ScrollViewer.GetVerticalScrollBarVisibility(this) is ScrollBarVisibility.Disabled;

    /// <summary>
    /// Limits the content to the height of the viewport while <see cref="IsContentScrollingDisabled"/>, so that content which scrolls itself gets a bounded height and nothing is left to scroll.
    /// </summary>
    /// <remarks>
    /// The limit is set on the content presenter instead of switching the vertical scroll bar visibility of the scroll viewer to <see cref="ScrollBarVisibility.Disabled"/>: Uno does not measure the content of a scroll viewer again when that visibility changes at run time.
    /// </remarks>
    private void UpdateContentHeightLimit()
    {
        if (scrollViewer is null || contentPresenter is null)
        {
            return;
        }

        if (IsContentScrollingDisabled)
        {
            double viewport = scrollViewer.ActualHeight;
            if (viewport > 0 && contentPresenter.MaxHeight != viewport)
            {
                contentPresenter.MaxHeight = viewport;
            }

            isContentHeightLimited = true;
        }
        else if (isContentHeightLimited)
        {
            contentPresenter.ClearValue(MaxHeightProperty);
            isContentHeightLimited = false;
        }
    }

    /// <summary>
    /// Pads the content by the heights of the header and the footer on top of <see cref="Control.Padding"/>, and keeps the scroll bar between them.
    /// </summary>
    private void UpdateInsets()
    {
        double top = headerPanel?.ActualHeight ?? 0;
        double bottom = footerPanel?.ActualHeight ?? 0;

        if (contentPresenter is not null)
        {
            var padding = Padding;
            var contentPadding = new Thickness(padding.Left, padding.Top + top, padding.Right, padding.Bottom + bottom);
            if (contentPresenter.Padding != contentPadding)
            {
                contentPresenter.Padding = contentPadding;
            }
        }

        if (verticalScrollBar is not null)
        {
            var margin = new Thickness(0, top, 0, bottom);
            if (verticalScrollBar.Margin != margin)
            {
                verticalScrollBar.Margin = margin;
            }
        }

        UpdateScrollBar();
    }

    /// <summary>
    /// Copies the range, the steps and the offset of the scroll viewer to the scroll bar and shows the bar while there is something to scroll.
    /// </summary>
    private void UpdateScrollBar()
    {
        if (scrollViewer is null || verticalScrollBar is null)
        {
            return;
        }

        double viewport = scrollViewer.ViewportHeight;
        double scrollable = Math.Max(0, scrollViewer.ScrollableHeight);
        // A page is what is visible between the header and the footer, not the whole viewport under them.
        double page = Math.Max(LineHeight, viewport - (headerPanel?.ActualHeight ?? 0) - (footerPanel?.ActualHeight ?? 0));

        isSyncing = true;
        try
        {
            // ViewportSize goes first: the scroll bar sizes its thumb when Minimum, Maximum or Value change, not when ViewportSize does.
            verticalScrollBar.ViewportSize = viewport;
            verticalScrollBar.Maximum = scrollable;
            verticalScrollBar.SmallChange = LineHeight;
            verticalScrollBar.LargeChange = page;
            verticalScrollBar.Value = scrollViewer.VerticalOffset;
            verticalScrollBar.IsEnabled = scrollable > 0;
            verticalScrollBar.Visibility = IsScrollBarEnabled && scrollable > 0 ? Visibility.Visible : Visibility.Collapsed;
        }
        finally
        {
            isSyncing = false;
        }
    }

    /// <summary>
    /// Updates the content padding and the scroll bar when <see cref="Control.Padding"/> changes.
    /// </summary>
    private void OnPaddingChanged(DependencyObject sender, DependencyProperty dp) => UpdateInsets();

    /// <summary>
    /// Turns the scrolling of the content on or off when <c>ScrollViewer.VerticalScrollBarVisibility</c> of the control changes.
    /// </summary>
    private void OnVerticalScrollBarVisibilityChanged(DependencyObject sender, DependencyProperty dp)
    {
        UpdateContentHeightLimit();
        UpdateScrollBar();
    }

    /// <summary>
    /// Updates the content padding and the scroll bar when the header or the footer changes its height.
    /// </summary>
    private void OnHeaderOrFooterSizeChanged(object sender, SizeChangedEventArgs e) => UpdateInsets();

    /// <summary>
    /// Keeps the content within the viewport while its scrolling is off and updates the scroll bar when the viewport changes.
    /// </summary>
    private void OnScrollViewerSizeChanged(object sender, SizeChangedEventArgs e)
    {
        UpdateContentHeightLimit();
        UpdateScrollBar();
    }

    /// <summary>
    /// Updates the scroll bar when the extent of the content changes.
    /// </summary>
    private void OnContentSizeChanged(object sender, SizeChangedEventArgs e) => UpdateScrollBar();

    /// <summary>
    /// Moves the scroll bar along when the content scrolls.
    /// </summary>
    private void OnScrollViewerViewChanged(object? sender, ScrollViewerViewChangedEventArgs e) => UpdateScrollBar();

    /// <summary>
    /// Scrolls the content when the user moves the scroll bar.
    /// </summary>
    private void OnScrollBarValueChanged(object sender, RangeBaseValueChangedEventArgs e)
    {
        if (isSyncing || scrollViewer is null)
        {
            return;
        }

        if (Math.Abs(e.NewValue - scrollViewer.VerticalOffset) > 0.5)
        {
            scrollViewer.ChangeView(null, e.NewValue, null, disableAnimation: true);
        }
    }

    /// <summary>
    /// Scrolls the content by the mouse wheel over the scroll bar, which lies next to the scroll viewer and not inside it, so the scroll viewer does not get the wheel there.
    /// </summary>
    private void OnScrollBarPointerWheelChanged(object sender, PointerRoutedEventArgs e)
    {
        if (scrollViewer is null || verticalScrollBar is null)
        {
            return;
        }

        int delta = e.GetCurrentPoint(verticalScrollBar).Properties.MouseWheelDelta;
        scrollViewer.ChangeView(null, scrollViewer.VerticalOffset - (delta / WheelDeltaPerNotch * LinesPerWheelNotch * LineHeight), null);
        e.Handled = true;
    }

    /// <summary>
    /// Enlarges a request to bring an element of the content into view by the heights of the header and the footer, so that the scroll viewer places the element between them and not under them, for example when it gets the keyboard focus.
    /// </summary>
    private void OnContentBringIntoViewRequested(UIElement sender, BringIntoViewRequestedEventArgs args)
    {
        double top = headerPanel?.ActualHeight ?? 0;
        double bottom = footerPanel?.ActualHeight ?? 0;
        if (top <= 0 && bottom <= 0)
        {
            return;
        }

        var target = args.TargetRect;
        args.TargetRect = new Rect(target.X, target.Y - top, target.Width, target.Height + top + bottom);
    }
}
