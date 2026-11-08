namespace Ecierge.Uno.Navigation.Tests;

using Ecierge.Uno.Navigation;

[TestClass, UnitTestCategory, NavigationTestCategory]
public class NavigationEntityTests
{
    private sealed class Loader
    {
        public int Calls { get; private set; }

        public Func<string, Task<string>> Answer { get; set; } = primitive => Task.FromResult("entity " + primitive);

        public Task<string> Load(string primitive)
        {
            Calls++;
            return Answer(primitive);
        }
    }

    [TestMethod]
    public void A_new_handle_must_not_start_loading()
    {
        var loader = new Loader();

        var handle = new NavigationEntity<string>("id-1", loader.Load);

        Assert.AreEqual(0, loader.Calls, "Expected reading a route not to cost a request.");
        Assert.IsFalse(handle.IsLoaded, "Expected a handle that was not asked for its entity not to be loaded.");
        Assert.AreEqual("id-1", handle.Primitive, "Expected the value to be available without loading.");
    }

    [TestMethod]
    public async Task LoadTask_must_start_the_first_attempt_once()
    {
        var loader = new Loader();
        var handle = new NavigationEntity<string>("id-1", loader.Load);

        var first = handle.LoadTask;
        var second = handle.LoadTask;
        var entity = await first;

        Assert.AreSame(first, second, "Expected the same attempt to be the current one.");
        Assert.AreEqual(1, loader.Calls, "Expected one request for any number of readers.");
        Assert.AreEqual("entity id-1", entity, "Expected the loaded entity.");
        Assert.IsTrue(handle.IsLoaded, "Expected the handle to be loaded after the attempt succeeded.");
    }

    [TestMethod]
    public async Task GetAsync_must_return_the_same_task_while_the_load_is_pending_or_succeeded()
    {
        var pending = new TaskCompletionSource<string>();
        var loader = new Loader { Answer = _ => pending.Task };
        var handle = new NavigationEntity<string>("id-1", loader.Load);

        var whilePending = handle.GetAsync();
        Assert.AreSame(whilePending, handle.GetAsync(), "Expected a pending load not to be started again.");

        pending.SetResult("loaded");
        await whilePending;

        Assert.AreSame(whilePending, handle.GetAsync(), "Expected a loaded entity not to be loaded again.");
        Assert.AreEqual(1, loader.Calls, "Expected one request in all.");
    }

    [TestMethod]
    public async Task GetAsync_must_start_a_new_attempt_after_a_failure_and_raise_Changed()
    {
        var loader = new Loader { Answer = _ => Task.FromException<string>(new HttpRequestException("down")) };
        var handle = new NavigationEntity<string>("id-1", loader.Load);
        var changes = 0;
        handle.Changed += (_, _) => changes++;
        await Assert.ThrowsExactlyAsync<HttpRequestException>(() => handle.GetAsync(), "Expected the first attempt to fail.");

        loader.Answer = primitive => Task.FromResult("entity " + primitive);
        var entity = await handle.GetAsync();

        Assert.AreEqual("entity id-1", entity, "Expected the server that is back to give the entity.");
        Assert.AreEqual(2, loader.Calls, "Expected a new request after the failure.");
        Assert.AreEqual(1, changes, "Expected the new attempt to be announced.");
    }

    [TestMethod]
    public async Task GetAsync_must_start_a_new_attempt_after_a_cancellation()
    {
        var loader = new Loader { Answer = _ => Task.FromCanceled<string>(new CancellationToken(true)) };
        var handle = new NavigationEntity<string>("id-1", loader.Load);
        await Assert.ThrowsAsync<TaskCanceledException>(() => handle.GetAsync(), "Expected the first attempt to be canceled.");

        loader.Answer = primitive => Task.FromResult("entity " + primitive);

        Assert.AreEqual("entity id-1", await handle.GetAsync(), "Expected a canceled attempt to be replaced.");
    }

    [TestMethod]
    public async Task ReloadAsync_must_replace_a_loaded_entity_and_raise_Changed()
    {
        var loader = new Loader();
        var handle = new NavigationEntity<string>("id-1", loader.Load);
        await handle.GetAsync();
        var changes = 0;
        handle.Changed += (_, _) => changes++;
        loader.Answer = primitive => Task.FromResult("newer " + primitive);

        var reloaded = await handle.ReloadAsync();

        Assert.AreEqual("newer id-1", reloaded, "Expected the entity of the new attempt.");
        Assert.IsTrue(handle.TryGetValue(out var value) && value == "newer id-1", "Expected the handle to hold the reloaded entity.");
        Assert.AreEqual(1, changes, "Expected the reload to be announced.");
    }

    [TestMethod]
    public async Task A_handle_created_with_an_entity_must_be_loaded_and_reload_from_its_value()
    {
        var loader = new Loader { Answer = primitive => Task.FromResult("reloaded " + primitive) };
        var handle = new NavigationEntity<string>("id-1", "given", loader.Load);

        Assert.IsTrue(handle.IsLoaded, "Expected a handle that was given its entity to be loaded.");
        Assert.AreEqual(0, loader.Calls, "Expected no request for an entity that is given.");
        Assert.AreEqual("reloaded id-1", await handle.ReloadAsync(), "Expected the reload to ask for the entity by its value.");
    }

    [TestMethod]
    public async Task TryGetValue_must_fail_until_the_load_succeeds_and_after_a_failure()
    {
        var pending = new TaskCompletionSource<string>();
        var loader = new Loader { Answer = _ => pending.Task };
        var handle = new NavigationEntity<string>("id-1", loader.Load);
        _ = handle.LoadTask;

        Assert.IsFalse(handle.TryGetValue(out _), "Expected a pending load to have no entity.");

        pending.SetException(new HttpRequestException("down"));
        await Assert.ThrowsExactlyAsync<HttpRequestException>(() => handle.LoadTask, "Expected the load to fail.");

        Assert.IsFalse(handle.TryGetValue(out _), "Expected a failed load to have no entity.");
        Assert.IsFalse(handle.IsLoaded, "Expected a failed load not to count as loaded.");
    }

    [TestMethod]
    public async Task A_loader_that_throws_must_fail_the_attempt_instead_of_the_caller()
    {
        var handle = new NavigationEntity<string>("id-1", _ => throw new InvalidOperationException("no loader"));

        var task = handle.LoadTask;

        await Assert.ThrowsExactlyAsync<InvalidOperationException>(() => task, "Expected the failure to be the failure of the attempt.");
    }

    [TestMethod]
    public async Task The_handle_without_generics_must_give_the_same_results()
    {
        var loader = new Loader();
        INavigationEntity handle = new NavigationEntity<string>("id-1", loader.Load);

        await handle.LoadAsync();

        Assert.IsTrue(handle.IsLoaded, "Expected the entity to be loaded through the interface.");
        Assert.IsTrue(handle.TryGetLoadedEntity(out var entity) && (string)entity == "entity id-1", "Expected the loaded entity as an object.");
        Assert.AreEqual(typeof(string), handle.EntityType, "Expected the type of the entity.");
    }
}
