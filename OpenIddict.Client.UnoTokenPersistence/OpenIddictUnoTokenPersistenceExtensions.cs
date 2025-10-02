/*
 * Licensed under the Apache License, Version 2.0 (http://www.apache.org/licenses/LICENSE-2.0)
 * See https://github.com/openiddict/openiddict-core for more information concerning
 * the license and the contributors participating to this project.
 */

using Microsoft.Extensions.DependencyInjection.Extensions;

using OpenIddict.Client.UnoTokenPersistence;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Exposes extensions allowing to register the OpenIddict client services.
/// </summary>
public static class OpenIddictUnoTokenPersistenceExtensions
{
    /// <summary>
    /// Registers the OpenIddict client system integration services in the DI container.
    /// </summary>
    /// <param name="builder">The services builder used by OpenIddict to register new services.</param>
    /// <param name="configuration">The configuration delegate used to configure the client services.</param>
    /// <remarks>This extension can be safely called multiple times.</remarks>
    /// <returns>The <see cref="OpenIddict.ClientBuilder"/>.</returns>
    public static OpenIddictUnoTokenPersistenceBuilder UseUnoTokenPersistence(
        this OpenIddictCoreBuilder builder, Action<OpenIddictUnoTokenPersistenceOptions>? configuration = default)
    {
        if (builder is null)
        {
            throw new ArgumentNullException(nameof(builder));
        }

        var services = builder.Services;
        if (configuration is not null)
            services.Configure(configuration);

        // Note: Uno uses simple comparison checks by default so the additional
        // query filtering applied by the default OpenIddict managers can be safely disabled.
        builder.DisableAdditionalFiltering();

        builder.SetDefaultTokenEntity<OpenIddictUnoToken>();

        builder.ReplaceTokenStore<OpenIddictUnoToken, OpenIddictUnoTokenStore<OpenIddictUnoToken> > (ServiceLifetime.Singleton);

        builder.Services.TryAddSingleton(typeof(OpenIddictUnoTokenStore<>));

        return new OpenIddictUnoTokenPersistenceBuilder(builder.Services);
    }
}
