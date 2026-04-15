namespace Ecierge.Uno.Navigation.Navigators;

using System;
using System.Threading.Tasks;

using Ecierge.Uno.Navigation;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Dispatching;

public abstract class Navigator<TTarget> : Navigator
    where TTarget : FrameworkElement
{
    private TTarget? _target;
    public new TTarget Target
    {
        get => _target ??= (TTarget)base.Target!;
        internal set => _target = value;
    }

    protected Navigator(IServiceProvider serviceProvider) : base(serviceProvider)
    {
        _target = (TTarget?)base.Target;
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
        
        // For singleton views, update navigator relationships instead of resetting
        // This preserves the NavigationRegion so child views can find their parent
        if (viewMap.IsStatic)
        {
            var existingRegion = view.GetNavigationRegion();
            if (existingRegion is not null)
            {
                // Update the navigator relationships for the cached scope
                existingRegion.Scope.ReassignNavigator(this);
            }
            // Don't call ResetNavigationRegion - children need to find this region
        }
        
        if (viewMap.ViewModel is { } viewModelType)
        {
            var result = Scope.CreateViewModel(request, request.Route.Data);
            if (result.Success)
            {
                var viewModel = result.Result;
                view.DataContext = viewModel;
                view.Unloaded += (_, _) =>
                {
                    if (viewModel is IDisposable disposable)
                    {
                        view.DataContext = null;
                        disposable.Dispose();
                    }
                };
            }
            else return result;
        }
        return new NavigationResult(request, view);
    }

    protected virtual FrameworkElement? WaitForVisualTreeTarget => Target;

    /// <summary>
    /// Waits for the content created by this navigator to be fully loaded,
    /// allowing child navigators to attach via AttachRegion.
    /// </summary>
    public override async ValueTask WaitForCreatedContentAsync()
    {
        FrameworkElement? target = WaitForVisualTreeTarget;
        if (target is null) return;
        if (!target.IsLoaded)
        {
            TaskCompletionSource tcs = new();
            void Loaded(object? s, RoutedEventArgs? e)
            {
                target.Loaded -= Loaded;
                // Use Low priority to ensure child AttachRegion has executed
                target.DispatcherQueue.TryEnqueue(DispatcherQueuePriority.Low, () => tcs.SetResult());
            }
            target.Loaded += Loaded;
            await tcs.Task;
        }
        else
        {
            // Content already loaded, but give a chance for any pending dispatcher work
            var tcs = new TaskCompletionSource();
            target.DispatcherQueue.TryEnqueue(DispatcherQueuePriority.Low, () => tcs.SetResult());
            await tcs.Task;
        }
    }

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
