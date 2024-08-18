namespace Ecierge.Uno.Navigation;

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Data;
using System.Text;

public class RouteValues : Dictionary<string, object> { }

public interface IViewDataMap
{
    ValueTask<object> FromRoute(Route route, string name);
    void ToRoute(RouteValues routeValues, string name, object data);
}

//public record ViewDataMap<TData>() : IViewDataMap
//{
//    public virtual Task<TData> FromRoute(Route route) => throw new NotSupportedException();
//    public virtual Task ToRoute(RouteValues routeValues, TData data) => throw new NotSupportedException();
//    async ValueTask<object> IViewDataMap.FromRoute(Route route)
//    {
//        route = route ?? throw new ArgumentNullException(nameof(route));
//        return await FromRoute(route)!;
//    }
//    void IViewDataMap.ToRoute(RouteValues routeValues, object data) => ToRoute(routeValues, (TData)data);
//}

public record ViewMap(
        Type View
    ,   Type? ViewModel = null
#pragma warning disable CA1819 // Properties should not return arrays
    // TODO: Replace with `ImmutableArray<string>` when it's available
    , params string[] AuthorizationPolicies
#pragma warning restore CA1819 // Properties should not return arrays
    );

public record ViewMap<TView>(
        Func<IServiceProvider, TView>? ViewFactory = null
    ) : ViewMap(typeof(TView));
public record ViewMap<TView, TViewModel>(
        Func<IServiceProvider, TView>? ViewFactory = null
    ,   Func<IServiceProvider, TViewModel>? ViewModelFactory = null
    ) : ViewMap(typeof(TView), typeof(TViewModel));

//public record DataViewMap<TView, TData>(
//        Func<IServiceProvider, TView>? ViewFactory = null
//    ) : ViewMap(typeof(TView), Data: new ViewDataMap<TData>());
//public record DataViewMap<TView, TViewModel, TData>(
//        Func<IServiceProvider, TView>? ViewFactory = null
//    ,   Func<IServiceProvider, TViewModel>? ViewModelFactory = null
//    ) : ViewMap(typeof(TView), typeof(TViewModel), new ViewDataMap<TData>());
