#if WINDOWS
namespace Ecierge.Uno.App.RuntimeTests;

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.VisualStudio.TestTools.UnitTesting;

using global::Uno.UI.RuntimeTests;

/// <summary>
/// Runs the runtime tests on the Windows head.
/// </summary>
/// <remarks>
/// The embedded runner of Uno.UI.RuntimeTests.Engine needs <c>Window.Current</c>, and its <c>UnitTestsControl</c> reads <c>ApplicationData</c>, which an unpackaged app cannot.
/// This runner hosts the test content itself and runs every <c>[TestMethod]</c> of the <c>[TestClass]</c> types once.
/// Set <c>ECIERGE_RUNTIME_TESTS</c> to any value to run the tests after start-up; the results go to the file named by <c>ECIERGE_RUNTIME_TESTS_OUTPUT_PATH</c> and the app exits.
/// The file is an NUnit 3 test result in the layout of the Uno runner: <c>test-run</c> holds the <c>test-suite</c> of the assembly, which holds one <c>test-suite</c> per test class, which holds its <c>test-case</c> elements.
/// </remarks>
internal static class WindowsRuntimeTestsRunner
{
    private static int lastId;

    public static async Task RunIfRequestedAsync(Window window)
    {
        if (string.IsNullOrEmpty(Environment.GetEnvironmentVariable("ECIERGE_RUNTIME_TESTS"))) return;

        var outputPath = Environment.GetEnvironmentVariable("ECIERGE_RUNTIME_TESTS_OUTPUT_PATH");
        var host = new ContentControl { HorizontalContentAlignment = HorizontalAlignment.Stretch, VerticalContentAlignment = VerticalAlignment.Stretch };
        window.Content = host;
        await UIHelper.WaitForLoaded(host);
        UnitTestsUIContentHelper.EmbeddedTestRoot = (host, () => host.Content as UIElement, element => host.Content = element);

        var outcomes = new List<Outcome>();
        var fixtures = new List<XElement>();
        var testClasses = typeof(WindowsRuntimeTestsRunner).Assembly.GetTypes().Where(type => type.GetCustomAttribute<TestClassAttribute>() is not null);
        foreach (var testClass in testClasses)
        {
            var classOutcomes = new List<Outcome>();
            foreach (var test in testClass.GetMethods().Where(method => method.GetCustomAttribute<TestMethodAttribute>() is not null))
            {
                classOutcomes.Add(await RunAsync(testClass, test));
                host.Content = null;
            }

            outcomes.AddRange(classOutcomes);
            fixtures.Add(CreateSuite("TestFixture", testClass.Name, testClass.FullName ?? testClass.Name, classOutcomes, classOutcomes.Select(outcome => outcome.Element)));
        }

        if (!string.IsNullOrEmpty(outputPath)) await WriteAsync(CreateResults(outcomes, fixtures), outputPath);
        Environment.Exit(outcomes.All(outcome => outcome.Passed) ? 0 : 1);
    }

    /// <summary>
    /// The result of one test and the <c>test-case</c> element that reports it.
    /// </summary>
    private sealed record Outcome(XElement Element, bool Passed, DateTimeOffset Started, DateTimeOffset Finished);

    private static async Task<Outcome> RunAsync(Type testClass, MethodInfo test)
    {
        var started = DateTimeOffset.UtcNow;
        var stopwatch = Stopwatch.StartNew();
        Exception? failure = null;
        try
        {
            if (test.Invoke(Activator.CreateInstance(testClass), null) is Task running) await running;
        }
        catch (Exception exception)
        {
            failure = exception is TargetInvocationException { InnerException: { } inner } ? inner : exception;
        }

        var className = testClass.FullName ?? testClass.Name;
        var testCase = new XElement("test-case",
            new XAttribute("id", NextId()),
            new XAttribute("name", test.Name),
            new XAttribute("fullname", $"{className}.{test.Name}"),
            new XAttribute("methodname", test.Name),
            new XAttribute("classname", className),
            new XAttribute("runstate", "Runnable"),
            new XAttribute("result", failure is null ? "Passed" : "Failed"),
            new XAttribute("start-time", FormatTime(started)),
            new XAttribute("end-time", FormatTime(started + stopwatch.Elapsed)),
            new XAttribute("duration", FormatSeconds(stopwatch.Elapsed.TotalSeconds)),
            new XAttribute("asserts", 0));
        if (failure is not null)
        {
            // An assertion carries its own message; any other exception is named, as NUnit reports an error of the test
            var message = failure is AssertFailedException ? failure.Message : $"{failure.GetType().FullName}: {failure.Message}";
            testCase.Add(new XElement("failure", new XElement("message", new XCData(message)), new XElement("stack-trace", new XCData(failure.StackTrace ?? string.Empty))));
        }

        return new Outcome(testCase, failure is null, started, started + stopwatch.Elapsed);
    }

    private static XDocument CreateResults(IReadOnlyList<Outcome> outcomes, IEnumerable<XElement> fixtures)
    {
        var assemblyName = typeof(WindowsRuntimeTestsRunner).Assembly.GetName().Name ?? "Ecierge.Uno.App";
        var environment = new XElement("environment",
            new XAttribute("os-version", Environment.OSVersion.VersionString),
            new XAttribute("clr-version", Environment.Version.ToString()),
            new XAttribute("platform", "Windows"),
            new XAttribute("machine-name", Environment.MachineName));
        var assemblySuite = CreateSuite("Assembly", assemblyName, assemblyName, outcomes, [environment, .. fixtures]);
        var run = new XElement("test-run", new XAttribute("id", "0"), new XAttribute("name", assemblyName), new XAttribute("fullname", assemblyName), Summarize(outcomes), assemblySuite);
        return new XDocument(new XDeclaration("1.0", "utf-8", null), run);
    }

    private static XElement CreateSuite(string type, string name, string fullName, IReadOnlyList<Outcome> outcomes, IEnumerable<XElement> children) =>
        new("test-suite", new XAttribute("type", type), new XAttribute("id", NextId()), new XAttribute("name", name), new XAttribute("fullname", fullName), Summarize(outcomes), children);

    /// <summary>
    /// The counts and the times that <c>test-run</c> and every <c>test-suite</c> state about the test cases below it.
    /// </summary>
    private static IEnumerable<XAttribute> Summarize(IReadOnlyList<Outcome> outcomes)
    {
        var passed = outcomes.Count(outcome => outcome.Passed);
        var failed = outcomes.Count - passed;
        var started = outcomes.Select(outcome => outcome.Started).DefaultIfEmpty(DateTimeOffset.UtcNow).Min();
        var finished = outcomes.Select(outcome => outcome.Finished).DefaultIfEmpty(started).Max();
        return
        [
            new("testcasecount", outcomes.Count),
            new("result", failed == 0 ? "Passed" : "Failed"),
            new("start-time", FormatTime(started)),
            new("end-time", FormatTime(finished)),
            new("duration", FormatSeconds(outcomes.Sum(outcome => (outcome.Finished - outcome.Started).TotalSeconds))),
            new("total", outcomes.Count),
            new("passed", passed),
            new("failed", failed),
            new("inconclusive", 0),
            new("skipped", 0),
            new("asserts", 0),
        ];
    }

    private static async Task WriteAsync(XDocument results, string path)
    {
        await using var stream = File.Create(path);
        await using var writer = XmlWriter.Create(stream, new XmlWriterSettings { Async = true, Indent = true, Encoding = new UTF8Encoding(false) });
        await results.SaveAsync(writer, CancellationToken.None);
    }

    private static string NextId() => $"0-{++lastId}";

    private static string FormatTime(DateTimeOffset time) => time.UtcDateTime.ToString("u", CultureInfo.InvariantCulture);

    private static string FormatSeconds(double seconds) => seconds.ToString("0.######", CultureInfo.InvariantCulture);
}
#endif
