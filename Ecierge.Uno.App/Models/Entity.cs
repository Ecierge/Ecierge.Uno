namespace Ecierge.Uno.App.Models;

using System;
using System.Threading.Tasks;

using Ecierge.Uno.Navigation;

public record Entity(string Name);

internal class EntityNavigationDataMap : NavigationDataMap<Entity>
{
    public override Task<Entity> LoadEntityAsync(string primitive) => Task.FromResult(new Entity(primitive));

    protected override string ToPrimitive(Entity entity) => entity.Name;
}
