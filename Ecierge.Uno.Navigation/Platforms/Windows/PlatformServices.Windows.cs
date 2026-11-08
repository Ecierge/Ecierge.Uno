namespace Ecierge.Uno.Navigation;

using System;

using Microsoft.Extensions.DependencyInjection;

internal static partial class PlatformServices
{
    static partial void AddForPlatform(IServiceCollection services)
     => services.AddSingleton<IDeepLinkSource, WindowsDeepLinkSource>();
}

/// <summary>
/// Reads the link that started the application from its protocol activation.
/// </summary>
internal sealed class WindowsDeepLinkSource : IDeepLinkSource
{
    /// <inheritdoc />
    public Uri? LaunchUri { get; } = Activation.IsAppInstanceActivationSupported()
        ? Activation.GetProtocolActivationUriWithWindowsRuntime()
        : null;

    /// <summary>
    /// A link that reaches an instance that runs is not handed to it yet: it needs the single instance redirection
    /// of the application, which the application sets up.
    /// </summary>
    public event EventHandler<Uri>? Received
    {
        add { }
        remove { }
    }
}
