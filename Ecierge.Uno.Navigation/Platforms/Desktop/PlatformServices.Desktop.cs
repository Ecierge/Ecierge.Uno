namespace Ecierge.Uno.Navigation;

using System;

using Microsoft.Extensions.DependencyInjection;

internal static partial class PlatformServices
{
    static partial void AddForPlatform(IServiceCollection services)
     => services.AddSingleton<IDeepLinkSource, DesktopDeepLinkSource>();
}

/// <summary>
/// Reads the link that started the application from the command line, where the system puts the link of a registered
/// scheme.
/// </summary>
internal sealed class DesktopDeepLinkSource : IDeepLinkSource
{
    /// <inheritdoc />
    public Uri? LaunchUri { get; } = Activation.GetProtocolActivationUriFromCommandLineArguments(Environment.GetCommandLineArgs());

    /// <summary>
    /// A link that reaches an instance that runs is not handed to it on this platform: the system starts another
    /// process with the link.
    /// </summary>
    public event EventHandler<Uri>? Received
    {
        add { }
        remove { }
    }
}
