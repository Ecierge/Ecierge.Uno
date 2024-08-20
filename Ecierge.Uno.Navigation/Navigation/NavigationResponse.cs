namespace Ecierge.Uno.Navigation;

public abstract record NavigationResponse
{
    public abstract bool Success { get; }

    public static NavigationResponse Failed { get; } = new FailedNavigationResponse();
}

public record FailedNavigationResponse : NavigationResponse
{
    internal FailedNavigationResponse() { }
    public override bool Success => false;
}

public record SuccessfulNavigationResponse(Routing.Route Route, Navigator Navigator) : NavigationResponse
{
    public override bool Success => true;
}

public record NoDefaultSegmentNavigationResponse(Routing.Route Route, Navigator Navigator) : NavigationResponse
{
    public override bool Success => true;
}

public record ResultNavigationResponse<TResult>(Routing.Route Route, Navigator Navigator, TResult Result) : NavigationResponse
{
    public override bool Success => true;
}
