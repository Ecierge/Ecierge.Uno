namespace Ecierge.Uno.Navigation;

public class RouteValues : Dictionary<string, object> { }

public interface INavigationDataMap
{
    Type PrimitiveType { get; }
    Type EntityType { get; }
    Task<object> FromNavigationData(INavigationData data, string name);
    void ToRoute(RouteValues routeValues, string name, object data);
}
