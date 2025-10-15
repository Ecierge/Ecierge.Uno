/*
 * Licensed under the Apache License, Version 2.0 (http://www.apache.org/licenses/LICENSE-2.0)
 * See https://github.com/openiddict/openiddict-core for more information concerning
 * the license and the contributors participating to this project.
 */

using System.ComponentModel;

using Microsoft.Extensions.DependencyInjection.Extensions;

using OpenIddict.Client.UnoTokenPersistence;
using OpenIddict.Core;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Exposes the necessary methods required to configure the OpenIddict MongoDB services.
/// </summary>
public sealed class OpenIddictUnoTokenPersistenceBuilder
{
    /// <summary>
    /// Initializes a new instance of <see cref="OpenIddictUnoTokenPersistenceBuilder"/>.
    /// </summary>
    /// <param name="services">The services collection.</param>
    public OpenIddictUnoTokenPersistenceBuilder(IServiceCollection services)
        => Services = services ?? throw new ArgumentNullException(nameof(services));

    /// <summary>
    /// Gets the services collection.
    /// </summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public IServiceCollection Services { get; }

    /// <summary>
    /// Amends the default OpenIddict MongoDB configuration.
    /// </summary>
    /// <param name="configuration">The delegate used to configure the OpenIddict options.</param>
    /// <remarks>This extension can be safely called multiple times.</remarks>
    /// <returns>The <see cref="OpenIddictUnoTokenPersistenceBuilder"/> instance.</returns>
    public OpenIddictUnoTokenPersistenceBuilder Configure(Action<OpenIddictUnoTokenPersistenceOptions> configuration)
    {
        if (configuration is null)
        {
            throw new ArgumentNullException(nameof(configuration));
        }

        Services.Configure(configuration);

        return this;
    }

    /// <summary>
    /// Configures OpenIddict to use the specified entity as the default token entity.
    /// </summary>
    /// <returns>The <see cref="OpenIddictUnoTokenPersistenceBuilder"/> instance.</returns>
    public OpenIddictUnoTokenPersistenceBuilder ReplaceDefaultTokenEntity<TToken>()
        where TToken : OpenIddictUnoToken
    {
        Services.Replace(
            ServiceDescriptor.Scoped<IOpenIddictTokenManager>(static provider =>
                provider.GetRequiredService<OpenIddictTokenManager<TToken>>()
            )
        );

        Services.Replace(
            ServiceDescriptor.Scoped<IOpenIddictTokenStore<TToken>, OpenIddictUnoTokenStore<TToken>>()
        );

        return this;
    }

    /// <summary>
    /// Replaces the default token identifier prefix (by default, openiddict).
    /// </summary>
    /// <param name="prefix">Token identifier prefix</param>
    /// <returns>The <see cref="OpenIddictUnoTokenPersistenceBuilder"/> instance.</returns>
    public OpenIddictUnoTokenPersistenceBuilder SetTokenIdentifierPrefix(string prefix)
    {
        if (string.IsNullOrEmpty(prefix))
        {
            throw new ArgumentException(SR.GetResourceString(SR.ID0261), nameof(prefix));
        }

        return Configure(options => options.TokenIdentifierPrefix = prefix);
    }

    /// <inheritdoc/>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public override bool Equals(object? obj) => base.Equals(obj);

    /// <inheritdoc/>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public override int GetHashCode() => base.GetHashCode();

    /// <inheritdoc/>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public override string? ToString() => base.ToString();
}
