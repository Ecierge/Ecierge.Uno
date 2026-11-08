namespace Ecierge.Uno.Navigation.Controls;

using System;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

using Ecierge.Uno.Navigation.Routing;

/// <summary>
/// Finds the names of the entities of a route for its breadcrumbs. It remembers the last name of each entity, so that
/// a breadcrumb keeps the name while its entity loads again instead of falling back to the title of the segment.
/// </summary>
/// <param name="services">The services that the data maps are taken from.</param>
internal sealed class BreadcrumbNames(IServiceProvider services)
{
    // Weak, so that the handles of the routes that are left are not kept alive by the bar
    private readonly ConditionalWeakTable<INavigationEntity, string> known = new();

    /// <summary>
    /// Gets the name of the entity of a data step: the name that the data map of its segment gives for the loaded
    /// entity, or the last such name while the entity loads again.
    /// </summary>
    /// <param name="step">The data step.</param>
    /// <param name="data">The data of the route.</param>
    /// <returns>
    /// The name, or <see langword="null"/> if the entity has not loaded yet, its load failed or its map gives no name.
    /// </returns>
    public string? DisplayNameOf(DataSegmentInstance step, INavigationData data)
    {
        if (!data.TryGetValue(step.DataSegment.Name, out var value)) return null;

        var map = step.DataSegment.DataMap is { } mapType ? services.GetService(mapType) as INavigationDataMap : null;
        switch (value)
        {
            case INavigationEntity handle:
                if (handle.TryGetLoadedEntity(out var entity) && NameOf(map, entity) is { } name)
                {
                    known.AddOrUpdate(handle, name);
                    return name;
                }

                return known.TryGetValue(handle, out var last) ? last : null;
            // The value of the step as text, or the task of an entity: there is no entity to name yet
            case string or Task:
                return null;
            default:
                return NameOf(map, value);
        }
    }

    private static string? NameOf(INavigationDataMap? map, object entity)
     => map?.GetDisplayName(entity) is { Length: > 0 } name ? name : null;
}
