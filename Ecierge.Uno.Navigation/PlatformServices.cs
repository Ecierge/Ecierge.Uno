namespace Ecierge.Uno.Navigation;

using Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Registers the services that only some platforms have. A platform adds its part in its own folder under
/// <c>Platforms</c>, and the platforms without a part register nothing.
/// </summary>
internal static partial class PlatformServices
{
    /// <summary>
    /// Adds the services of the platform that the library is built for.
    /// </summary>
    /// <param name="services">The services.</param>
    /// <returns>The services.</returns>
    public static IServiceCollection AddPlatformServices(this IServiceCollection services)
    {
        AddForPlatform(services);
        return services;
    }

    static partial void AddForPlatform(IServiceCollection services);
}
