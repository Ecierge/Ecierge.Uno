namespace Ecierge.Uno.Navigation;

public class RouteValues : Dictionary<string, object> { }

public interface INavigationDataMap
{
    static abstract Type PrimitiveType { get; }
    static abstract Type EntityType { get; }
    bool HasValue(INavigationData data, string name)
    {
        data = data ?? throw new System.ArgumentNullException(nameof(data));
        return data.ContainsKey(name);
    }
    Task<object> FromNavigationData(INavigationData data, string name);
    RouteData ToNavigationData(INavigationData? data, string name, object value);
}

public abstract class NavigationDataMap<TPrimitive, TObject> : INavigationDataMap
{
    public static Type PrimitiveType { get; } = typeof(TPrimitive);
    public static Type EntityType { get; } = typeof(TObject);

    protected abstract Task<TObject> FromPrimitive(TPrimitive primitive);
    protected abstract TPrimitive ToPrimitive(TObject obj);
    protected virtual string PrimitiveToString(TPrimitive primitive)
    {
        primitive = primitive ?? throw new System.ArgumentNullException(nameof(primitive));
        return primitive.ToString()!;
    }

    async Task<object> INavigationDataMap.FromNavigationData(INavigationData data, string name)
    {
        data = data ?? throw new System.ArgumentNullException(nameof(data));
        switch (data[name])
        {
            case TObject obj:
                return obj;
            case TPrimitive primitive:
                return (await FromPrimitive(primitive))!;
            default:
                throw new NotSupportedException();
        }
    }

    RouteData INavigationDataMap.ToNavigationData(INavigationData? data, string name, object value)
    {
        data = data ?? NavigationData.Empty;
        TPrimitive primitive;
        object obj;
        switch (value)
        {
            case TObject o:
                obj = o;
                primitive = ToPrimitive(o);
                break;
            case TPrimitive p:
                obj = p;
                primitive = p;
                break;
            default:
                throw new NotSupportedException();
        }
        return new RouteData(PrimitiveToString(primitive), data.SetItem(name, obj));
    }
}

public record struct RouteData(string Primitive, INavigationData NavigationData);
