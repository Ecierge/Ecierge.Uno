namespace Ecierge.Uno.Navigation.Tests.Doubles;

using Ecierge.Uno.Navigation;

using Microsoft.Extensions.DependencyInjection;

// Inside Ecierge.Uno, a plain Uno.* name would resolve to the Ecierge.Uno namespace
using global::Uno.Extensions.Equality;

/// <summary>An entity that a route carries by its id.</summary>
[ImplicitKeys(IsEnabled = false)]
internal sealed record Building(string Id, string Name);

/// <summary>A view type for the view maps of the routes of a test.</summary>
public sealed class PageView { }

/// <summary>A second view type, for a segment whose view differs.</summary>
public sealed class BuildingPageView { }

/// <summary>
/// Stands for the server that the data maps load from. A test makes it fail and recover, and gives the response of a
/// request when it wants to hold it back.
/// </summary>
internal sealed class BuildingServer
{
    /// <summary>Gets or sets a value that makes every request fail, like a server that is down.</summary>
    public bool IsDown { get; set; }

    /// <summary>Gets the ids that were requested, in the order of the requests.</summary>
    public List<string> Requests { get; } = new();

    /// <summary>Gets or sets what answers a request when the server is up.</summary>
    public Func<string, Task<Building>> Answer { get; set; } = id => Task.FromResult(new Building(id, "Building " + id));

    public Task<Building> LoadAsync(string id)
    {
        Requests.Add(id);
        return IsDown
            ? Task.FromException<Building>(new HttpRequestException("The server is down"))
            : Answer(id);
    }
}

/// <summary>Loads a building from the server by its id, and writes it as its id.</summary>
internal sealed class BuildingMap(BuildingServer server) : NavigationDataMap<Building>
{
    public override Task<Building> LoadEntityAsync(string primitive) => server.LoadAsync(primitive);

    protected override string ToPrimitive(Building entity) => entity.Id;
}

/// <summary>The routes of the tests that need a building.</summary>
internal static class TestRoutes
{
    public const string Login = "Login";
    public const string Building = "Building";
    public const string BuildingData = "BuildingInfo";

    /// <summary>
    /// Creates a host for the routes below, with the server that the data maps load buildings from.
    /// </summary>
    /// <param name="server">The server.</param>
    /// <param name="configureServices">Adds or replaces services.</param>
    /// <param name="configureOptions">Adjusts the navigation options.</param>
    public static NavigationTestHost CreateHost(
        BuildingServer server,
        Action<IServiceCollection>? configureServices = null,
        Func<NavigationOptions, NavigationOptions>? configureOptions = null)
     => NavigationTestHost.Create(
            Register,
            services =>
            {
                services.AddSingleton(server);
                configureServices?.Invoke(services);
            },
            configureOptions);

    /// <summary>
    /// Login, and a building with the pages Menu, Assets, MyProperty (the default) and Details with its tabs Info
    /// (the default) and Floors, and the dialog AddAsset.
    /// </summary>
    public static void Register(IViewRegistryBuilder views, INavigationDataRegistryBuilder data, IRouteRegistryBuilder routes)
    {
        views.Register(new ViewMap<PageView>(), new ViewMap<BuildingPageView>());
        data.Register<BuildingMap>();
        routes.Register((v, d) => [
            new NameSegment(Login, v[typeof(PageView)]),
            new NameSegment(
                Building,
                v[typeof(BuildingPageView)],
                new DataSegment(
                    BuildingData,
                    d[typeof(Building)],
                    true,
                    new NameSegment("Menu", v[typeof(PageView)]),
                    new NameSegment("Assets", v[typeof(PageView)]),
                    new NameSegment("MyProperty", v[typeof(PageView)], isDefault: true),
                    new NameSegment(
                        "Details",
                        v[typeof(PageView)],
                        nested: [new NameSegment("Info", isDefault: true), new NameSegment("Floors")]),
                    new DialogSegment("AddAsset", v[typeof(PageView)])))
        ]);
    }
}
