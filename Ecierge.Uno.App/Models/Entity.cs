namespace Ecierge.Uno.App.Models;

using System;
using System.Collections.Generic;
using System.Threading.Tasks;

using Ecierge.Uno.Navigation;

/// <summary>
/// An entity that a route carries by its id and that the breadcrumbs show by its name.
/// </summary>
/// <param name="Id">The id, which is written in the route.</param>
/// <param name="Name">The name that the user sees.</param>
public partial record Entity(string Id, string Name)
{
    /// <summary>
    /// Creates an entity from the name that the user typed, with an id that can be written in a route.
    /// </summary>
    /// <param name="name">The name.</param>
    /// <returns>The entity.</returns>
    public static Entity FromName(string name)
     => new(name.Trim().ToLowerInvariant().Replace(' ', '-'), name.Trim());
}

/// <summary>
/// Loads an entity by its id, as a server would, and names it for the breadcrumbs.
/// </summary>
internal class EntityNavigationDataMap : NavigationDataMap<Entity>
{
    private static readonly Dictionary<string, string> KnownNames = new(StringComparer.OrdinalIgnoreCase)
    {
        ["sunset-tower"] = "Sunset Tower",
        ["harbor-view"] = "Harbor View",
    };

    public override async Task<Entity> LoadEntityAsync(string primitive)
    {
        // A second of a request to a server: the breadcrumb shows the title of its segment until the name is known
        await Task.Delay(TimeSpan.FromSeconds(1)).ConfigureAwait(false);
        return new Entity(primitive, KnownNames.TryGetValue(primitive, out var name) ? name : primitive);
    }

    protected override string ToPrimitive(Entity entity) => entity.Id;

    public override string? GetDisplayName(Entity entity) => entity.Name;
}
