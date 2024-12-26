#nullable enable

using Uno.Disposables;
using Microsoft.UI.Xaml.Controls;

namespace Ecierge.Uno.Controls;

public partial class LocationBreadcrumbBar : Control
{
    private readonly SerialDisposable m_itemsRepeaterLoadedRevoker = new SerialDisposable();
    private readonly SerialDisposable m_itemsRepeaterElementPreparedRevoker = new SerialDisposable();
    private readonly SerialDisposable m_itemsRepeaterElementIndexChangedRevoker = new SerialDisposable();
    private readonly SerialDisposable m_itemsRepeaterElementClearingRevoker = new SerialDisposable();
    private readonly SerialDisposable m_itemsSourceChanged = new SerialDisposable();
    private readonly SerialDisposable m_breadcrumbKeyDownHandlerRevoker = new SerialDisposable();

    private readonly SerialDisposable m_itemsSourceAsObservableVectorChanged = new SerialDisposable();

    // This collection is only composed of the consumer defined objects, it doesn't
    // include the extra ellipsis/null element. This variable is only used to capture
    // changes in the ItemsSource
    private ItemsSourceView? m_breadcrumbItemsSourceView = null;

    // This is the "element collection" provided to the underlying ItemsRepeater, so it
    // includes the extra ellipsis/null element in the position 0.
    private LocationBreadcrumbIterable? m_itemsIterable = null;

    private ItemsRepeater? m_itemsRepeater = null;
    private LocationBreadcrumbElementFactory? m_itemsRepeaterElementFactory = null;
    private LocationBreadcrumbLayout? m_itemsRepeaterLayout = null;

    // Pointers to first and last items to update visual states
    private LocationBreadcrumbBarItem? m_ellipsisLocationBreadcrumbBarItem = null;
    private LocationBreadcrumbBarItem? m_lastLocationBreadcrumbBarItem = null;

    // Index of the last focused item when breadcrumb lost focus
    private int m_focusedIndex = 1;

    // Template Parts
    private const string s_itemsRepeaterPartName = "PART_ItemsRepeater";
}
