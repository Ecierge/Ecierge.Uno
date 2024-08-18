namespace Ecierge.Uno.Navigation.Navigators;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Microsoft.Extensions.DependencyInjection;

public class ContentControlNavigator : Navigator
{
    public override async ValueTask<NavigationResponse> NavigateAsync(NavigationRequest request)
    {
        var viewMap = request.NameSegment.View!;
        var view = (FrameworkElement)ServiceProvider.GetRequiredService(viewMap.View);
        if (viewMap.ViewModel is Type viewModelType)
        {
            var viewModel = Scope.CreateViewModel(viewModelType, request.NavigationData);
            view.DataContext = viewModel;
        }
        ((ContentControl)Region!.Target!).Content = view;
        return new SuccessfulNavigationResponse(null, this);
    }
}
