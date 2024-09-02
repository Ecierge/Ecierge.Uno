namespace Ecierge.Uno.Navigation.Navigators;

using System;
using System.Threading.Tasks;

public abstract class ContentControlNavigatorBase<TContentControl>(IServiceProvider serviceProvider) : FactoryNavigator<TContentControl>(serviceProvider)
    where TContentControl : ContentControl
{
    protected override FrameworkElement? WaitForVisualTreeTarget => Target.Content as FrameworkElement;
}

public class ContentControlNavigator : ContentControlNavigatorBase<ContentControl>
{
    public ContentControlNavigator(IServiceProvider serviceProvider) : base(serviceProvider) { }

    protected override async ValueTask<NavigationResult> NavigateCoreAsync(NavigationRequest request)
    {
        var result = CreateView(request);
        if (!result.Success) return result;

        var view = (FrameworkElement)result.Result!;
        var contentControl = Target!;
        contentControl.Content = view;
        return new NavigationResult(request.RouteSegment);
    }
}
