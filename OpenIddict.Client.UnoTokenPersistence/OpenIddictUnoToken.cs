/*
 * Licensed under the Apache License, Version 2.0 (http://www.apache.org/licenses/LICENSE-2.0)
 * See https://github.com/openiddict/openiddict-core for more information concerning
 * the license and the contributors participating to this project.
 */

using System.Diagnostics;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace OpenIddict.Client.UnoTokenPersistence;

/// <summary>
/// Represents an OpenIddict token.
/// </summary>
[DebuggerDisplay("Id = {Id.ToString(),nq} ; Subject = {Subject,nq} ; Type = {Type,nq} ; Status = {Status,nq}")]
public partial class OpenIddictUnoToken
{
    public virtual string? Id { get; set; } = Guid.NewGuid().ToString();
    /// <summary>
    /// Gets or sets the identifier of the application associated with the current token.
    /// </summary>
    public virtual string? ApplicationId { get; set; }

    /// <summary>
    /// Gets or sets the identifier of the authorization associated with the current token.
    /// </summary>
    public virtual string? AuthorizationId { get; set; }

    /// <summary>
    /// Gets or sets the concurrency token.
    /// </summary>
    public virtual string? ConcurrencyToken { get; set; } = Guid.NewGuid().ToString();

    /// <summary>
    /// Gets or sets the UTC creation date of the current token.
    /// </summary>
    public virtual DateTime? CreationDate { get; set; }

    /// <summary>
    /// Gets or sets the UTC expiration date of the current token.
    /// </summary>
    public virtual DateTime? ExpirationDate { get; set; }

    ///// <summary>
    ///// Gets or sets the unique identifier associated with the current token.
    ///// </summary>
    //public virtual string Id { get; set; }

    /// <summary>
    /// Gets or sets the payload of the current token, if applicable.
    /// Note: this property is only used for reference tokens
    /// and may be encrypted for security reasons.
    /// </summary>
    public virtual string? Payload { get; set; }

    /// <summary>
    /// Gets or sets the additional properties associated with the current token.
    /// </summary>
    /// <remarks>
    /// Stored as STJ extension data (<see cref="IDictionary{TKey, TValue}"/> of
    /// <see cref="JsonElement"/>). <see cref="System.Collections.Immutable.ImmutableDictionary{TKey, TValue}"/>
    /// is not a valid extension-data type. OpenIddict store maps this bag to/from
    /// <c>ImmutableDictionary&lt;string, JsonElement&gt;</c>.
    /// </remarks>
    [JsonExtensionData]
    public virtual IDictionary<string, JsonElement>? Properties { get; set; }

    /// <summary>
    /// Gets or sets the UTC redemption date of the current token.
    /// </summary>
    public virtual DateTime? RedemptionDate { get; set; }

    /// <summary>
    /// Gets or sets the reference identifier associated
    /// with the current token, if applicable.
    /// Note: this property is only used for reference tokens
    /// and may be hashed or encrypted for security reasons.
    /// </summary>
    public virtual string? ReferenceId { get; set; }

    /// <summary>
    /// Gets or sets the status of the current token.
    /// </summary>
    public virtual string? Status { get; set; }

    /// <summary>
    /// Gets or sets the subject associated with the current token.
    /// </summary>
    public virtual string? Subject { get; set; }

    /// <summary>
    /// Gets or sets the type of the current token.
    /// </summary>
    public virtual string? Type { get; set; }
}
