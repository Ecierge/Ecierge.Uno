namespace Ecierge.Uno.Navigation.Navigators;

using System.Threading.Tasks;

using Ecierge.Uno.Navigation;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.UI.Dispatching;

public abstract class FactoryNavigator : Navigator
{
    public FactoryNavigator(IServiceProvider serviceProvider) : base(serviceProvider) { }

    public NavigationResult CreateView(NavigationRequest request)
    {
        var viewMap = request.NameSegment.View!;
        var view = (FrameworkElement)ServiceProvider.GetRequiredService(viewMap.View);
        if (viewMap.ViewModel is Type viewModelType)
        {
            var result = Scope.CreateViewModel(request, request.NavigationData);
            if (result.Success)
            {
                var viewModel = result.Result;
                view.DataContext = viewModel;
            }
            else return result;
        }
        return new NavigationResult(request.RouteSegment, view);
    }

    protected override async ValueTask WaitForVisualTree()
    {
        FrameworkElement target = Region!.Target!;
        TaskCompletionSource tcs = new();
        var dispatcher = Region.Scope.ServiceProvider.GetRequiredService<DispatcherQueue>();
        void Loaded(object? s, object? e)
        {
            dispatcher.TryEnqueue(DispatcherQueuePriority.Low, () => tcs.SetResult());
            //tcs.SetResult();
            target.Loaded -= Loaded;
        }
        target.Loaded += Loaded;
        await tcs.Task;
    }
}

public abstract class SelectorNavigator : Navigator
{
    protected abstract string SelectedName { get; }

    public SelectorNavigator(IServiceProvider serviceProvider) : base(serviceProvider) { }

    protected override sealed ValueTask<NavigationResult> NavigateCoreAsync(NavigationRequest request)
     => new(SelectItem(request));

    protected abstract NavigationResult SelectItem(NavigationRequest request);
}
