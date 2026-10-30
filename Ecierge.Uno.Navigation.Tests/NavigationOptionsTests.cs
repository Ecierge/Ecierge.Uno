namespace Ecierge.Uno.Navigation.Tests;

using Ecierge.Uno.Navigation;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.UI.Xaml.Controls;

[TestClass, UnitTestCategory, NavigationTestCategory]
public class NavigationOptionsTests
{
    private static NavigationOptions OptionsOf(Func<NavigationOptions, NavigationOptions>? configure)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddNavigation(configure);
        using var provider = services.BuildServiceProvider();
        return provider.GetRequiredService<IOptions<NavigationOptions>>().Value;
    }

    [TestMethod]
    public void The_defaults_must_keep_dialogs_in_the_history_and_a_journal_of_a_hundred_entries()
    {
        var options = OptionsOf(null);

        Assert.IsTrue(options.DialogsInHistory, "Expected a dialog to be an entry of the history by default.");
        Assert.AreEqual(100, options.JournalCapacity, "Expected the default capacity.");
        Assert.AreEqual("/", options.BasePath, "Expected the application to be hosted at the root.");
        Assert.IsNull(options.DeepLinkScheme, "Expected no custom scheme to be followed unless one is set.");
    }

    [TestMethod]
    public void The_configure_callback_of_AddNavigation_must_change_the_options()
    {
        var options = OptionsOf(o => o with { DialogsInHistory = false, JournalCapacity = 5, DeepLinkScheme = "ecierge-console", BasePath = "/console/" });

        Assert.IsFalse(options.DialogsInHistory, "Expected the callback to turn dialogs in the history off.");
        Assert.AreEqual(5, options.JournalCapacity, "Expected the callback to set the capacity.");
        Assert.AreEqual("ecierge-console", options.DeepLinkScheme, "Expected the callback to set the scheme.");
        Assert.AreEqual("/console/", options.BasePath, "Expected the callback to set the base path.");
    }

    [TestMethod]
    public void The_configure_callback_must_not_remove_the_navigators_the_library_registers()
    {
        var options = OptionsOf(o => o with { DialogsInHistory = false });

        Assert.IsTrue(options.TryGetNavigatorType(typeof(ContentDialog), out var navigatorType), "Expected the navigator of a content dialog to stay registered.");
        Assert.IsNotNull(navigatorType, "Expected the type of the navigator.");
    }

    [TestMethod]
    public void The_configure_callback_must_not_remove_the_navigators_even_when_it_replaces_the_dictionary()
    {
        var options = OptionsOf(o => o with { Navigators = new Dictionary<Type, Type>() });

        Assert.IsTrue(options.TryGetNavigatorType(typeof(ContentDialog), out _), "Expected the navigators to be added after the callback ran.");
    }

    [TestMethod]
    public async Task A_journal_must_keep_as_many_entries_as_the_option_says()
    {
        using var host = Doubles.TestRoutes.CreateHost(new Doubles.BuildingServer(), configureOptions: o => o with { JournalCapacity = 2 });

        await host.Root.NavigateRouteAsync(this, "Login");
        await host.Root.NavigateRouteAsync(this, "Building/b1/Menu");
        await host.Root.NavigateRouteAsync(this, "Building/b2/Menu");

        CollectionAssert.AreEqual(
            new[] { "Building/b1/Menu", "Building/b2/Menu" },
            host.Root.Journal.Entries.Select(e => e.Path).ToArray(),
            "Expected the oldest entry to be dropped when the capacity of the options is full.");
    }
}
