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

    public NavigationResult(params string[] args) => Errors = args ?? throw new ArgumentNullException(nameof(args));
}
