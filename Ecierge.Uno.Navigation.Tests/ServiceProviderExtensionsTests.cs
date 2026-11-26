namespace Ecierge.Uno.Navigation.Tests;

using Microsoft.Extensions.DependencyInjection;

/// <summary>
/// The instances that a service provider keeps in its scoped and its singleton repository, and the factories that create
/// them on first use.
/// </summary>
[TestClass, UnitTestCategory, NavigationTestCategory]
public class ServiceProviderExtensionsTests
{
    private sealed class Widget;

    // The two repositories are registered as in the navigation services: one per scope and one for the application
    private static ServiceProvider CreateProvider()
    {
        var services = new ServiceCollection();
        services.AddSingleton<ISingletonInstanceRepository, InstanceRepository>();
        services.AddScoped<IScopedInstanceRepository, InstanceRepository>();
        return services.BuildServiceProvider();
    }

    [TestMethod]
    public void A_scoped_factory_must_create_its_instance_on_first_use_and_keep_it()
    {
        using var root = CreateProvider();
        using var scope = root.CreateScope();
        var created = 0;
        Func<object> create = () =>
        {
            created++;
            return new Widget();
        };

        scope.ServiceProvider.SetScopedInstance(typeof(Widget), create);

        var first = scope.ServiceProvider.GetInstance(typeof(Widget));
        var second = scope.ServiceProvider.GetInstance(typeof(Widget));
        Assert.IsInstanceOfType<Widget>(first, "Expected the factory to create the instance when it is asked for.");
        Assert.AreSame(first, second, "Expected the instance that the factory created to be kept.");
        Assert.AreEqual(1, created, "Expected the factory to run once.");
    }

    [TestMethod]
    public void A_singleton_factory_must_create_its_instance_on_first_use_and_keep_it()
    {
        using var root = CreateProvider();
        using var scope = root.CreateScope();
        var created = 0;
        Func<object> create = () =>
        {
            created++;
            return new Widget();
        };

        scope.ServiceProvider.SetSingletonInstance(typeof(Widget), create);

        var first = scope.ServiceProvider.GetInstance(typeof(Widget));
        var second = scope.ServiceProvider.GetInstance(typeof(Widget));
        Assert.IsInstanceOfType<Widget>(first, "Expected the factory to create the instance when it is asked for.");
        Assert.AreSame(first, second, "Expected the instance that the factory created to be kept.");
        Assert.AreEqual(1, created, "Expected the factory to run once.");
    }

    [TestMethod]
    public void A_scoped_factory_set_with_a_type_must_be_found_by_the_generic_lookup()
    {
        using var root = CreateProvider();
        using var scope = root.CreateScope();
        var created = 0;
        Func<object> create = () =>
        {
            created++;
            return new Widget();
        };

        scope.ServiceProvider.SetScopedInstance(typeof(Widget), create);

        var first = scope.ServiceProvider.GetInstance<Widget>();
        var second = scope.ServiceProvider.GetInstance<Widget>();
        Assert.IsNotNull(first, "Expected the generic lookup to create the instance of a factory that was set with a type.");
        Assert.AreSame(first, second, "Expected the instance that the factory created to be kept.");
        Assert.AreEqual(1, created, "Expected the factory to run once.");
    }

    [TestMethod]
    public void A_singleton_factory_set_with_a_type_must_be_found_by_the_generic_lookup()
    {
        using var root = CreateProvider();
        using var scope = root.CreateScope();
        var created = 0;
        Func<object> create = () =>
        {
            created++;
            return new Widget();
        };

        scope.ServiceProvider.SetSingletonInstance(typeof(Widget), create);

        var first = scope.ServiceProvider.GetInstance<Widget>();
        var second = scope.ServiceProvider.GetInstance<Widget>();
        Assert.IsNotNull(first, "Expected the generic lookup to create the instance of a factory that was set with a type.");
        Assert.AreSame(first, second, "Expected the instance that the factory created to be kept.");
        Assert.AreEqual(1, created, "Expected the factory to run once.");
    }

    [TestMethod]
    public void Removing_the_singleton_instance_must_keep_the_scoped_instance_of_the_same_type()
    {
        using var root = CreateProvider();
        using var scope = root.CreateScope();
        var scoped = new Widget();
        scope.ServiceProvider.SetScopedInstance(scoped);
        scope.ServiceProvider.SetSingletonInstance(new Widget());

        scope.ServiceProvider.RemoveSingletonInstance<Widget>();

        Assert.AreSame(scoped, scope.ServiceProvider.GetInstance<Widget>(), "Expected the scoped instance to stay when the singleton one is removed.");
    }

    [TestMethod]
    public void Removing_the_scoped_instance_must_keep_the_singleton_instance_of_the_same_type()
    {
        using var root = CreateProvider();
        using var scope = root.CreateScope();
        var singleton = new Widget();
        scope.ServiceProvider.SetScopedInstance(new Widget());
        scope.ServiceProvider.SetSingletonInstance(singleton);

        scope.ServiceProvider.RemoveScopedInstance<Widget>();

        Assert.AreSame(singleton, scope.ServiceProvider.GetInstance<Widget>(), "Expected the singleton instance to stay when the scoped one is removed.");
    }
}
