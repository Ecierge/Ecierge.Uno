/*
 * Licensed under the Apache License, Version 2.0 (http://www.apache.org/licenses/LICENSE-2.0)
 * See https://github.com/openiddict/openiddict-core for more information concerning
 * the license and the contributors participating to this project.
 */

namespace OpenIddict.Client.UnoTokenPersistence;

/// <summary>
/// Provides various settings needed to configure
/// the OpenIddict Entity Framework Core integration.
/// </summary>
public sealed class OpenIddictUnoTokenPersistenceOptions
{

    /// <summary>
    /// Gets or sets a token identifier prefix used to identify OpenIddict Client tokens.
    /// </summary>
    public string TokenIdentifierPrefix { get; set; } = "openiddict";
}
