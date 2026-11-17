namespace Ecierge.Uno.App.Tests;

[TestClass]
public class AppInfoTests
{
    [TestInitialize]
    public void Setup()
    {
    }

    [TestMethod]
    public void AppInfoCreation()
    {
        var appInfo = new AppConfig { Environment = "Test" };

        appInfo.Should().NotBeNull();
        appInfo.Environment.Should().Be("Test");
    }
}
