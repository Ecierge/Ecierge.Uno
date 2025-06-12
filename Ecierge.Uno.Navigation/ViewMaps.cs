namespace Ecierge.Uno.Navigation;

using System;
using System.Collections.Generic;
using Microsoft.Extensions.DependencyInjection;

public abstract class ViewMapBase(Type view, Type? viewModel, IReadOnlyCollection<Type> additionalDependencies)
{
    public Type View { get; } = view;
    public Type? ViewModel { get; } = viewModel;

    public ViewMapBase With<TDependency>()
    {
        var dependencies = new List<Type>(additionalDependencies) {typeof(TDependency)};
        return CreateInstance(dependencies);
    }

    protected abstract ViewMapBase CreateInstance(IReadOnlyCollection<Type> dependencies);

    public IServiceCollection Register(IServiceCollection services)
    {
        foreach (var dependency in additionalDependencies)
        {
            services.AddTransient(dependency);
        }
        
        services.AddTransient(View);
        if (ViewModel is not null)
        {
            services.AddTransient(ViewModel);
        }
        
        return services;
    }
}

public class ViewMap<TView> : ViewMapBase
{
    private ViewMap(IReadOnlyCollection<Type> dependencies) : base(typeof(TView), null, dependencies)
    {
        
    }

    public ViewMap() : base(typeof(TView), null, [])
    {
    }

    protected override ViewMapBase CreateInstance(IReadOnlyCollection<Type> dependencies)
    {
        return new ViewMap<TView>(dependencies);
    }
}

public class ViewMap<TView, TViewModel> : ViewMapBase
{
    private ViewMap(IReadOnlyCollection<Type> dependencies) : base(typeof(TView), typeof(TViewModel), dependencies)
    {
        
    }

    public ViewMap() : this([])
    {
    }

    protected override ViewMapBase CreateInstance(IReadOnlyCollection<Type> dependencies)
    {
        return new ViewMap<TView, TViewModel>(dependencies);
    }
}
