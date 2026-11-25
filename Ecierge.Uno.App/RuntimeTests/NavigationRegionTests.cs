namespace Ecierge.Uno.App.RuntimeTests;

using System;
using System.Collections.Generic;
using System.Threading.Tasks;

using Ecierge.Uno.App.Presentation;
using Ecierge.Uno.Navigation;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Markup;
using Microsoft.UI.Xaml.Media;
using Microsoft.VisualStudio.TestTools.UnitTesting;

using global::Uno.UI.RuntimeTests;

using NavigationProperties = global::Ecierge.Uno.Navigation.Navigation;
using Region = global::Ecierge.Uno.Navigation.Regions.NavigationRegion;

/// <summary>
/// Navigation region lookups in a live tree: the marks that <c>AttachRegion</c> puts on the ancestors, the lookups after a region detaches and the walks across gaps in the logical tree.
/// </summary>
/// <remarks>
/// Every test builds root &gt; panel &gt; host under a root region with the routes of the sample app and attaches content of the host to the <c>Main</c> segment.
/// </remarks>
[TestClass]
[RunsOnUIThread]
public sealed class NavigationRegionTests
{
    // A route under the root segment of the sample app; its nested segments are Tab1, Tab2 and Dialog, so Main cannot nest under Main.
    private const string MainSegment = "Main";

    [TestMethod]
    public async Task Detaching_a_region_must_remove_its_marks_from_the_ancestors()
    {
        var tree = await CreateTreeAsync();
        var first = await ShowInHostAsync(tree, new ContentControl());
        first.AttachRegion(MainSegment);
        var firstRegion = RegionOf(first);
        Assert.IsNotNull(firstRegion);
        Assert.AreSame(firstRegion, RegionOf(tree.Host), "AttachRegion marks the ancestors up to the boundary.");
        Assert.AreSame(firstRegion, RegionOf(tree.Panel), "AttachRegion marks the ancestors up to the boundary.");

        first.DetachRegion();

        Assert.IsNull(RegionOf(tree.Host), "The host keeps the mark of the detached region.");
        Assert.IsNull(RegionOf(tree.Panel), "The panel keeps the mark of the detached region.");
        Assert.AreSame(tree.RootRegion, RegionOf(tree.Root));
    }

    [TestMethod]
    public async Task Content_attached_after_the_old_content_detached_must_nest_under_the_region_above_the_host()
    {
        var tree = await CreateTreeAsync();
        var first = await ShowInHostAsync(tree, new ContentControl());
        first.AttachRegion(MainSegment);
        var second = await ShowInHostAsync(tree, new ContentControl());
        first.DetachRegion();

        second.AttachRegion(MainSegment);

        Assert.AreSame(tree.RootRegion, RegionOf(second)?.Parent);
    }

    [TestMethod]
    public async Task Replaced_content_must_nest_under_the_region_above_the_host()
    {
        var tree = await CreateTreeAsync();
        var events = new List<string>();
        var errors = new List<Exception>();
        var first = AttachOnLoaded(new ContentControl(), "first", events, errors);
        await ShowInHostAsync(tree, first);
        await UIHelper.WaitForIdle();

        var second = AttachOnLoaded(new ContentControl(), "second", events, errors);
        await ShowInHostAsync(tree, second);
        await Task.Delay(200);
        await UIHelper.WaitForIdle();

        var order = string.Join(", ", events);
        Assert.AreEqual(0, errors.Count, $"Events: {order}. Errors: {string.Join(" | ", errors)}");
        Assert.IsNull(RegionOf(first), $"The old content has not detached. Events: {order}");
        Assert.AreSame(tree.RootRegion, RegionOf(second)?.Parent, $"Events: {order}");
    }

    [TestMethod]
    public async Task Element_that_found_a_detached_region_must_search_again()
    {
        var tree = await CreateTreeAsync();
        var sibling = new Button();
        tree.Panel.Children.Add(sibling);
        var first = await ShowInHostAsync(tree, new ContentControl());
        await UIHelper.WaitForLoaded(sibling);
        first.AttachRegion(MainSegment);
        Assert.AreSame(RegionOf(first), sibling.FindNavigationRegion(), "The sibling resolves to the region that marked the panel.");

        first.DetachRegion();

        Assert.AreSame(tree.RootRegion, sibling.FindNavigationRegion());
    }

    [TestMethod]
    public async Task Child_of_an_open_popup_must_find_the_region_around_the_popup()
    {
        var tree = await CreateTreeAsync();
        var popupChild = new Border { Width = 10, Height = 10 };
        var popup = new Popup { Child = popupChild };
        tree.Panel.Children.Add(popup);
        var first = await ShowInHostAsync(tree, new ContentControl());
        first.AttachRegion(MainSegment);
        popup.XamlRoot = tree.Root.XamlRoot;
        popup.IsOpen = true;
        try
        {
            await UIHelper.WaitForLoaded(popupChild);

            // The child of an open popup has the popup as its logical parent and the root of the popups as its visual parent.
            Assert.AreSame(RegionOf(first), popupChild.FindNavigationRegion());
        }
        finally
        {
            popup.IsOpen = false;
        }
    }

    [TestMethod]
    public async Task Template_part_must_attach_under_the_region_around_the_templated_control()
    {
        var tree = await CreateTreeAsync();
        var templated = new ContentControl
        {
            Template = (ControlTemplate)XamlReader.Load(
                """
                <ControlTemplate xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
                                 xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
                                 TargetType="ContentControl">
                  <Border>
                    <ContentControl x:Name="Part" />
                  </Border>
                </ControlTemplate>
                """)
        };
        await ShowInHostAsync(tree, templated);
        await UIHelper.WaitForIdle();
        var part = FindDescendant<ContentControl>(templated, "Part") ?? throw new InvalidOperationException("The template part is not in the tree.");

        part.AttachRegion(MainSegment);

        Assert.AreSame(tree.RootRegion, RegionOf(part)?.Parent, $"Parent of the template part: {part.Parent?.GetType().Name ?? "null"}");
    }

    private sealed record TestTree(ContentControl Root, Region RootRegion, StackPanel Panel, ContentControl Host);

    private static async Task<TestTree> CreateTreeAsync()
    {
        var host = new ContentControl();
        var panel = new StackPanel();
        panel.Children.Add(host);
        var root = new ContentControl { Content = panel };
        await UIHelper.Load(root);

        // The root region the way WindowExtensions.NavigateAsync attaches it, with the routes of the sample app.
        var app = (App)Application.Current;
        var services = app.Host?.Services ?? throw new InvalidOperationException("The host of the app is not built.");
        var rootSegment = new NameSegment(
            Qualifiers.Root,
            services.GetRequiredService<IViewRegistry>()[typeof(Shell)],
            isDefault: true,
            nested: services.GetRequiredService<IRouteRegistry>().RootSegment.Nested);
        var scope = new NavigationScope(services.CreateScope(), app.MainWindow ?? throw new InvalidOperationException("The app has no window."), rootSegment, root);
        root.AttachRootNavigationRegion(scope);
        return new TestTree(root, RegionOf(root) ?? throw new InvalidOperationException("The root region is not attached."), panel, host);
    }

    private static async Task<T> ShowInHostAsync<T>(TestTree tree, T content)
        where T : FrameworkElement
    {
        tree.Host.Content = content;
        await UIHelper.WaitForLoaded(content);
        return content;
    }

    /// <summary>
    /// The wiring of <c>NavigationRegion.Attach</c>: attach on <c>Loaded</c>, detach on <c>Unloaded</c>; the exceptions are collected instead of thrown from the event handlers.
    /// </summary>
    private static ContentControl AttachOnLoaded(ContentControl element, string name, List<string> events, List<Exception> errors)
    {
        element.Loaded += (_, _) =>
        {
            events.Add($"{name} loaded");
            try { element.AttachRegion(MainSegment); }
            catch (Exception exception) { errors.Add(exception); }
        };
        element.Unloaded += (_, _) =>
        {
            events.Add($"{name} unloaded");
            try { element.DetachRegion(); }
            catch (Exception exception) { errors.Add(exception); }
        };
        return element;
    }

    private static Region? RegionOf(FrameworkElement element) => element.GetValue(NavigationProperties.InfoProperty) as Region;

    private static T? FindDescendant<T>(DependencyObject parent, string name)
        where T : FrameworkElement
    {
        for (var i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
        {
            var child = VisualTreeHelper.GetChild(parent, i);
            if (child is T element && element.Name == name) return element;
            if (FindDescendant<T>(child, name) is T found) return found;
        }

        return null;
    }
}
