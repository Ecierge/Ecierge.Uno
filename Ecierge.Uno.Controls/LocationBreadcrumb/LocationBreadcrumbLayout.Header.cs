#nullable enable
#if HAS_UNO
using Uno.UI.DataBinding;
#endif
using Windows.Foundation;

namespace Ecierge.Uno.Controls.LocationBreadcrumb;
internal partial class LocationBreadcrumbLayout : NonVirtualizingLayout
{
    private Size m_availableSize;
    private LocationBreadcrumbBarItem? m_ellipsisButton = null;
#if HAS_UNO
    private ManagedWeakReference? m_breadcrumb = null;
#else
    private WeakReference<LocationBreadcrumbBar> m_breadcrumb = null;
#endif

    private bool m_ellipsisIsRendered;
    private uint m_firstRenderedItemIndexAfterEllipsis;
    private uint m_visibleItemsCount;
}
