namespace Ecierge.Uno.Navigation.Tests;

using Ecierge.Uno.Navigation;
using Ecierge.Uno.Navigation.Tests.Doubles;

/// <summary>
/// How a data map turns what a route holds under the name of a data segment into the handle of the entity, and how it
/// shares one handle between the pages of the segment.
/// </summary>
[TestClass, UnitTestCategory, NavigationTestCategory]
public class NavigationDataMapTests
{
    private const string Name = "BuildingInfo";

    private readonly BuildingServer server = new();

    private BuildingMap Map => new(server);

    private static INavigationData DataWith(object value) => NavigationData.Empty.Add(Name, value);

    [TestMethod]
    public void GetOrCreateEntity_must_reuse_the_handle_of_the_same_value()
    {
        var map = Map;
        var handle = map.CreateEntity("b1");

        var found = map.GetOrCreateEntity(DataWith(handle), Name, "b1");

        Assert.AreSame(handle, found, "Expected the pages of one building to share the handle, so that one reload is seen by all.");
    }

    [TestMethod]
    public void GetOrCreateEntity_must_ignore_the_case_of_the_value()
    {
        var map = Map;
        var handle = map.CreateEntity("B1");

        Assert.AreSame(handle, map.GetOrCreateEntity(DataWith(handle), Name, "b1"), "Expected an id in another case to be the same building.");
    }

    [TestMethod]
    public void GetOrCreateEntity_must_create_a_new_handle_for_another_value()
    {
        var map = Map;
        var handle = map.CreateEntity("b1");

        var found = map.GetOrCreateEntity(DataWith(handle), Name, "b2");

        Assert.AreNotSame(handle, found, "Expected another building to get its own handle.");
        Assert.AreEqual("b2", found.Primitive, "Expected the handle to be of the value that was asked for.");
        Assert.AreEqual("b1", handle.Primitive, "Expected the handle of the first building to stay as it was.");
    }

    [TestMethod]
    public void GetOrCreateEntity_must_create_a_handle_when_the_data_has_nothing_under_the_name()
    {
        var found = Map.GetOrCreateEntity(NavigationData.Empty, Name, "b1");

        Assert.AreEqual("b1", found.Primitive, "Expected a handle of the value.");
        Assert.IsFalse(found.IsLoaded, "Expected the handle not to load until somebody asks.");
        Assert.IsEmpty(server.Requests, "Expected reading a route not to cost a request.");
    }

    [TestMethod]
    public void GetOrCreateEntity_must_wrap_an_entity_of_the_same_value_in_a_loaded_handle()
    {
        var found = Map.GetOrCreateEntity(DataWith(new Building("b1", "Hall")), Name, "b1");

        Assert.IsTrue(found.IsLoaded, "Expected a handle made of an entity to be loaded.");
        Assert.IsEmpty(server.Requests, "Expected an entity that is at hand not to be requested.");
    }

    [TestMethod]
    public void GetOrCreateEntity_must_not_trust_a_task_that_has_not_succeeded()
    {
        var map = Map;
        var faulted = Task.FromException<Building>(new HttpRequestException("down"));

        var found = map.GetOrCreateEntity(DataWith(faulted), Name, "b1");

        Assert.IsFalse(found.IsLoaded, "Expected the failure not to be carried over: a handle loads again.");
    }

    [TestMethod]
    public void GetEntity_must_make_a_handle_of_whatever_the_data_holds()
    {
        var map = (INavigationDataMap)Map;
        var existing = Map.CreateEntity("b1");

        Assert.AreSame(existing, map.GetEntity(DataWith(existing), Name), "Expected a handle to be returned as it is.");
        Assert.IsTrue(map.GetEntity(DataWith(new Building("b2", "Hall")), Name).IsLoaded, "Expected an entity to give a loaded handle.");
        Assert.AreEqual("b3", map.GetEntity(DataWith("b3"), Name).Primitive, "Expected a text to give a handle of that value.");
        Assert.IsTrue(map.GetEntity(DataWith(Task.FromResult(new Building("b4", "Hall"))), Name).IsLoaded, "Expected a task that succeeded to give a loaded handle.");
    }

    [TestMethod]
    public void GetEntity_must_fail_for_a_missing_name_and_an_unsupported_value()
    {
        var map = (INavigationDataMap)Map;

        Assert.ThrowsExactly<KeyNotFoundException>(() => map.GetEntity(NavigationData.Empty, Name), "Expected nothing to be made of a missing name.");
        Assert.ThrowsExactly<NotSupportedException>(() => map.GetEntity(DataWith(42), Name), "Expected a value of another kind to be refused.");
    }

    [TestMethod]
    public void CreateEntity_of_an_object_must_accept_the_kinds_a_caller_may_give()
    {
        var map = (INavigationDataMap)Map;
        var handle = Map.CreateEntity("b1");

        Assert.AreSame(handle, map.CreateEntity(handle), "Expected a handle to be returned as it is.");
        Assert.IsTrue(map.CreateEntity(new Building("b2", "Hall")).IsLoaded, "Expected an entity to give a loaded handle.");
        Assert.AreEqual("b3", map.CreateEntity("b3").Primitive, "Expected a text to give a handle of that value.");
        Assert.AreEqual("b4", map.CreateEntity(Task.FromResult(new Building("b4", "Hall")), "b4").Primitive, "Expected a task to give a handle with the value that is given.");
        Assert.ThrowsExactly<ArgumentException>(() => map.CreateEntity(Task.FromResult(new Building("b5", "Hall"))), "Expected a task without its value to be refused, as a task cannot tell it.");
        Assert.ThrowsExactly<NotSupportedException>(() => map.CreateEntity(42), "Expected a value of another kind to be refused.");
    }

    [TestMethod]
    public void GetStringValue_must_write_a_handle_an_entity_and_a_text_as_the_value()
    {
        var map = (INavigationDataMap)Map;

        Assert.AreEqual("b1", map.GetStringValue(Map.CreateEntity("b1")), "Expected the value of a handle.");
        Assert.AreEqual("b2", map.GetStringValue(new Building("b2", "Hall")), "Expected the map to write an entity as its id.");
        Assert.AreEqual("b3", map.GetStringValue("b3"), "Expected a text to stay as it is.");
        Assert.AreEqual("b4", map.GetStringValue(DataWith(new Building("b4", "Hall")), Name), "Expected the value to be read from the data.");
    }

    [TestMethod]
    public async Task TryGetEntity_must_find_only_an_entity_that_is_at_hand()
    {
        var map = Map;
        var loaded = map.CreateEntity("b1");
        await loaded.GetAsync();
        var failed = map.CreateEntity(Task.FromException<Building>(new HttpRequestException("down")), "b2");

        Assert.IsTrue(map.TryGetEntity(DataWith(loaded), Name, out var entity) && entity!.Id == "b1", "Expected the entity of a handle that is loaded.");
        Assert.IsFalse(map.TryGetEntity(DataWith(failed), Name, out _), "Expected a failed handle to have no entity.");
        Assert.IsFalse(map.TryGetEntity(DataWith(map.CreateEntity("b3")), Name, out _), "Expected a handle that did not start loading to have no entity.");
        Assert.IsTrue(map.TryGetEntity(DataWith(new Building("b4", "Hall")), Name, out _), "Expected an entity in the data to be found.");
        Assert.IsTrue(map.TryGetEntity(DataWith(Task.FromResult(new Building("b5", "Hall"))), Name, out _), "Expected the result of a task that succeeded.");
        Assert.IsFalse(map.TryGetEntity(DataWith(new TaskCompletionSource<Building>().Task), Name, out _), "Expected a pending task to have no entity, and not to be waited for.");
        Assert.IsFalse(map.TryGetEntity(DataWith("b6"), Name, out _), "Expected a text to have no entity.");
        Assert.IsFalse(map.TryGetEntity(NavigationData.Empty, Name, out _), "Expected a missing name to have no entity.");
    }

    [TestMethod]
    public async Task GetEntityTask_of_a_failed_handle_must_load_again_when_the_server_is_back()
    {
        server.IsDown = true;
        var map = Map;
        var handle = map.CreateEntity("b1");
        await Assert.ThrowsExactlyAsync<HttpRequestException>(() => map.GetEntityTask(DataWith(handle), Name), "Expected the first attempt to fail while the server is down.");
        server.IsDown = false;

        await map.GetEntityTask(DataWith(handle), Name);

        Assert.IsTrue(handle.IsLoaded, "Expected the page that asks after the recovery to get the building.");
    }

    [TestMethod]
    public async Task TryGetEntityTask_of_a_handle_must_give_the_task_of_the_entity()
    {
        var map = Map;
        var handle = map.CreateEntity("b1");

        Assert.IsTrue(map.TryGetEntityTask(DataWith(handle), Name, out Task<Building>? task), "Expected a task for a handle.");
        Assert.AreEqual("b1", (await task!).Id, "Expected the task to load the building of the handle.");
        Assert.IsFalse(map.TryGetEntityTask(DataWith("b1"), Name, out Task<Building>? _), "Expected a text to have no task: it only says what to load.");
    }

    [TestMethod]
    public async Task GetEntityTask_of_a_text_must_load_the_entity_from_the_server()
    {
        var map = (INavigationDataMap)Map;

        await map.GetEntityTask(DataWith("b7"), Name);

        CollectionAssert.AreEqual(new[] { "b7" }, server.Requests, "Expected the building to be requested by its value.");
        Assert.ThrowsExactly<KeyNotFoundException>(() => map.GetEntityTask(NavigationData.Empty, Name), "Expected a missing name to be refused.");
    }
}
