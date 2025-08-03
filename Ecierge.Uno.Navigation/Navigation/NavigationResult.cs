namespace Ecierge.Uno.Navigation;

public record struct NavigationResult
{
    public RouteSegment? SegmentNavigated { get; private set; }
    public object? Result { get; private set; }
    // The route already corresponds to the current route, no navigation necessary
    public bool IsSkipped { get; private set; }
    public IReadOnlyList<string> Errors { get; private set; }
    public bool Success => SegmentNavigated is not null;
    public NavigationRequest? Request { get; init; } = null;

    public NavigationResult(NavigationRequest request, object? result = null, bool isSkipped = false)
    {
        Request = request ?? throw new ArgumentNullException(nameof(request));
        SegmentNavigated = request.RouteSegment;
        Result = result;
        IsSkipped = isSkipped;
        Errors = Array.Empty<string>();
    }

    public NavigationResult(params string[] errors) => Errors = errors ?? throw new ArgumentNullException(nameof(errors));
    public NavigationResult(IReadOnlyList<string> errors) => Errors = errors ?? throw new ArgumentNullException(nameof(errors));
}

public record struct NavigationResult<T>
{
    public RouteSegment? SegmentNavigated { get; private set; }
    public T? Result { get; private set; }
    public IReadOnlyList<string> Errors { get; private set; }
    public bool Success => SegmentNavigated is not null;
    public NavigationRequest? Request { get; init; } = null;

    public NavigationResult(NavigationRequest request, T? result = default)
    {
        Request = request ?? throw new ArgumentNullException(nameof(request));
        SegmentNavigated = request.RouteSegment;
        Result = result;
        Errors = Array.Empty<string>();
    }

    public NavigationResult(params string[] args) => Errors = args ?? throw new ArgumentNullException(nameof(args));
    public NavigationResult(IReadOnlyList<string> args) => Errors = args ?? throw new ArgumentNullException(nameof(args));

    public static implicit operator NavigationResult(NavigationResult<T> result)
    {
        if (result.Success)
            return new(result.Request!, result.Result);
        else
            return new(result.Errors);
    }
}
