namespace Ecierge.Uno.Navigation.Tests;

using Ecierge.Uno.Navigation;
using Ecierge.Uno.Navigation.Tests.Doubles;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

/// <summary>
/// The navigations that the actions of a page start are fired and forgotten, so a failure has to be logged to be seen.
/// </summary>
[TestClass, UnitTestCategory, NavigationTestCategory]
public class NavigationActionLogTests
{
    private sealed record Entry(LogLevel Level, Exception? Exception, string Message);

    private sealed class CapturingLoggerProvider : ILoggerProvider
    {
        public List<Entry> Entries { get; } = new();

        public ILogger CreateLogger(string categoryName) => new CapturingLogger(Entries);

        public void Dispose()
        {
        }

        private sealed class CapturingLogger(List<Entry> entries) : ILogger
        {
            public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

            public bool IsEnabled(LogLevel logLevel) => true;

            public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
                => entries.Add(new Entry(logLevel, exception, formatter(state, exception)));
        }
    }

    private static NavigationTestHost CreateHost(CapturingLoggerProvider logs)
     => TestRoutes.CreateHost(new BuildingServer(), services => services.AddSingleton<ILoggerProvider>(logs));

    [TestMethod]
    public async Task A_navigation_that_an_action_started_and_that_fails_must_be_logged_and_still_fail()
    {
        var logs = new CapturingLoggerProvider();
        using var host = CreateHost(logs);

        var thrown = await Assert.ThrowsExactlyAsync<InvalidOperationException>(
            async () => await NavigationActionLog.Run("TestAction", host.Root, "Tab2", () => ValueTask.FromException<NavigationResponse>(new InvalidOperationException("The navigation failed."))),
            "Expected the failure to reach the caller.");

        var entry = Assert.ContainsSingle(logs.Entries.Where(e => e.Level == LogLevel.Error), "Expected one error in the log.");
        Assert.AreSame(thrown, entry.Exception, "Expected the log to carry the exception.");
        StringAssert.Contains(entry.Message, "TestAction", "Expected the log to name the action.");
        StringAssert.Contains(entry.Message, "Tab2", "Expected the log to name what the action navigates to.");
    }

    [TestMethod]
    public void An_action_that_cannot_start_its_navigation_must_be_logged_and_still_throw()
    {
        var logs = new CapturingLoggerProvider();
        using var host = CreateHost(logs);

        var thrown = Assert.ThrowsExactly<InvalidOperationException>(
            () => NavigationActionLog.Run("TestAction", host.Root, "Tab2", () => throw new InvalidOperationException("The region has no child region attached.")),
            "Expected the exception to reach the caller.");

        var entry = Assert.ContainsSingle(logs.Entries.Where(e => e.Level == LogLevel.Error), "Expected one error in the log.");
        Assert.AreSame(thrown, entry.Exception, "Expected the log to carry the exception.");
    }

    [TestMethod]
    public async Task A_navigation_that_an_action_started_and_that_succeeds_must_not_be_logged()
    {
        var logs = new CapturingLoggerProvider();
        using var host = CreateHost(logs);

        var response = await NavigationActionLog.Run("TestAction", host.Root, "Login", () => host.Root.NavigateRouteAsync(this, "Login"));

        Assert.IsTrue(response.Success, "Expected the response of the navigation to come back.");
        Assert.IsEmpty(logs.Entries.Where(e => e.Level >= LogLevel.Warning), "Expected nothing to be reported about a navigation that worked.");
    }
}
