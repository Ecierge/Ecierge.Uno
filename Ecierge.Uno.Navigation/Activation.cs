namespace Ecierge.Uno.Navigation;

using System;
using System.Collections.ObjectModel;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;

using Windows.ApplicationModel.Activation;
using Windows.Foundation.Metadata;

internal static class Activation
{
    /// <summary>
    /// Determines whether the specified <paramref name="exception"/> is considered fatal.
    /// </summary>
    /// <param name="exception">The exception.</param>
    /// <returns>
    /// <see langword="true"/> if the exception is considered fatal, <see langword="false"/> otherwise.
    /// </returns>
    public static bool IsFatal(Exception exception)
    {
        RuntimeHelpers.EnsureSufficientExecutionStack();

        return exception switch
        {
            ThreadAbortException => true,
            OutOfMemoryException and not InsufficientMemoryException => true,

            AggregateException { InnerExceptions: var exceptions } => IsAnyFatal(exceptions),
            Exception { InnerException: Exception inner } => IsFatal(inner),

            _ => false
        };

        static bool IsAnyFatal(ReadOnlyCollection<Exception> exceptions)
        {
            for (var index = 0; index < exceptions.Count; index++)
            {
                if (IsFatal(exceptions[index]))
                {
                    return true;
                }
            }

            return false;
        }
    }

    /// <summary>
    /// Determines whether the current Windows version
    /// is greater than or equals to the specified version.
    /// </summary>
    /// <returns>
    /// <see langword="true"/> if the current Windows version is greater than
    /// or equals to the specified version, <see langword="false"/> otherwise.
    /// </returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [SupportedOSPlatformGuard("windows")]
    internal static bool IsWindowsVersionAtLeast(int major, int minor = 0, int build = 0, int revision = 0)
    {
#if SUPPORTS_OPERATING_SYSTEM_VERSIONS_COMPARISON
        return OperatingSystem.IsWindowsVersionAtLeast(major, minor, build, revision);
#else
        if (Environment.OSVersion.Platform is PlatformID.Win32NT &&
            Environment.OSVersion.Version >= new Version(major, minor, build, revision))
        {
            return true;
        }

        // Note: on older versions of .NET, Environment.OSVersion.Version is known to be affected by
        // the compatibility shims used by Windows 10+ when the application doesn't have a manifest
        // that explicitly indicates it's compatible with Windows 10 and higher. To avoid that, a
        // second pass using RuntimeInformation.OSDescription (that calls NtDll.RtlGetVersion() under
        // the hood) is made. Note: no version is returned on UWP due to the missing Win32 API.
        return RuntimeInformation.OSDescription.StartsWith("Microsoft Windows ", StringComparison.OrdinalIgnoreCase) &&
               RuntimeInformation.OSDescription["Microsoft Windows ".Length..] is string value &&
               Version.TryParse(value, out Version? version) && version >= new Version(major, minor, build, revision);
#endif
    }

    /// <summary>
    /// Determines whether the Windows Runtime APIs are supported on this platform.
    /// </summary>
    /// <returns><see langword="true"/> if the Windows Runtime APIs are supported, <see langword="false"/> otherwise.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [SupportedOSPlatformGuard("windows10.0.17763")]
    // Note: as WinRT is only supported on Windows 8 and higher, trying to call any of the
    // WinRT APIs on previous versions of Windows will typically result in type-load or
    // type-initialization exceptions. To prevent that, this method acts as a platform
    // guard that will prevent the WinRT projections from being loaded by the runtime on
    // platforms that don't support it. Since OpenIddict declares Windows 10 1809 as the
    // oldest supported version in the package, it is also used for the runtime check.
    internal static bool IsWindowsRuntimeSupported() => IsWindowsVersionAtLeast(10, 0, 17763);

    /// <summary>
    /// Determines whether WinRT app instance activation is supported on this platform.
    /// </summary>
    /// <returns>
    /// <see langword="true"/> if WinRT app instance activation is supported, <see langword="false"/> otherwise.
    /// </returns>
    [SupportedOSPlatformGuard("windows10.0.17763")]
    internal static bool IsAppInstanceActivationSupported()
    {
#if WINDOWS10_0_17763_0_OR_GREATER
        return IsWindowsRuntimeSupported() && IsApiPresent();

        [MethodImpl(MethodImplOptions.NoInlining)]
        static bool IsApiPresent() => ApiInformation.IsMethodPresent(
            typeName           : typeof(AppInstance).FullName!,
            methodName         : nameof(AppInstance.GetActivatedEventArgs),
            inputParameterCount: 0);
#else
        return false;
#endif
    }

    /// <summary>
    /// Resolves the protocol activation using the Windows Runtime APIs, if applicable.
    /// </summary>
    /// <returns>
    /// The <see cref="Uri"/> if the application instance was activated
    /// via a protocol activation, <see langword="null"/> otherwise.
    /// </returns>
    [MethodImpl(MethodImplOptions.NoInlining), SupportedOSPlatform("windows10.0.17763")]
    internal static Uri? GetProtocolActivationUriWithWindowsRuntime()
    {
        try
        {
            return AppInstance.GetActivatedEventArgs() is
                ProtocolActivatedEventArgs args ? args.Uri : null;
        }

        catch (Exception exception) when (!IsFatal(exception))
        {
            return null;
        }
    }

    /// <summary>
    /// Resolves the protocol activation from the command line arguments, if applicable.
    /// </summary>
    /// <returns>
    /// The <see cref="Uri"/> if the application instance was activated
    /// via a protocol activation, <see langword="null"/> otherwise.
    /// </returns>
    internal static Uri? GetProtocolActivationUriFromCommandLineArguments(string?[]? arguments) => arguments switch
    {
    // In most cases, the first segment present in the command line arguments contains the path of the
    // executable, but it's technically possible to start an application in a way that the command line
    // arguments will never include the executable path. To support both cases, the URI is extracted
    // from the second segment when 2 segments are present. Otherwise, the first segment is used.
    //
    // For more information, see https://devblogs.microsoft.com/oldnewthing/20060515-07/?p=31203.

    [_, string argument] when Uri.TryCreate(argument, UriKind.Absolute, out Uri? uri) && !uri.IsFile => uri,
    [string argument] when Uri.TryCreate(argument, UriKind.Absolute, out Uri? uri) && !uri.IsFile => uri,

        _ => null
    };

}
