namespace Ecierge.Uno.Navigation.Tests;

using System.Reflection;

using Ecierge.Uno.Navigation;
using Ecierge.Uno.Navigation.Tests.Doubles;

using Microsoft.Extensions.DependencyInjection;

/// <summary>
/// How the constructor of a page or view model gets the entity of a data segment: as the handle, as the task of the
/// entity, as the entity when it is loaded, and never as the faulted task of an earlier attempt.
/// </summary>
[TestClass, UnitTestCategory, NavigationTestCategory]
public class NavigationConstructorTests
{
    private readonly BuildingServer server = new();

    private sealed class TaskModel(Task<Building> buildingInfo)
    {
        public Task<Building> Building { get; } = buildingInfo;
    }

    private sealed class HandleModel(NavigationEntity<Building> buildingInfo)
    {
        public NavigationEntity<Building> Building { get; } = buildingInfo;
    }

    private sealed class InterfaceModel(INavigationEntity buildingInfo)
    {
        public INavigationEntity Building { get; } = buildingInfo;
    }

    private sealed class EntityModel(Building buildingInfo)
    {
        public Building Building { get; } = buildingInfo;
    }

    private sealed class AliasModel([NavigationParameter("BuildingInfo")] Building current)
    {
        public Building Current { get; } = current;
    }

    private sealed class OptionalModel(Building? buildingInfo = null, int count = 7)
    {
        public Building? Building { get; } = buildingInfo;

        public int Count { get; } = count;
    }

    private sealed class MissingModel(string notThere)
    {
        public string NotThere { get; } = notThere;
    }

    private IKeyedServiceProvider ServicesWith(INavigationData data)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton(server);
        services.AddNavigation(routeBuilder: TestRoutes.Register);

        // Registered last, so that it replaces the data of the scope: the data of the route the page is created for
        services.AddSingleton(data);
        return (IKeyedServiceProvider)services.BuildServiceProvider();
    }

    private T Create<T>(INavigationData data)
        where T : class
    {
        var constructor = typeof(T).GetNavigationConstructor(ServicesWith(data));
        Assert.IsNotNull(constructor, $"Expected the constructor of {typeof(T).Name} to be resolved.");
        return (T)constructor.Value.Constructor.Invoke(constructor.Value.Parameters);
    }

    private NavigationEntity<Building> Handle(string primitive = "b1") => new BuildingMap(server).CreateEntity(primitive);

    private static INavigationData DataWith(object value) => NavigationData.Empty.Add("BuildingInfo", value);

    [TestMethod]
    public async Task A_Task_parameter_must_get_the_task_of_the_entity()
    {
        var model = Create<TaskModel>(DataWith(Handle()));

        Assert.AreEqual("b1", (await model.Building).Id, "Expected the task to give the building of the handle.");
    }

    [TestMethod]
    public async Task A_Task_parameter_must_get_a_new_attempt_when_the_last_one_failed()
    {
        server.IsDown = true;
        var handle = Handle();
        await Assert.ThrowsExactlyAsync<HttpRequestException>(() => handle.GetAsync(), "Expected the first attempt to fail.");
        server.IsDown = false;

        var model = Create<TaskModel>(DataWith(handle));

        Assert.AreEqual("b1", (await model.Building).Id, "Expected a page that takes a task to get the building once the server is back, not the failure that was kept.");
    }

    [TestMethod]
    public void A_handle_parameter_must_get_the_shared_handle()
    {
        var handle = Handle();

        Assert.AreSame(handle, Create<HandleModel>(DataWith(handle)).Building, "Expected the page to hold the handle that the other pages hold.");
        Assert.AreSame(handle, Create<InterfaceModel>(DataWith(handle)).Building, "Expected the handle to be given for its interface too.");
    }

    [TestMethod]
    public async Task An_entity_parameter_must_get_the_entity_when_the_handle_is_loaded()
    {
        var handle = Handle();
        await handle.GetAsync();

        Assert.AreEqual("b1", Create<EntityModel>(DataWith(handle)).Building.Id, "Expected the loaded building.");
    }

    [TestMethod]
    public async Task An_entity_parameter_must_not_be_resolved_before_the_entity_is_loaded_or_after_the_load_failed()
    {
        server.IsDown = true;
        var failed = Handle();
        await Assert.ThrowsExactlyAsync<HttpRequestException>(() => failed.GetAsync(), "Expected the load to fail.");

        Assert.IsNull(typeof(EntityModel).GetNavigationConstructor(ServicesWith(DataWith(Handle("b2")))), "Expected a page that needs the building not to be created before it is loaded.");
        Assert.IsNull(typeof(EntityModel).GetNavigationConstructor(ServicesWith(DataWith(failed))), "Expected a page not to be created from a load that failed.");
    }

    [TestMethod]
    public async Task A_parameter_with_NavigationParameter_must_read_the_data_under_the_name_of_the_attribute()
    {
        var handle = Handle();
        await handle.GetAsync();

        Assert.AreEqual("b1", Create<AliasModel>(DataWith(handle)).Current.Id, "Expected the data to be found under the name of the attribute, not the name of the parameter.");
    }

    [TestMethod]
    public void An_optional_parameter_without_data_must_get_its_default()
    {
        var model = Create<OptionalModel>(NavigationData.Empty);

        Assert.IsNull(model.Building, "Expected no building when there is none.");
        Assert.AreEqual(7, model.Count, "Expected the default of the parameter.");
    }

    [TestMethod]
    public void A_mandatory_parameter_without_data_must_leave_the_constructor_unresolved()
    {
        Assert.IsNull(typeof(MissingModel).GetNavigationConstructor(ServicesWith(NavigationData.Empty)), "Expected a page that cannot get what it needs not to be created.");
    }

    [TestMethod]
    public async Task A_task_that_a_caller_put_in_the_data_must_still_be_accepted()
    {
        var model = Create<TaskModel>(DataWith(Task.FromResult(new Building("b1", "Hall"))));

        Assert.AreEqual("b1", (await model.Building).Id, "Expected routes that carry a task to keep working.");
    }

    [TestMethod]
    public void An_entity_that_a_caller_put_in_the_data_must_still_be_accepted()
    {
        Assert.AreEqual("b1", Create<EntityModel>(DataWith(new Building("b1", "Hall"))).Building.Id, "Expected routes that carry an entity to keep working.");
    }
}
