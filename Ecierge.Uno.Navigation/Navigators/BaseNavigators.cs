namespace Ecierge.Uno.Navigation.Navigators;

using System;
using System.Threading.Tasks;

using Ecierge.Uno.Navigation;

using Microsoft.Extensions.DependencyInjection;

/// <summary>
/// A navigator whose region is a control of a known type.
/// </summary>
/// <typeparam name="TTarget">The type of the control of the region.</typeparam>
public abstract class Navigator<TTarget> : Navigator
    where TTarget : FrameworkElement
{
    private TTarget? _target;

    /// <summary>
    /// Gets the control of the region.
    /// </summary>
    /// <exception cref="InvalidOperationException">
    /// The region has no control of the type yet, as the region of a dialog that is not shown.
    /// </exception>
    public new TTarget Target
        => _target ??= base.Target as TTarget
            ?? throw new InvalidOperationException(
                $"The region '{Region.Segment.Name}' has no control of type {typeof(TTarget).Name}: {base.Target?.GetType().Name ?? "no control"}.");

    /// <summary>
    /// Creates a navigator.
    /// </summary>
    /// <param name="serviceProvider">The services of the scope of the region.</param>
    protected Navigator(IServiceProvider serviceProvider) : base(serviceProvider) { }
}

/// <summary>
/// A navigator that shows a new view in its region for every segment that it navigates to.
/// </summary>
/// <typeparam name="TTarget">The type of the control of the region.</typeparam>
public abstract class FactoryNavigator<TTarget> : Navigator<TTarget>
    where TTarget : FrameworkElement
{
    /// <summary>
    /// Creates a navigator.
    /// </summary>
    /// <param name="serviceProvider">The services of the scope of the region.</param>
    public FactoryNavigator(IServiceProvider serviceProvider) : base(serviceProvider) { }

    /// <summary>
    /// Creates the view of the request, with its view model when the segment has one.
    /// </summary>
    /// <param name="request">The request to create the view for.</param>
    /// <returns>The result with the view, or the errors if it could not be created.</returns>
    public NavigationResult CreateView(NavigationRequest request)
    {
        var viewMap = request.View;
        if (viewMap is null)
            return new NavigationResult($"The segment '{request.NameSegment.Name}' has no view");

        var view = (FrameworkElement)ServiceProvider.GetRequiredService(viewMap.View);
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
                return new NavigationResult(result.Request ?? request, view);
            }
            else return result;
        }
        return new NavigationResult(request, view);
    }

    /// <summary>
    /// Gets the control that has to be loaded before the content is considered shown.
    /// </summary>
    protected virtual FrameworkElement? WaitForVisualTreeTarget => Target;

    /// <inheritdoc/>
    protected override async ValueTask WaitForVisualTree()
    {
        FrameworkElement? target = WaitForVisualTreeTarget;
        if (target is null) return;
        if (!target.IsLoaded)
        {
            TaskCompletionSource tcs = new();
            void Loaded(object? s, RoutedEventArgs? e)
            {
                target.Loaded -= Loaded;
                if (!Dispatcher.TryEnqueue(NavigationDispatcherPriority.Low, () => tcs.TrySetResult()))
                    tcs.TrySetResult();
            }
            target.Loaded += Loaded;
            await tcs.Task;
        }
    }
}

/// <summary>
/// A navigator that selects one of the items of its control for every segment that it navigates to.
/// </summary>
/// <typeparam name="TTarget">The type of the control of the region.</typeparam>
public abstract class SelectorNavigator<TTarget> : Navigator<TTarget>
    where TTarget : FrameworkElement
{
    /// <summary>
    /// Gets the name of the segment that is selected.
    /// </summary>
    protected abstract string SelectedName { get; }

    /// <summary>
    /// Creates a navigator.
    /// </summary>
    /// <param name="serviceProvider">The services of the scope of the region.</param>
    public SelectorNavigator(IServiceProvider serviceProvider) : base(serviceProvider) { }

    /// <inheritdoc/>
    protected override sealed ValueTask<NavigationResult> NavigateCoreAsync(NavigationRequest request)
     => new(SelectItem(request));

    /// <summary>
    /// Selects the item of the segment of the request.
    /// </summary>
    /// <param name="request">The request to select the item for.</param>
    /// <returns>The result of the selection.</returns>
    protected abstract NavigationResult SelectItem(NavigationRequest request);
}
