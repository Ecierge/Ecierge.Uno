namespace Ecierge.Uno.Navigation.Regions;

using System;
using System.Diagnostics;

using Microsoft.Extensions.DependencyInjection;

/// <summary>
/// A place of the user interface that shows one segment of a route at a time and has the navigator that changes it.
/// </summary>
/// <remarks>
/// <para>
/// A region is created together with its <see cref="NavigationScope"/> and before its <see cref="Navigator"/>, so
/// the navigator is created with its region and never has to wait for it. The region of a dialog has no
/// <see cref="Target"/> until the dialog is shown.
/// </para>
/// </remarks>
[DebuggerDisplay("{Segment.Name}")]
public sealed class NavigationRegion
{
    private Navigator? navigator;

    /// <summary>Creates the region of a scope.</summary>
    /// <exception cref="ArgumentNullException"><paramref name="scope"/> is <see langword="null"/>.</exception>
    internal NavigationRegion(NavigationScope scope, NavigationRegion? parent, FrameworkElement? target, FrameworkElement? root)
    {
        Scope = scope ?? throw new ArgumentNullException(nameof(scope));
        Parent = parent;
        Target = target;
        Root = root;
    }

    /// <summary>
    /// Gets the scope of the services of the region.
    /// </summary>
    public NavigationScope Scope { get; }

    /// <summary>
    /// Gets the route segment that the region was created for.
    /// </summary>
    public NameSegment Segment => Scope.Segment;

    /// <summary>
    /// Gets the region that contains this one, or <see langword="null"/> for the root region of a window.
    /// </summary>
    public NavigationRegion? Parent { get; }

    /// <summary>
    /// Gets the control that the region shows its content in, or <see langword="null"/> while it has none, as
    /// the region of a dialog that is not shown yet.
    /// </summary>
    public FrameworkElement? Target { get; internal set; }

    /// <summary>
    /// Gets the control that bounds the search of the regions inside it, or <see langword="null"/> if there is no
    /// such boundary.
    /// </summary>
    public FrameworkElement? Root { get; }

    /// <summary>
    /// Gets the navigator of the region.
    /// </summary>
    public Navigator Navigator => navigator ??= Scope.ServiceProvider.GetRequiredService<Navigator>();

    /// <summary>
    /// Gets a value that tells whether this is the root region of a window.
    /// </summary>
    public bool IsRoot => Parent is null;

    /// <summary>
    /// Gets the root region of the window that the region belongs to.
    /// </summary>
    public NavigationRegion RootRegion
    {
        get
        {
            var region = this;
            while (region.Parent is { } parent)
            {
                region = parent;
            }

            return region;
        }
    }

    internal void AttachNavigator(Navigator navigator) => this.navigator = navigator;
}
