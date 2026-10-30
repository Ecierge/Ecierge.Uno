namespace Ecierge.Uno.Navigation.Navigators;

using System;
using System.Threading.Tasks;

using Microsoft.Extensions.DependencyInjection;

using Windows.Foundation;

/// <summary>
/// A navigator that shows a dialog over the page.
/// </summary>
public interface IDialogNavigator
{
    /// <summary>
    /// Closes the dialog and detaches the navigator of the dialog from the navigator it was requested through.
    /// </summary>
    /// <returns>A task that completes when the dialog is closed.</returns>
    Task CloseNavigator();
}

/// <summary>
/// A navigator that shows the view of a segment as a dialog over the page.
/// </summary>
/// <typeparam name="TTarget">The type of the control of the dialog.</typeparam>
/// <typeparam name="TResult">The type of the result that the dialog gives when it is closed.</typeparam>
public abstract class DialogNavigator<TTarget, TResult> : FactoryNavigator<TTarget>, IDialogNavigator
    where TTarget : FrameworkElement
{
    /// <summary>The operation that shows the dialog.</summary>
    protected IAsyncOperation<TResult>? showTask;

    /// <summary>
    /// Creates a navigator.
    /// </summary>
    /// <param name="serviceProvider">The services of the scope of the region.</param>
    public DialogNavigator(IServiceProvider serviceProvider) : base(serviceProvider) { }

    /// <summary>
    /// Creates and shows the dialog of the request.
    /// </summary>
    /// <param name="request">The request to show the dialog for.</param>
    /// <returns>The result with the operation that shows the dialog.</returns>
    protected abstract Task<NavigationResult<IAsyncOperation<TResult>>> DisplayDialog(NavigationRequest request);

    /// <inheritdoc/>
    protected override async ValueTask<NavigationResult> NavigateCoreAsync(NavigationRequest request)
    {
        var result = await DisplayDialog(request);
        if (!result.Success) return result;

        showTask = result.Result;
        return new NavigationResult(request);
    }

    /// <summary>
    /// Closes the dialog.
    /// </summary>
    protected void CloseDialog()
    {
        var dialog = showTask;
        showTask = null;
        dialog?.Cancel();
    }

    /// <summary>
    /// Closes the dialog and detaches the navigator of the dialog from the navigator it was requested through.
    /// </summary>
    /// <returns>A completed task.</returns>
    public Task CloseNavigator()
    {
        // The history is moved by whoever closes the dialog like this, so the event of the closed dialog must not
        // move it again
        ClosedByNavigation = true;
        CloseDialog();
        DetachFromParent();
        return Task.CompletedTask;
    }

    /// <summary>
    /// Gets a value that tells that the dialog was closed by a navigation and not by the user.
    /// </summary>
    protected bool ClosedByNavigation { get; private set; }

    /// <summary>
    /// Detaches the navigator of the dialog from the navigator it was requested through, so that the page below the
    /// dialog is the last region again.
    /// </summary>
    protected void DetachFromParent()
    {
        if (Parent is { } parent && ReferenceEquals(parent.ChildNavigator, this))
        {
            parent.ChildNavigator = null;
        }
    }
}

/// <summary>
/// A navigator that shows the view of a segment as a <see cref="ContentDialog"/>.
/// </summary>
public class ContentDialogNavigator : DialogNavigator<ContentDialog, ContentDialogResult>
{
    /// <summary>
    /// Creates a navigator.
    /// </summary>
    /// <param name="serviceProvider">The services of the scope of the region.</param>
    public ContentDialogNavigator(IServiceProvider serviceProvider) : base(serviceProvider) { }

    /// <inheritdoc/>
    protected override async Task<NavigationResult<IAsyncOperation<ContentDialogResult>>> DisplayDialog(NavigationRequest request)
    {
        var result = CreateView(request);
        if (!result.Success) return new NavigationResult<IAsyncOperation<ContentDialogResult>>(result.Errors);

        var dialog = result.Result switch
        {
            ContentDialog contentDialog => contentDialog,
            // TODO: Implement more dialog settings
            FrameworkElement view => new ContentDialog { Content = view },
            _ => throw new InvalidOperationException("Invalid dialog view")
        };
        ServiceProvider.AddScopedInstance<FrameworkElement>(dialog);

#if WINDOWS
        dialog.XamlRoot = ServiceProvider.GetRequiredService<Window>().Content!.XamlRoot;
#endif
        // The region of the dialog is created before the dialog, so the dialog is its control from now on
        Region.Target = dialog;
        // To support navigation inside the dialog
        dialog.SetNavigationRegion(Region);

        void DialogClosed(ContentDialog s, ContentDialogClosedEventArgs e)
        {
            dialog.Closed -= DialogClosed;
            this.showTask = null;
            DetachFromParent();
            if (!ClosedByNavigation)
            {
                // The user closed the dialog with one of its buttons: closing is going back
                DialogHistory.OnClosedByUser(this);
            }
        }
        dialog.Closed += DialogClosed;

        var operation = dialog.ShowAsync();
        return new NavigationResult<IAsyncOperation<ContentDialogResult>>(request, operation);
    }
}
