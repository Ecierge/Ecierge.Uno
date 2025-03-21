#nullable enable

#if HAS_UNO
using Uno.Disposables;
using Uno.UI.DataBinding;
#else
using IElementFactoryShim = Microsoft.UI.Xaml.IElementFactory;
using ManagedWeakReference = System.WeakReference;
using SerialDisposable = System.Reactive.Disposables.SerialDisposable;
#endif

namespace Ecierge.Uno.Controls;

public partial class LocationBreadcrumbBarItem : ComboBox
{
    // Only used for bug workaround in BreadcrumbElementFactory::RecycleElementCore.
    internal bool IsEllipsisDropDownItem()
    {
        return m_isEllipsisDropDownItem;
    }

    // Common item fields

    // Contains the 1-indexed assigned to the element
    private int m_index;
    private bool m_isEllipsisDropDownItem = false;
    private FrameworkElement _lastFocusedElement;
    private bool _isKeyPressed = false;

    // Inline item fields

    private bool m_isEllipsisItem;
    private bool m_isLastItem;

    // BreadcrumbBarItem visual representation
    private Button? m_button = null;
    private ComboBox? m_comboBox = null;
    // Parent BreadcrumbBarItem to ask for hidden elements
    private ManagedWeakReference? m_parentBreadcrumb = null;

    // Flyout content for ellipsis item
    private Flyout? m_ellipsisFlyout = null;
    private ItemsRepeater? m_ellipsisItemsRepeater = null;
    private IElementFactoryShim? m_ellipsisDropDownItemDataTemplate = null;
    private LocationBreadcrumbElementFactory? m_ellipsisElementFactory = null;


    // Ellipsis dropdown item fields

    // BreadcrumbBarItem that owns the flyout
    private LocationBreadcrumbBarItem? m_ellipsisItem = null;

    // Visual State tracking
    private uint m_trackedPointerId = 0;
    private bool m_isPressed = false;
    private bool m_isPointerOver = false;

    // Common item token & revoker

    private readonly SerialDisposable m_childPreviewKeyDownToken = new SerialDisposable();
    private readonly SerialDisposable m_keyDownRevoker = new SerialDisposable();

    // Inline item token & revokers
    private long? m_flowDirectionChangedToken = null;
    private readonly SerialDisposable m_buttonLoadedRevoker = new SerialDisposable();
    private readonly SerialDisposable m_buttonClickRevoker = new SerialDisposable();
    private readonly SerialDisposable m_ellipsisRepeaterElementPreparedRevoker = new SerialDisposable();
    private readonly SerialDisposable m_ellipsisRepeaterElementIndexChangedRevoker = new SerialDisposable();
    private readonly SerialDisposable m_isPressedButtonRevoker = new SerialDisposable();
    private readonly SerialDisposable m_isPointerOverButtonRevoker = new SerialDisposable();
    private readonly SerialDisposable m_isEnabledButtonRevoker = new SerialDisposable();

    // Ellipsis dropdown item revoker
    private readonly SerialDisposable m_isEnabledChangedRevoker = new SerialDisposable();

    // Common Visual States
    private const string s_normalStateName = "Normal";
    private const string s_currentStateName = "Current";
    private const string s_pointerOverStateName = "PointerOver";
    private const string s_pressedStateName = "Pressed";
    private const string s_disabledStateName = "Disabled";

    // Inline Item Type Visual States
    private const string s_ellipsisStateName = "Ellipsis";
    private const string s_ellipsisRTLStateName = "EllipsisRTL";
    private const string s_lastItemStateName = "LastItem";
    private const string s_defaultStateName = "Default";
    private const string s_defaultRTLStateName = "DefaultRTL";

    // Item Type Visual States
    private const string s_inlineStateName = "Inline";
    private const string s_ellipsisDropDownStateName = "EllipsisDropDown";

    // Template Parts
    private const string s_ellipsisItemsRepeaterPartName = "PART_EllipsisItemsRepeater";
    private const string s_itemButtonPartName = "PART_ItemButton";
    private const string s_itemEllipsisFlyoutPartName = "PART_EllipsisFlyout";

    // Automation Names
    private const string s_ellipsisFlyoutAutomationName = "EllipsisFlyout";
    private const string s_ellipsisItemsRepeaterAutomationName = "EllipsisItemsRepeater";
}
