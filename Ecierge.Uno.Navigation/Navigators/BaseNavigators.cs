namespace Ecierge.Uno.Navigation.Navigators;

using System;
using System.Threading.Tasks;

using CommunityToolkit.WinUI;

using Ecierge.Uno.Navigation;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.UI.Dispatching;

public abstract class Navigator<TTarget> : Navigator
    where TTarget : FrameworkElement
{
    new public TTarget? Target { get; internal set; }

    protected Navigator(IServiceProvider serviceProvider) : base(serviceProvider)
    {
        Target = (TTarget?)base.Target;
    }
}

public abstract class FactoryNavigator<TTarget> : Navigator<TTarget>
    where TTarget : FrameworkElement
{
    public FactoryNavigator(IServiceProvider serviceProvider) : base(serviceProvider) { }

    public NavigationResult CreateView(NavigationRequest request)
    {
        var viewMap = request.View!;
        var view = (FrameworkElement)ServiceProvider.GetRequiredService(viewMap.View);
        if (viewMap.ViewModel is { } viewModelType)
        {
            var result = Scope.CreateViewModel(request, request.Route.Data);
            if (result.Success)
            {
                var viewModel = result.Result;
                view.DataContext = viewModel;
            }
            else return result;
        }
        return new NavigationResult(request.RouteSegment, view);
    }

    protected virtual FrameworkElement? WaitForVisualTreeTarget => Target;

    protected override async ValueTask WaitForVisualTree()
    {
        FrameworkElement? target = WaitForVisualTreeTarget;
        if (target is null) return;
        if (!target.IsLoaded)
        {
            TaskCompletionSource tcs = new();
            void Loaded(object? s, RoutedEventArgs? e)
            {
                target.DispatcherQueue.TryEnqueue(DispatcherQueuePriority.Low, () => tcs.SetResult());
                target.Loaded -= Loaded;
            }
            target.Loaded += Loaded;
            await tcs.Task;
        }
    }
}

public abstract class SelectorNavigator<TTarget> : Navigator<TTarget>
    where TTarget : FrameworkElement
{
    protected abstract string SelectedName { get; }

    public SelectorNavigator(IServiceProvider serviceProvider) : base(serviceProvider) { }

    protected override sealed ValueTask<NavigationResult> NavigateCoreAsync(NavigationRequest request)
     => new(SelectItem(request));

    protected abstract NavigationResult SelectItem(NavigationRequest request);
}
