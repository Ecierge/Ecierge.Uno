namespace Ecierge.Uno.Navigation.Routing;

using System;

public class InvalidRouteException : Exception
{
    protected InvalidRouteException() { }
    protected InvalidRouteException(string message) : base(message) { }
    protected InvalidRouteException(string message, Exception innerException) : base(message, innerException) { }
}

public class NestedSegmentNotFoundException : InvalidRouteException
{
    protected NestedSegmentNotFoundException() { }
    public NestedSegmentNotFoundException(RouteSegment parentSegment, string nestedSegment)
        : base($"The nested segment '{nestedSegment}' was not found in the parent segment '{parentSegment.Name}'.") { }
    protected NestedSegmentNotFoundException(string message) : base(message) { }
    protected NestedSegmentNotFoundException(string message, Exception innerException) : base(message, innerException) { }
}
