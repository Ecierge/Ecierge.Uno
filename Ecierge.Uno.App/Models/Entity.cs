namespace Ecierge.Uno.App.Models;

using System;
using System.Threading.Tasks;

using Ecierge.Uno.Navigation;

public record Entity(string Name);

internal class EntityNavigationDataMap : INavigationDataMap
{
    public static Type PrimitiveType { get; } = typeof(string);
    public static Type EntityType { get; } = typeof(Entity);

    public Task<object> FromNavigationData(INavigationData data, string name)
    {
        data = data ?? throw new System.ArgumentNullException(nameof(data));
        var value = data[name] switch
        {
            string s => new Entity(s),
            Entity entity => entity,
            _ => throw new System.InvalidOperationException()
        };
        return Task.FromResult<object>(value);
    }

    public RouteData ToNavigationData(INavigationData? data, string name, object value)
    {
        data = data ?? NavigationData.Empty;
        return new RouteData((value as Entity)!.Name, data.Add(name, value));
    }
}
