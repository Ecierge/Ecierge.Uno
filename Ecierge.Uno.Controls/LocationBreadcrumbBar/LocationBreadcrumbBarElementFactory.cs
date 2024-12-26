using Ecierge.Uno.Controls.Breadcrumb;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

#if !HAS_UNO
using IElementFactoryShim = Microsoft.UI.Xaml.IElementFactory;
using ElementFactory = Ecierge.Uno.Controls.LocationBreadcrumbBar.ElementFactory;
#else
using ElementFactoryRecycleArgs = Microsoft.UI.Xaml.Controls.ElementFactoryRecycleArgs;
using ElementFactoryGetArgs = Microsoft.UI.Xaml.Controls.ElementFactoryGetArgs;
#endif

namespace Ecierge.Uno.Controls.LocationBreadcrumbBar;

internal partial class LocationBreadcrumbElementFactory : ElementFactory
{

    private IElementFactoryShim? m_itemTemplateWrapper = null;

    public LocationBreadcrumbElementFactory()
    {
    }

    internal void UserElementFactory(object? newValue)
    {
        if (newValue is DataTemplate dataTemplate)
        {
            m_itemTemplateWrapper = (IElementFactoryShim)new ItemTemplateWrapper(dataTemplate);
        }
        else if (newValue is DataTemplateSelector selector)
        {
            m_itemTemplateWrapper = (IElementFactoryShim)new ItemTemplateWrapper(selector);
        }
        else if (newValue is IElementFactory customElementFactory)
        {
            m_itemTemplateWrapper = (IElementFactoryShim?)customElementFactory;
        }
    }

    protected override UIElement GetElementCore(ElementFactoryGetArgs args)
    {
        object GetNewContent(IElementFactoryShim? itemTemplateWrapper)
        {
            if (args.Data is LocationBreadcrumbBarItem)
            {
                return args.Data;
            }

            if (itemTemplateWrapper != null)
            {
                return itemTemplateWrapper.GetElement(args);
            }
            return args.Data;
        }

        var newContent = GetNewContent(m_itemTemplateWrapper);

        // Element is already a BreadcrumbBarItem, so we just return it.
        if (newContent is LocationBreadcrumbBarItem breadcrumbItem)
        {
            // When the list has not changed the returned item is still a BreadcrumbBarItem but the
            // item is not reset, so we set the content here
            breadcrumbItem.SelectedItem = args.Data;
            return breadcrumbItem;
        }

        var newBreadcrumbBarItem = new LocationBreadcrumbBarItem();
        newBreadcrumbBarItem.SelectedItem = args.Data;

        // If a user provided item template exists, we pass the template down
        // to the ContentPresenter of the BreadcrumbBarItem.
        if (m_itemTemplateWrapper is ItemTemplateWrapper itemTemplateWrapper)
        {
            newBreadcrumbBarItem.ItemTemplate = itemTemplateWrapper.Template;
        }

        return newBreadcrumbBarItem;
    }

    protected override void RecycleElementCore(ElementFactoryRecycleArgs args)
    {
        if (args.Element is { } element)
        {
            bool isEllipsisDropDownItem = false; // Use of isEllipsisDropDownItem is workaround for
                                                 // crashing bug when attempting to show ellipsis dropdown after clicking one of its items.

            if (element is LocationBreadcrumbBarItem breadcrumbItem)
            {
                var breadcrumbItemImpl = breadcrumbItem;
                breadcrumbItemImpl.ResetVisualProperties();
                
                isEllipsisDropDownItem = breadcrumbItemImpl.IsEllipsisDropDownItem();
            }

            if (m_itemTemplateWrapper != null && isEllipsisDropDownItem)
            {
                m_itemTemplateWrapper.RecycleElement(args);

            }
        }
    }

}
