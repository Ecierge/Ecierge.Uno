namespace Ecierge.Uno.Navigation.Navigators;

using System.Threading.Tasks;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

public abstract class FactoryNavigator : Navigator
{
    public FactoryNavigator(IServiceProvider serviceProvider) : base(serviceProvider) { }

    public object CreateView(NavigationRequest request)
    {
        var viewMap = request.NameSegment.View!;
        var view = (FrameworkElement)ServiceProvider.GetRequiredService(viewMap.View);
        if (viewMap.ViewModel is Type viewModelType)
        {
            var viewModel = Scope.CreateViewModel(viewModelType, request.NavigationData);
            view.DataContext = viewModel;
        }
        return view;
    }
}

public abstract class SelectorNavigator : Navigator
{
    protected abstract string SelectedName { get; }

    public SelectorNavigator(IServiceProvider serviceProvider) : base(serviceProvider) { }

    public override sealed ValueTask<NavigationResponse> NavigateCoreAsync(NavigationRequest request)
     => new(SelectItem(request));

    public abstract NavigationResponse SelectItem(NavigationRequest request);
}
