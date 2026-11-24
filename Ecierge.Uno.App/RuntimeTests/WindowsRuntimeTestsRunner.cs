#if WINDOWS
namespace Ecierge.Uno.App.RuntimeTests;

using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
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
/// Set <c>ECIERGE_RUNTIME_TESTS</c> to any value to run the tests after start-up; the results (NUnit test cases) go to the file named by <c>ECIERGE_RUNTIME_TESTS_OUTPUT_PATH</c> and the app exits.
/// </remarks>
internal static class WindowsRuntimeTestsRunner
{
    public static async Task RunIfRequestedAsync(Window window)
    {
        if (string.IsNullOrEmpty(Environment.GetEnvironmentVariable("ECIERGE_RUNTIME_TESTS"))) return;

        var outputPath = Environment.GetEnvironmentVariable("ECIERGE_RUNTIME_TESTS_OUTPUT_PATH");
        var host = new ContentControl { HorizontalContentAlignment = HorizontalAlignment.Stretch, VerticalContentAlignment = VerticalAlignment.Stretch };
        window.Content = host;
        await UIHelper.WaitForLoaded(host);
        UnitTestsUIContentHelper.EmbeddedTestRoot = (host, () => host.Content as UIElement, element => host.Content = element);

        var results = new XElement("test-run");
        var failed = 0;
        var testClasses = typeof(WindowsRuntimeTestsRunner).Assembly.GetTypes().Where(type => type.GetCustomAttribute<TestClassAttribute>() is not null);
        foreach (var testClass in testClasses)
        {
            foreach (var test in testClass.GetMethods().Where(method => method.GetCustomAttribute<TestMethodAttribute>() is not null))
            {
                var testCase = new XElement("test-case", new XAttribute("name", $"{testClass.FullName}.{test.Name}()"));
                try
                {
                    if (test.Invoke(Activator.CreateInstance(testClass), null) is Task running) await running;
                    testCase.Add(new XAttribute("result", "Passed"));
                }
                catch (Exception exception)
                {
                    failed++;
                    var error = exception is TargetInvocationException { InnerException: { } inner } ? inner : exception;
                    testCase.Add(new XAttribute("result", "Failed"), new XElement("failure", new XElement("message", error.ToString())));
                }

                host.Content = null;
                results.Add(testCase);
            }
        }

        if (!string.IsNullOrEmpty(outputPath)) await File.WriteAllTextAsync(outputPath, results.ToString());
        Environment.Exit(failed == 0 ? 0 : 1);
    }
}
#endif
