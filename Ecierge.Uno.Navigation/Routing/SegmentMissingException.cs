namespace Ecierge.Uno.Navigation.Routing;

public class SegmentMissingException : Exception
{
    protected SegmentMissingException() { }
    protected SegmentMissingException(string message) : base(message) { }
    protected SegmentMissingException(string message, Exception innerException) : base(message, innerException) { }
}

public class NestedSegmentMissingException : SegmentMissingException
{
    protected NestedSegmentMissingException() { }
    public NestedSegmentMissingException(string segmentName, string parentName) : base($"Nested segment '{segmentName}' not found in parent segment '{parentName}'") { }
    protected NestedSegmentMissingException(string message) : base(message) { }
    protected NestedSegmentMissingException(string message, Exception innerException) : base(message, innerException) { }
}

public class ParentSegmentMissingException : SegmentMissingException
{
    protected ParentSegmentMissingException() { }
    public ParentSegmentMissingException(FrameworkElement element) : base($"FrameworkElement '{element.Name ?? element.GetType().Name}' does not have a parent segment") { }
    protected ParentSegmentMissingException(string message) : base(message) { }
    protected ParentSegmentMissingException(string message, Exception innerException) : base(message, innerException) { }
}

public class RootSegmentMissingException : SegmentMissingException
{
    public RootSegmentMissingException() { }
    protected RootSegmentMissingException(string message) : base(message) { }
    protected RootSegmentMissingException(string message, Exception innerException) : base(message, innerException) { }
}
