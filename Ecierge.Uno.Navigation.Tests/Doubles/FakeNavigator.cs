namespace Ecierge.Uno.Navigation.Tests.Doubles;

using Ecierge.Uno.Navigation;
using Ecierge.Uno.Navigation.Navigators;

using Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Decides what the fake navigators do: which segments have a region inside their view, and which requests fail.
/// </summary>
internal sealed class FakeRegionPlan
{
    /// <summary>
    /// Gets or sets the type of the navigator of the region inside the view of a segment, or
    /// <see langword="null"/> if the view has no region. By default every segment that has nested segments has one.
    /// </summary>
    public Func<NameSegment, Type?> ChildNavigatorType { get; set; } =
        segment => segment.Nested.Length > 0 || segment.DataSegment is { Nested.Length: > 0 }
            ? typeof(FakeNavigator)
            : null;

    /// <summary>
    /// Gets or sets a function that may give a result of its own for a request, for example a failure.
    /// </summary>
    public Func<FakeNavigator, NavigationRequest, NavigationResult?>? Intercept { get; set; }

    /// <summary>
    /// Gets the requests of all the navigators in the order in which they were navigated.
    /// </summary>
    public List<(FakeNavigator Navigator, NavigationRequest Request)> Navigated { get; } = new();
}

/// <summary>
/// A navigator without a control. It records its requests, and attaches the region that the view of the navigated
/// segment would attach, as the control navigators do when their content is loaded.
/// </summary>
internal class FakeNavigator : Navigator
{
    private bool lastResultWasSkipped;

    public FakeNavigator(IServiceProvider serviceProvider) : base(serviceProvider)
    {
        Plan = serviceProvider.GetRequiredService<FakeRegionPlan>();
    }

    public FakeRegionPlan Plan { get; }

    public List<NavigationRequest> Requests { get; } = new();

    /// <summary>Gets the name of the segment that the navigator navigated to last.</summary>
    public string? NavigatedName { get; private set; }

    protected override ValueTask<NavigationResult> NavigateCoreAsync(NavigationRequest request)
    {
        Requests.Add(request);
        Plan.Navigated.Add((this, request));

        // The view model of the page that owns the entity starts loading it, as the real navigator does
        if (request is PrimitiveDataSegmentNavigationRequest dataRequest
            && request.Route.Data.TryGetValue(dataRequest.Segment.Name, out var held)
            && held is INavigationEntity entity)
        {
            _ = entity.LoadTask;
        }
        var result = Plan.Intercept?.Invoke(this, request) ?? new NavigationResult(request);
        lastResultWasSkipped = result.IsSkipped;
        if (result.Success)
        {
            NavigatedName = request.NameSegment.Name;
        }

        return new(result);
    }

    protected override ValueTask WaitForVisualTree()
    {
        if (!lastResultWasSkipped
            && Route.LastNamedSegment?.NameSegment is { } segment
            && Plan.ChildNavigatorType(segment) is { } navigatorType)
        {
            Scope.CreateScope(this, segment, navigatorType);
        }

        return ValueTask.CompletedTask;
    }
}

/// <summary>
/// A navigator of a dialog without a control: it is open from the time it navigates until it is closed.
/// </summary>
internal sealed class FakeDialogNavigator : Navigator, IDialogNavigator
{
    public FakeDialogNavigator(IServiceProvider serviceProvider) : base(serviceProvider) { }

    public bool IsOpen { get; private set; }

    public int CloseCount { get; private set; }

    protected override ValueTask<NavigationResult> NavigateCoreAsync(NavigationRequest request)
    {
        IsOpen = true;
        return new(new NavigationResult(request));
    }

    public Task CloseNavigator()
    {
        if (IsOpen) CloseCount++;
        IsOpen = false;
        if (Parent is { } parent && ReferenceEquals(parent.ChildNavigator, this))
        {
            parent.ChildNavigator = null;
        }

        return Task.CompletedTask;
    }

    /// <summary>Does what a dialog does when the user closes it with one of its own buttons.</summary>
    public void CloseByUser()
    {
        IsOpen = false;
        if (Parent is { } parent && ReferenceEquals(parent.ChildNavigator, this))
        {
            parent.ChildNavigator = null;
        }

        DialogHistory.OnClosedByUser(this);
    }
}
