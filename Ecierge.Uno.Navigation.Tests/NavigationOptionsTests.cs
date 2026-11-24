namespace Ecierge.Uno.Navigation.Tests;

using Ecierge.Uno.Navigation;
using Ecierge.Uno.Navigation.Navigators;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.UI.Xaml.Controls;

[TestClass, UnitTestCategory, NavigationTestCategory]
public class NavigationOptionsTests
{
    private static NavigationOptions OptionsOf(
        Func<NavigationOptions, NavigationOptions>? configure,
        IConfiguration? configuration = null,
        Func<NavigationOptions, bool>? validate = null)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        if (configuration is not null) services.Configure<NavigationOptions>(configuration.GetSection(nameof(NavigationOptions)));
        if (validate is not null) services.AddOptions<NavigationOptions>().Validate(validate, "The navigation options are not valid.");
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
        Assert.AreEqual("Close", options.DialogCloseButtonText, "Expected a dialog built around a plain view to get a close button.");
        Assert.AreEqual(TimeSpan.FromSeconds(30), options.StaleNavigationTimeout, "Expected a navigation to be given up after half a minute.");
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
    public void The_configure_callback_must_see_the_navigators_the_library_registers()
    {
        var seen = false;

        OptionsOf(o =>
        {
            seen = o.TryGetNavigatorType(typeof(ContentDialog), out _);
            return o;
        });

        Assert.IsTrue(seen, "Expected the callback to get the options after the navigators of the library were added.");
    }

    [TestMethod]
    public void The_configure_callback_must_be_able_to_replace_a_navigator_the_library_registers()
    {
        var options = OptionsOf(o =>
        {
            o.Navigators[typeof(ContentDialog)] = typeof(ContentControlNavigator);
            return o;
        });

        Assert.IsTrue(options.TryGetNavigatorType(typeof(ContentDialog), out var navigatorType), "Expected a navigator for a content dialog.");
        Assert.AreEqual(typeof(ContentControlNavigator), navigatorType, "Expected the navigator of the callback to win over the one of the library.");
    }

    [TestMethod]
    public void The_configure_callback_must_have_the_last_word_on_the_navigators_when_it_replaces_the_dictionary()
    {
        var options = OptionsOf(o => o with { Navigators = new Dictionary<Type, Type>() });

        Assert.IsEmpty(options.Navigators, "Expected the dictionary of the callback to be what the services get.");
    }

    [TestMethod]
    public void The_configure_callback_must_see_the_values_bound_from_the_configuration()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["NavigationOptions:UseNativeBackButton"] = "true" })
            .Build();
        bool? seen = null;

        OptionsOf(o =>
        {
            seen = o.UseNativeBackButton;
            return o;
        }, configuration);

        Assert.IsTrue(seen, "Expected the callback to get the options after the configuration was bound.");
    }

    [TestMethod]
    public void The_options_the_services_get_must_be_the_object_the_callback_returned()
    {
        NavigationOptions? returned = null;

        var options = OptionsOf(o => returned = o with { AddressBarUpdateEnabled = false });

        Assert.AreSame(returned, options, "Expected the options of the callback to be used as they are, not copied.");
    }

    // The options before the callback fail this validator, only the options the callback returns pass it
    [TestMethod]
    public void A_validator_must_accept_options_the_configure_callback_made_valid()
    {
        var options = OptionsOf(o => o with { UseNativeBackButton = true }, validate: o => o.UseNativeBackButton == true);

        Assert.IsTrue(options.UseNativeBackButton, "Expected the validator to check the options the callback returned.");
    }

    // The options before the callback pass this validator, only the options the callback returns fail it
    [TestMethod]
    public void A_validator_must_reject_options_the_configure_callback_made_invalid()
    {
        Assert.ThrowsExactly<OptionsValidationException>(
            () => OptionsOf(o => o with { UseNativeBackButton = true }, validate: o => o.UseNativeBackButton != true),
            "Expected the validator to check the options the callback returned.");
    }

    [TestMethod]
    public async Task A_journal_must_keep_as_many_entries_as_the_option_says()
    {
        using var host = Doubles.TestRoutes.CreateHost(new Doubles.BuildingServer(), configureOptions: o => o with { JournalCapacity = 2 });

        await host.Root.NavigateRouteAsync(this, "Login");
        await host.Root.NavigateRouteAsync(this, "Building/b1/Menu");
        await host.Root.NavigateRouteAsync(this, "Building/b2/Menu");

        Assert.AreSequenceEqual(
            new[] { "Building/b1/Menu", "Building/b2/Menu" },
            host.Root.Journal.Entries.Select(e => e.Path),
            "Expected the oldest entry to be dropped when the capacity of the options is full.");
    }
}
