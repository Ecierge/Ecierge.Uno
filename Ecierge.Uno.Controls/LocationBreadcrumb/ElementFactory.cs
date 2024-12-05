using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;

#if !HAS_UNO
using IElementFactoryShim = Microsoft.UI.Xaml.IElementFactory;
#endif


namespace Ecierge.Uno.Controls.LocationBreadcrumb;
public class ElementFactory : IElementFactoryShim
{
#if HAS_UNO
    public UIElement GetElement(Microsoft.UI.Xaml.Controls.ElementFactoryGetArgs args)
     => GetElementCore(args);


    public void RecycleElement(Microsoft.UI.Xaml.Controls.ElementFactoryRecycleArgs args)
     => RecycleElementCore(args);

    protected virtual UIElement GetElementCore(Microsoft.UI.Xaml.Controls.ElementFactoryGetArgs args)
            => throw new NotImplementedException();

    protected virtual void RecycleElementCore(Microsoft.UI.Xaml.Controls.ElementFactoryRecycleArgs args)
        => throw new NotImplementedException();
#else
    public UIElement GetElement(Microsoft.UI.Xaml.ElementFactoryGetArgs args)
    => GetElementCore(args);

    public void RecycleElement(Microsoft.UI.Xaml.ElementFactoryRecycleArgs args)
     => RecycleElementCore(args);

    protected virtual UIElement GetElementCore(Microsoft.UI.Xaml.ElementFactoryGetArgs args)
            => throw new NotImplementedException();

    protected virtual void RecycleElementCore(Microsoft.UI.Xaml.ElementFactoryRecycleArgs args)
        => throw new NotImplementedException();
#endif
}
