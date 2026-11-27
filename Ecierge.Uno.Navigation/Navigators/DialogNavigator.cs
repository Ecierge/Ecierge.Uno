namespace Ecierge.Uno.Navigation.Navigators;

using System;
using System.Threading.Tasks;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

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
    /// The resources that hold the color of an outline, in the order they are tried: <c>OutlineBrush</c> of Material, then
    /// <c>CardStrokeColorDefaultBrush</c> and <c>ControlStrokeColorDefaultBrush</c> of Fluent.
    /// </summary>
    private static readonly string[] OutlineBrushKeys = ["OutlineBrush", "CardStrokeColorDefaultBrush", "ControlStrokeColorDefaultBrush"];

    /// <summary>
    /// Creates a navigator.
    /// </summary>
    /// <param name="serviceProvider">The services of the scope of the region.</param>
    public ContentDialogNavigator(IServiceProvider serviceProvider) : base(serviceProvider) { }

    /// <inheritdoc/>
    /// <exception cref="InvalidOperationException">
    /// The view of the dialog is neither a content dialog nor a control.
    /// </exception>
    protected override async Task<NavigationResult<IAsyncOperation<ContentDialogResult>>> DisplayDialog(NavigationRequest request)
    {
        var result = CreateView(request);
        if (!result.Success) return new NavigationResult<IAsyncOperation<ContentDialogResult>>(result.Errors);

        var dialog = result.Result switch
        {
            ContentDialog contentDialog => contentDialog,
            FrameworkElement view => CreateDialogAround(view),
            _ => throw new InvalidOperationException("Invalid dialog view")
        };
        ServiceProvider.SetScopedInstance<FrameworkElement>(dialog);

        // A dialog is shown in the root of the window of the application; one without it is not shown on every platform
        if (ServiceProvider.GetService<Window>()?.Content?.XamlRoot is { } xamlRoot)
        {
            dialog.XamlRoot = xamlRoot;
        }
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
        // Nobody awaits the operation, so a dialog that could not be shown would fail without a trace
        operation.Completed = (shown, status) =>
        {
            if (status == AsyncStatus.Error)
            {
                Logger.LogWarning(shown.ErrorCode, "The dialog of the segment '{SegmentName}' could not be shown", Region.Segment.Name);
            }
        };
        return new NavigationResult<IAsyncOperation<ContentDialogResult>>(request, operation);
    }

    /// <summary>
    /// Builds the dialog around a view that is not a content dialog. The view has no buttons, so the dialog gets the
    /// close button of <see cref="NavigationOptions.DialogCloseButtonText"/>: without a button nothing closes the dialog
    /// on a platform where the escape key does not. It also gets the outline of <see cref="NavigationOptions.DialogBorderThickness"/>,
    /// as a theme may draw a dialog without one.
    /// </summary>
    /// <param name="view">The view to show in the dialog.</param>
    /// <returns>The dialog.</returns>
    private ContentDialog CreateDialogAround(FrameworkElement view)
    {
        var options = ServiceProvider.GetService<IOptions<NavigationOptions>>()?.Value;
        var dialog = new ContentDialog { Content = view };
        if (!string.IsNullOrEmpty(options?.DialogCloseButtonText))
        {
            dialog.CloseButtonText = options.DialogCloseButtonText;
            dialog.DefaultButton = ContentDialogButton.Close;
        }
        if (options is { DialogBorderThickness: > 0 } && FindOutlineBrush() is { } outline)
        {
            dialog.BorderThickness = new Thickness(options.DialogBorderThickness);
            dialog.BorderBrush = outline;
        }
        return dialog;
    }

    /// <summary>
    /// Finds the brush that the theme of the application draws an outline with.
    /// </summary>
    /// <returns>The brush, or <see langword="null"/> when the theme has none of the brushes the library knows.</returns>
    private static Brush? FindOutlineBrush()
    {
        var resources = Application.Current?.Resources;
        if (resources is null) return null;
        foreach (var key in OutlineBrushKeys)
        {
            if (resources.TryGetValue(key, out var value) && value is Brush brush) return brush;
        }
        return null;
    }
}
