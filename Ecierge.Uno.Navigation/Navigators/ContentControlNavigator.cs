namespace Ecierge.Uno.Navigation.Navigators;

using System;
using System.Threading.Tasks;

public class ContentControlNavigator : FactoryNavigator
{
    public ContentControlNavigator(IServiceProvider serviceProvider) : base(serviceProvider) { }

    protected override async ValueTask<NavigationResult> NavigateCoreAsync(NavigationRequest request)
    {
        var result = CreateView(request);
        if (!result.Success) return result;

        var view = (FrameworkElement)result.Result!;
        var contentControl = (ContentControl)Region!.Target!;
        contentControl.Content = view;
        return new NavigationResult(request.RouteSegment);
    }
}
