namespace Ecierge.Uno.Navigation;

using System;

public class NavigationRegionMissingException : Exception
{
    public NavigationRegionMissingException() { }
    public NavigationRegionMissingException(FrameworkElement element) : base($"Framework element {element.Name ?? element.GetType().Name} does not have navigation region") { }
    public NavigationRegionMissingException(Navigator navigator) : base($"Navigator {navigator} does not have navigation region set") { }
    protected NavigationRegionMissingException(string message) : base(message) { }
    protected NavigationRegionMissingException(string message, Exception innerException) : base(message, innerException) { }
}

//public class ParentNavigationRegionMissingException : NavigationRegionMissingException
//{
//    private ParentNavigationRegionMissingException() { }
//    public ParentNavigationRegionMissingException(Navigator navigator) : base($"Navigator {navigator} does not have ") { }
//    private ParentNavigationRegionMissingException(string message) : base(message) { }
//    private ParentNavigationRegionMissingException(string message, Exception innerException) : base(message, innerException) { }
//}

public class RootNavigationRegionMissingException : NavigationRegionMissingException
{
    public RootNavigationRegionMissingException() { }
    private RootNavigationRegionMissingException(string message) : base(message) { }
    private RootNavigationRegionMissingException(string message, Exception innerException) : base(message, innerException) { }
}
