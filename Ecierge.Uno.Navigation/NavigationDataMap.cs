namespace Ecierge.Uno.Navigation;

public class RouteValues : Dictionary<string, object> { }

public interface INavigationDataMap
{
    Type PrimitiveType { get; }
    Type EntityType { get; }
    Task<object> FromNavigationData(INavigationData data, string name);
    RouteData ToNavigationData(INavigationData? data, string name, object value);
}

public record struct RouteData(string Primitive, INavigationData NavigationData);
