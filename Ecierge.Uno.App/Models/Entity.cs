namespace Ecierge.Uno.App.Models;

using System;
using System.Threading.Tasks;

using Ecierge.Uno.Navigation;

public record Entity(string Name);

internal class EntityViewDataMap : INavigationDataMap
{
    public Type PrimitiveType { get; } = typeof(string);
    public Type EntityType { get; } = typeof(Entity);

    public async Task<object> FromNavigationData(INavigationData data, string name)
    {
        data = data ?? throw new System.ArgumentNullException(nameof(data));
        return new Entity((data[name] as string)!);
    }

    public void ToRoute(RouteValues routeValues, string name, object data)
    {
        routeValues = routeValues ?? throw new System.ArgumentNullException(nameof(routeValues));
        routeValues[name] = data;
    }
}
