using System.Threading.Tasks;

using Ecierge.Uno.Navigation;

namespace Ecierge.Uno.App.Models;

public record Entity(string Name);

internal class EntityViewDataMap : IViewDataMap
{
    public async ValueTask<object> FromRoute(Route route, string name)
    {
        route = route ?? throw new System.ArgumentNullException(nameof(route));
        return new Entity((route.Data![name] as string)!);
    }

    public void ToRoute(RouteValues routeValues, string name, object data)
    {
        routeValues = routeValues ?? throw new System.ArgumentNullException(nameof(routeValues));
        routeValues[name] = data;
    }
}
