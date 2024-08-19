namespace Ecierge.Uno.Navigation.Navigators;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

public class ContentControlNavigator : FactoryNavigator
{
    public ContentControlNavigator(IServiceProvider serviceProvider) : base(serviceProvider) { }

    public override async ValueTask<NavigationResponse> NavigateCoreAsync(NavigationRequest request)
    {
        var view = CreateView(request);
        var contentControl = (ContentControl)Region!.Target!;
        contentControl.Content = view;
        return new SuccessfulNavigationResponse(null, this);
    }
}
