namespace Ecierge.Uno.Navigation;

using System;
using System.Collections.Generic;

using Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Base class for view maps, which associate a view type with an optional view model type and additional dependencies.
/// </summary>
/// <param name="view">The type of the view.</param>
/// <param name="viewModel">The type of the view model, if any.</param>
/// <param name="additionalDependencies">A collection of additional dependency types.</param>
public abstract class ViewMapBase(Type view, Type? viewModel, IEnumerable<Type> additionalDependencies)
{
    /// <summary>
    /// Gets the type of the view associated with this map.
    /// </summary>
    public Type View { get; } = view;
    /// <summary>
    /// Gets the type of the view model associated with this map, if any.
    /// </summary>
    public Type? ViewModel { get; } = viewModel;
    /// <summary>
    /// Gets the type of the view model associated with this map, if any.
    /// </summary>
    public bool IsStatic { get; } = false;
    /// <summary>
    /// Gets the type of the view model associated with this map, if any.
    /// </summary>
    public IEnumerable<Type> AdditionalDependencies { get; } = additionalDependencies;

    /// <summary>
    /// Adds an additional dependency type to the view map.
    /// </summary>
    /// <typeparam name="TDependency">The type of the additional dependency to add.</typeparam>
    /// <returns>A new instance of <see cref="ViewMapBase"/> with the added dependency.</returns>
    public ViewMapBase With<TDependency>()
    {
        var dependencies = new List<Type>(additionalDependencies) { typeof(TDependency) };
        return CreateInstance(dependencies);
    }

    /// <summary>
    /// Creates a new instance of the view map with the specified additional dependencies.
    /// </summary>
    /// <param name="dependencies">The collection of additional dependencies.</param>
    /// <returns>A new instance of <see cref="ViewMapBase"/> derived type with the specified dependencies.</returns>
    protected abstract ViewMapBase CreateInstance(IReadOnlyCollection<Type> dependencies);

    /// <summary>
    /// Registers the view and view model types in the provided service collection.
    /// </summary>
    /// <param name="services">The service collection to register view and view model types with.</param>
    /// <returns>An instance of <see cref="IServiceCollection"/> with the registered view and view model types.</returns>
    public virtual IServiceCollection Register(IServiceCollection services)
    {
        foreach (var dependency in additionalDependencies)
        {
            services.AddTransient(dependency);
        }

        services.AddTransient(View);
        if (ViewModel is not null)
        {
            services.AddTransientWithNavigationParameters(ViewModel);
        }

        return services;
    }
}

/// <summary>
/// Represents a view map for a specific view type, optionally with a view model type and additional dependencies.
/// </summary>
/// <typeparam name="TView">View type to associate with this map.</typeparam>
public class ViewMap<TView> : ViewMapBase
{
    private ViewMap(IReadOnlyCollection<Type> dependencies) : base(typeof(TView), null, dependencies) { }

    /// <summary>
    /// Initializes a new instance of the <see cref="ViewMap{TView}"/> class with no additional dependencies.
    /// </summary>
    public ViewMap() : base(typeof(TView), null, []) { }

    /// <inheritdoc />
    protected override ViewMapBase CreateInstance(IReadOnlyCollection<Type> dependencies) => new ViewMap<TView>(dependencies);
}

/// <summary>
/// Represents a view map for a specific view type and its corresponding view model type, with optional additional dependencies.
/// </summary>
/// <typeparam name="TView">View type to associate with this map.</typeparam>
/// <typeparam name="TViewModel">View model type to associate with this map.</typeparam>
public class ViewMap<TView, TViewModel> : ViewMapBase
{
    private ViewMap(IReadOnlyCollection<Type> dependencies) : base(typeof(TView), typeof(TViewModel), dependencies) { }

    /// <summary>
    /// Initializes a new instance of the <see cref="ViewMap{TView, TViewModel}"/> class with no additional dependencies.
    /// </summary>
    public ViewMap() : this([]) { }

    /// <summary>
    /// Creates a new instance of the view map with the specified additional dependencies.
    /// </summary>
    /// <param name="dependencies">The collection of additional dependencies.</param>
    /// <returns>A new instance of <see cref="ViewMapBase"/> derived type with the specified dependencies.</returns>
    protected override ViewMapBase CreateInstance(IReadOnlyCollection<Type> dependencies) => new ViewMap<TView, TViewModel>(dependencies);
}

/// <summary>
/// Represents a view map for a specific view type and its corresponding view model type, with optional additional dependencies.
/// </summary>
/// <typeparam name="TView">View type to associate with this map.</typeparam>
/// <typeparam name="TViewModel">View model type to associate with this map.</typeparam>
public class StaticViewMap<TView, TViewModel> : ViewMapBase
{
    private static TView? _instance;

    private StaticViewMap(IReadOnlyCollection<Type> dependencies) : base(typeof(TView), typeof(TViewModel), dependencies) { }

    /// <summary>
    /// Initializes a new instance of the <see cref="ViewMap{TView, TViewModel}"/> class with no additional dependencies.
    /// </summary>
    public StaticViewMap() : this([]) { }
    /// <summary>
    /// Gets the type of the view model associated with this map, if any.
    /// </summary>
    public bool IsStatic { get; } = true;


    /// <summary>
    /// Creates a new instance of the view map with the specified additional dependencies.
    /// </summary>
    /// <param name="dependencies">The collection of additional dependencies.</param>
    /// <returns>A new instance of <see cref="ViewMapBase"/> derived type with the specified dependencies.</returns>
    protected override ViewMapBase CreateInstance(IReadOnlyCollection<Type> dependencies) => new StaticViewMap<TView, TViewModel>(dependencies);

    public override IServiceCollection Register(IServiceCollection services)
    {
        foreach (var dependency in this.AdditionalDependencies)
        {
            services.AddTransient(dependency);
        }
        services.AddTransient(View, x =>
        {
            if (_instance == null)
            {
                try
                {
                    var constructor = View.GetConstructors().FirstOrDefault();
                    if (constructor == null)
                    {
                        throw new InvalidOperationException($"No public constructor found for static view '{View.FullName}'. Have you forgotten to mark it as public?");
                    }
                    var parameters = constructor.GetParameters();
                    var args = parameters.Select(p => x.GetRequiredService(p.ParameterType)).ToArray();
                    var viewInstance = constructor.Invoke(args);
                    _instance = (TView)viewInstance;
                }
                catch (Exception e)
                {
                    throw new InvalidOperationException($"Failed to create instance of static view '{View.FullName}'.", e);
                }
            }
            return _instance;
        });
        if (ViewModel is not null)
        {
            services.AddTransientWithNavigationParameters(ViewModel);
        }

        return services;
    }
}
