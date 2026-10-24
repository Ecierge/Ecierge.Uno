
using System.IO;

namespace Ecierge.Uno.App.UITests;

[TestClass]
public abstract class TestBase
{
    private IApp? _app;

    static TestBase()
    {
        AppInitializer.TestEnvironment.AndroidAppName = Constants.AndroidAppName;
        AppInitializer.TestEnvironment.WebAssemblyDefaultUri = Constants.WebAssemblyDefaultUri;
        AppInitializer.TestEnvironment.iOSAppName = Constants.iOSAppName;
        AppInitializer.TestEnvironment.AndroidAppName = Constants.AndroidAppName;
        AppInitializer.TestEnvironment.iOSDeviceNameOrId = Constants.iOSDeviceNameOrId;
        AppInitializer.TestEnvironment.CurrentPlatform = Constants.CurrentPlatform;
        AppInitializer.TestEnvironment.WebAssemblyBrowser = Constants.WebAssemblyBrowser;

#if DEBUG
        AppInitializer.TestEnvironment.WebAssemblyHeadless = false;
#endif

        // Start the app only once, so the tests runs don't restart it
        // and gain some time for the tests.
        AppInitializer.ColdStartApp();
    }

    /// <summary>
    /// The context of the running test. MSTest sets it before every test.
    /// </summary>
    public TestContext TestContext { get; set; } = null!;

    protected IApp App
    {
        get => _app!;
        private set
        {
            _app = value;
            global::Uno.UITest.Helpers.Queries.Helpers.App = value;
        }
    }

    [TestInitialize]
    public void SetUpTest()
    {
        App = AppInitializer.AttachToApp();
    }

    [TestCleanup]
    public void TearDownTest()
    {
        TakeScreenshot("teardown");
    }

    public FileInfo TakeScreenshot(string stepName)
    {
        var title = $"{TestContext.TestName}_{stepName}"
            .Replace(" ", "_")
            .Replace(".", "_");

        var fileInfo = App.Screenshot(title);

        var fileNameWithoutExt = Path.GetFileNameWithoutExtension(fileInfo.Name);
        if (fileNameWithoutExt != title && fileInfo.DirectoryName != null)
        {
            var destFileName = Path
                .Combine(fileInfo.DirectoryName, title + Path.GetExtension(fileInfo.Name));

            if (File.Exists(destFileName))
            {
                File.Delete(destFileName);
            }

            File.Move(fileInfo.FullName, destFileName);

            TestContext.AddResultFile(destFileName);

            fileInfo = new FileInfo(destFileName);
        }
        else
        {
            TestContext.AddResultFile(fileInfo.FullName);
        }

        return fileInfo;
    }

}
