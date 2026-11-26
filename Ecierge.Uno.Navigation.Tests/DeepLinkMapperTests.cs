namespace Ecierge.Uno.Navigation.Tests;

using Ecierge.Uno.Navigation;

[TestClass, UnitTestCategory, NavigationTestCategory]
public class DeepLinkMapperTests
{
    private static readonly NavigationOptions Options = new() { DeepLinkScheme = "ecierge-console" };

    private static string? Map(string link, NavigationOptions? options = null)
     => DeepLinkMapper.ToRoutePath(new Uri(link, UriKind.RelativeOrAbsolute), options ?? Options);

    [TestMethod]
    public void A_link_of_the_custom_scheme_must_give_the_route_with_the_case_of_its_names()
    {
        Assert.AreEqual("Building/3f2a/Assets", Map("ecierge-console://Building/3f2a/Assets"), "Expected the first segment, which is read as a host, to keep its case.");
    }

    [TestMethod]
    public void The_scheme_of_a_link_must_be_compared_without_regard_to_case()
    {
        Assert.AreEqual("Building/3f2a", Map("ECIERGE-CONSOLE://Building/3f2a"), "Expected the scheme to match in any case.");
    }

    [TestMethod]
    public void A_query_and_a_fragment_must_not_be_part_of_the_route()
    {
        Assert.AreEqual("Building/3f2a/Assets", Map("ecierge-console://Building/3f2a/Assets?tab=2#top"), "Expected only the path to be the route.");
        Assert.AreEqual("Building/3f2a/Assets", Map("https://console.example.com/Building/3f2a/Assets?tab=2#top"), "Expected only the path of the address to be the route.");
    }

    [TestMethod]
    public void A_dialog_and_an_escaped_value_must_be_decoded()
    {
        Assert.AreEqual("Building/3f2a/Menu/!AddAsset", Map("ecierge-console://Building/3f2a/Menu/%21AddAsset"), "Expected the escaped dialog qualifier to be read.");
        Assert.AreEqual("Building/a b/Menu", Map("ecierge-console://Building/a%20b/Menu"), "Expected the escaped value to be read.");
    }

    [TestMethod]
    public void A_trailing_slash_must_not_change_the_route()
    {
        Assert.AreEqual("Building/3f2a", Map("ecierge-console://Building/3f2a/"), "Expected the trailing separator to be ignored.");
    }

    [TestMethod]
    public void A_link_of_another_scheme_must_not_be_a_route()
    {
        Assert.IsNull(Map("com.example.auth://callback/code"), "Expected the callback of a sign-in not to be taken for a page.");
        Assert.IsNull(Map("mailto:someone@example.com"), "Expected a link to a mailbox not to be a page.");
    }

    [TestMethod]
    public void A_custom_scheme_must_not_be_followed_unless_the_options_name_it()
    {
        Assert.IsNull(Map("ecierge-console://Building/3f2a", new NavigationOptions()), "Expected no custom scheme to be followed without a name.");
    }

    [TestMethod]
    public void A_link_of_the_web_must_give_its_path_as_the_route()
    {
        Assert.AreEqual("Building/3f2a/Details/Floors", Map("https://console.example.com/Building/3f2a/Details/Floors"), "Expected the path of the address to be the route.");
        Assert.AreEqual("Login", Map("http://localhost:5000/Login"), "Expected an address without the secure scheme to be a route too.");
    }

    [TestMethod]
    public void A_link_of_the_web_must_be_inside_the_base_path()
    {
        var options = new NavigationOptions { BasePath = "/console/" };

        Assert.AreEqual("Building/3f2a", Map("https://example.com/console/Building/3f2a", options), "Expected the base path to be removed.");
        Assert.IsNull(Map("https://example.com/other/Building/3f2a", options), "Expected a page of another application not to be taken.");
        Assert.IsNull(Map("https://example.com/consolex/Building/3f2a", options), "Expected a name that only starts like the base path not to match.");
        Assert.IsNull(Map("https://example.com/console", options), "Expected the base path itself to name no page.");
    }

    [TestMethod]
    public void A_link_without_a_path_must_name_no_page()
    {
        Assert.IsNull(Map("https://example.com/"), "Expected the root of the site to name no page.");
        Assert.IsNull(Map("https://example.com/index.html"), "Expected the page that the host serves for every address to name no page.");
        Assert.IsNull(Map("Building/3f2a"), "Expected a relative link to name no page.");
    }

    [TestMethod]
    [DataRow("ecierge-console://-")]
    [DataRow("ecierge-console://-/Login")]
    [DataRow("ecierge-console://../Login")]
    [DataRow("ecierge-console://./Info")]
    public void A_link_must_not_carry_the_commands_of_the_text_of_a_route(string link)
    {
        Assert.IsNull(Map(link), "Expected a link not to go back, clear the history or start in another region.");
    }

    [TestMethod]
    public void A_missing_argument_must_be_refused()
    {
        Assert.ThrowsExactly<ArgumentNullException>(() => DeepLinkMapper.ToRoutePath(null!, Options), "Expected a missing link to be refused.");
        Assert.ThrowsExactly<ArgumentNullException>(() => DeepLinkMapper.ToRoutePath(new Uri("https://example.com/Login"), null!), "Expected missing options to be refused.");
    }
}
