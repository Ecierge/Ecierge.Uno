namespace Ecierge.Uno.Navigation;

public record struct NavigationResult
{
    public RouteSegment? SegmentNavigated { get; private set; }
    public object? Result { get; private set; }
    public IReadOnlyList<string> Errors { get; private set; }
    public bool Success => SegmentNavigated is not null;

    public NavigationResult(RouteSegment segmentNavigated, object? result = null)
    {
        SegmentNavigated = segmentNavigated ?? throw new ArgumentNullException(nameof(segmentNavigated));
        Result = result;
        Errors = Array.Empty<string>();
    }

    public NavigationResult(params string[] errors) => Errors = errors ?? throw new ArgumentNullException(nameof(errors));
    public NavigationResult(IReadOnlyList<string> erros) => Errors = erros ?? throw new ArgumentNullException(nameof(erros));
}

public record struct NavigationResult<T>
{
    public RouteSegment? SegmentNavigated { get; private set; }
    public T? Result { get; private set; }
    public IReadOnlyList<string> Errors { get; private set; }
    public bool Success => SegmentNavigated is not null;

    public NavigationResult(RouteSegment segmentNavigated, T? result = default)
    {
        SegmentNavigated = segmentNavigated ?? throw new ArgumentNullException(nameof(segmentNavigated));
        Result = result;
        Errors = Array.Empty<string>();
    }

    public NavigationResult(params string[] args) => Errors = args ?? throw new ArgumentNullException(nameof(args));
    public NavigationResult(IReadOnlyList<string> args) => Errors = args ?? throw new ArgumentNullException(nameof(args));

    public static implicit operator NavigationResult(NavigationResult<T> result)
    {
        if (result.Success)
            return new(result.SegmentNavigated!, result.Result);
        else
            return new(result.Errors);
    }
}
