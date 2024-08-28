namespace Ecierge.Uno.Navigation.Navigators;

using System;
using System.Threading.Tasks;

using Microsoft.Extensions.DependencyInjection;

using Windows.Foundation;

using static System.Formats.Asn1.AsnWriter;

public abstract class DialogNavigator<TTarget, TResult> : FactoryNavigator<TTarget>
    where TTarget : FrameworkElement
{
    protected IAsyncOperation<TResult>? showTask;

    public DialogNavigator(IServiceProvider serviceProvider) : base(serviceProvider) { }

    protected abstract Task<NavigationResult<IAsyncOperation<TResult>>> DisplayDialog(NavigationRequest request);

    protected override async ValueTask<NavigationResult> NavigateCoreAsync(NavigationRequest request)
    {
        var result = await DisplayDialog(request);
        if (!result.Success) return result;

        showTask = result.Result;
        return new NavigationResult(request.RouteSegment);
    }

    protected void CloseDialog()
    {
        var dialog = showTask;
        showTask = null;
        dialog?.Cancel();
    }

    public Task CloseNavigator()
    {
        CloseDialog();
        return Task.CompletedTask;
    }

    public override ValueTask<NavigationResult> NavigateBackAsync(object initiator)
    {
        CloseDialog();
        Parent!.ChildNavigator = null;
        return new(new NavigationResult(Parent!.Region.Segment));
    }
}

public class ContentDialogNavigator : DialogNavigator<ContentDialog, ContentDialogResult>
{
    private static readonly Type ContentDialogType = typeof(ContentDialog);

    public ContentDialogNavigator(IServiceProvider serviceProvider) : base(serviceProvider) { }

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
#if WINDOWS
        dialog.XamlRoot = ServiceProvider.GetRequiredService<Window>().Content!.XamlRoot;
#endif
        var parentRegion = Parent!.Region;

        this.Region = new Regions.NavigationRegion(this.Scope)
        {
            Parent = parentRegion,
            Target = dialog,
            Root = parentRegion.Root
        };
        // To support navigation inside the dialog
        dialog.SetNavigationRegion(this.Region);

        void DialogClosed(ContentDialog s, ContentDialogClosedEventArgs e)
        {
            dialog.Closed -= DialogClosed;
            this.NavigateBackAsync(s);
        }
        dialog.Closed += DialogClosed;

        var showTask = dialog.ShowAsync();
        return new NavigationResult<IAsyncOperation<ContentDialogResult>>(request.RouteSegment, showTask);
    }
}
