/*
 * Licensed under the Apache License, Version 2.0 (http://www.apache.org/licenses/LICENSE-2.0)
 * See https://github.com/openiddict/openiddict-core for more information concerning
 * the license and the contributors participating to this project.
 */

using System.Collections.Immutable;
using System.Data;
using System.Runtime.CompilerServices;
using System.Text.Json;

using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;

namespace OpenIddict.Client.UnoTokenPersistence;

/// <summary>
/// Provides methods allowing to manage the tokens stored in a <see cref="IKeyValueStorage"/>.
/// </summary>
/// <typeparam name="TToken">The type of the Token entity.</typeparam>
public class OpenIddictUnoTokenStore<TToken> : IOpenIddictTokenStore<TToken>
    where TToken : OpenIddictUnoToken
{
    public OpenIddictUnoTokenStore(
        IServiceProvider serviceProvider,
        IMemoryCache cache,
        // Until Uno.Extensions support native named services
        // we have to use GetRequiredDefaultInstance
        //IKeyValueStorage keyValueStorage,
        IOptionsMonitor<OpenIddictUnoTokenPersistenceOptions> options)
    {
        Cache = cache ?? throw new ArgumentNullException(nameof(cache));
        KeyValueStorage = serviceProvider.GetRequiredDefaultInstance<IKeyValueStorage>();
        //KeyValueStorage = keyValueStorage ?? throw new ArgumentNullException(nameof(keyValueStorage));
        Options = options ?? throw new ArgumentNullException(nameof(options));
    }

    /// <summary>
    /// Gets the memory cache associated with the current store.
    /// </summary>
    protected IMemoryCache Cache { get; }

    /// <summary>
    /// Gets the database context associated with the current store.
    /// </summary>
    protected IKeyValueStorage KeyValueStorage { get; }

    /// <summary>
    /// Gets the options associated with the current store.
    /// </summary>
    protected IOptionsMonitor<OpenIddictUnoTokenPersistenceOptions> Options { get; }

    private bool TokenPrefixPredicate(string key) => key.StartsWith(Options.CurrentValue.TokenIdentifierPrefix, StringComparison.InvariantCulture);

    /// <inheritdoc/>
    public virtual async ValueTask<long> CountAsync(CancellationToken cancellationToken)
    {
        var keys = await KeyValueStorage.GetKeysAsync(cancellationToken);
        return keys.LongCount(TokenPrefixPredicate);
    }

    /// <inheritdoc/>
    public virtual async ValueTask<long> CountAsync<TResult>(
        Func<IQueryable<TToken>, IQueryable<TResult>> query, CancellationToken cancellationToken)
    {
        if (query is null)
        {
            throw new ArgumentNullException(nameof(query));
        }

        var keys = await KeyValueStorage.GetKeysAsync(cancellationToken);
        var tokens = await
            Task.WhenAll(keys.Where(TokenPrefixPredicate).Select(key => KeyValueStorage.GetAsync<TToken>(key, cancellationToken).AsTask()));
        var querable = tokens.Where(token => token is not null).Select(token => token!).AsQueryable();
        return query(querable).LongCount();
    }

    /// <inheritdoc/>
    public virtual ValueTask CreateAsync(TToken token, CancellationToken cancellationToken)
    {
        token = token ?? throw new ArgumentNullException(nameof(token));
        var identifier = Options.CurrentValue.TokenIdentifierPrefix + token.Id;
        return KeyValueStorage.SetAsync(identifier, token, cancellationToken);
    }

    /// <inheritdoc/>
    public virtual ValueTask DeleteAsync(TToken token, CancellationToken cancellationToken)
    {
        token = token ?? throw new ArgumentNullException(nameof(token));
        var identifier = Options.CurrentValue.TokenIdentifierPrefix + token.Id;
        return KeyValueStorage.ClearAsync(identifier, cancellationToken);
    }

    /// <inheritdoc/>
    public virtual IAsyncEnumerable<TToken> FindAsync(string subject, string client, CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(subject))
        {
            throw new ArgumentException(SR.GetResourceString(SR.ID0198), nameof(subject));
        }

        if (string.IsNullOrEmpty(client))
        {
            throw new ArgumentException(SR.GetResourceString(SR.ID0124), nameof(client));
        }

        return ExecuteAsync(cancellationToken);

        async IAsyncEnumerable<TToken> ExecuteAsync([EnumeratorCancellation] CancellationToken cancellationToken)
        {
            var keys = await KeyValueStorage.GetKeysAsync(cancellationToken);
            keys = keys.Where(TokenPrefixPredicate).ToArray();
            foreach (var key in keys)
            {
                var token = await KeyValueStorage.GetAsync<TToken>(key, cancellationToken);
                if (token is not null && token.Subject == subject && token.ApplicationId == client)
                {
                    yield return token;
                }
            }
        }
    }

    /// <inheritdoc/>
    public virtual IAsyncEnumerable<TToken> FindAsync(
        string subject, string client,
        string status, CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(subject))
        {
            throw new ArgumentException(SR.GetResourceString(SR.ID0198), nameof(subject));
        }

        if (string.IsNullOrEmpty(client))
        {
            throw new ArgumentException(SR.GetResourceString(SR.ID0124), nameof(client));
        }

        if (string.IsNullOrEmpty(status))
        {
            throw new ArgumentException(SR.GetResourceString(SR.ID0199), nameof(status));
        }

        return ExecuteAsync(cancellationToken);

        async IAsyncEnumerable<TToken> ExecuteAsync([EnumeratorCancellation] CancellationToken cancellationToken)
        {
            var keys = await KeyValueStorage.GetKeysAsync(cancellationToken);
            keys = keys.Where(TokenPrefixPredicate).ToArray();
            foreach (var key in keys)
            {
                var token = await KeyValueStorage.GetAsync<TToken>(key, cancellationToken);
                if (token is not null && token.Subject == subject && token.ApplicationId == client && token.Status == status)
                {
                    yield return token;
                }
            }
        }
    }

    /// <inheritdoc/>
    public virtual IAsyncEnumerable<TToken> FindAsync(
        string subject, string client,
        string status, string type, CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(subject))
        {
            throw new ArgumentException(SR.GetResourceString(SR.ID0198), nameof(subject));
        }

        if (string.IsNullOrEmpty(client))
        {
            throw new ArgumentException(SR.GetResourceString(SR.ID0124), nameof(client));
        }

        if (string.IsNullOrEmpty(status))
        {
            throw new ArgumentException(SR.GetResourceString(SR.ID0199), nameof(status));
        }

        if (string.IsNullOrEmpty(type))
        {
            throw new ArgumentException(SR.GetResourceString(SR.ID0200), nameof(type));
        }

        return ExecuteAsync(cancellationToken);

        async IAsyncEnumerable<TToken> ExecuteAsync([EnumeratorCancellation] CancellationToken cancellationToken)
        {
            var keys = await KeyValueStorage.GetKeysAsync(cancellationToken);
            keys = keys.Where(TokenPrefixPredicate).ToArray();
            foreach (var key in keys)
            {
                var token = await KeyValueStorage.GetAsync<TToken>(key, cancellationToken);
                if (token is not null && token.Subject == subject && token.ApplicationId == client && token.Status == status && token.Type == type)
                {
                    yield return token;
                }
            }
        }
    }

    /// <inheritdoc/>
    public virtual IAsyncEnumerable<TToken> FindByApplicationIdAsync(string identifier, CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(identifier))
        {
            throw new ArgumentException(SR.GetResourceString(SR.ID0195), nameof(identifier));
        }

        return ExecuteAsync(cancellationToken);

        async IAsyncEnumerable<TToken> ExecuteAsync([EnumeratorCancellation] CancellationToken cancellationToken)
        {
            var keys = await KeyValueStorage.GetKeysAsync(cancellationToken);
            keys = keys.Where(TokenPrefixPredicate).ToArray();
            foreach (var key in keys)
            {
                var token = await KeyValueStorage.GetAsync<TToken>(key, cancellationToken);
                if (token is not null && token.ApplicationId == identifier)
                {
                    yield return token;
                }
            }
        }
    }

    /// <inheritdoc/>
    public virtual IAsyncEnumerable<TToken> FindByAuthorizationIdAsync(string identifier, CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(identifier))
        {
            throw new ArgumentException(SR.GetResourceString(SR.ID0195), nameof(identifier));
        }

        return ExecuteAsync(cancellationToken);

        async IAsyncEnumerable<TToken> ExecuteAsync([EnumeratorCancellation] CancellationToken cancellationToken)
        {
            var keys = await KeyValueStorage.GetKeysAsync(cancellationToken);
            keys = keys.Where(TokenPrefixPredicate).ToArray();
            foreach (var key in keys)
            {
                var token = await KeyValueStorage.GetAsync<TToken>(key, cancellationToken);
                if (token is not null && token.AuthorizationId == identifier)
                {
                    yield return token;
                }
            }
        }
    }

    /// <inheritdoc/>
    public virtual async ValueTask<TToken?> FindByIdAsync(string identifier, CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(identifier))
        {
            throw new ArgumentException(SR.GetResourceString(SR.ID0195), nameof(identifier));
        }

        return await KeyValueStorage.GetAsync<TToken>(identifier, cancellationToken);
    }

    /// <inheritdoc/>
    public virtual async ValueTask<TToken?> FindByReferenceIdAsync(string identifier, CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(identifier))
        {
            throw new ArgumentException(SR.GetResourceString(SR.ID0195), nameof(identifier));
        }

        var keys = await KeyValueStorage.GetKeysAsync(cancellationToken);
        keys = keys.Where(TokenPrefixPredicate).ToArray();
        foreach (var key in keys)
        {
            var token = await KeyValueStorage.GetAsync<TToken>(key, cancellationToken);
            if (token is not null && token.ReferenceId == identifier)
            {
                return token;
            }
        }
        return null;
    }

    /// <inheritdoc/>
    public virtual IAsyncEnumerable<TToken> FindBySubjectAsync(string subject, CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(subject))
        {
            throw new ArgumentException(SR.GetResourceString(SR.ID0198), nameof(subject));
        }

        return ExecuteAsync(cancellationToken);

        async IAsyncEnumerable<TToken> ExecuteAsync([EnumeratorCancellation] CancellationToken cancellationToken)
        {
            var keys = await KeyValueStorage.GetKeysAsync(cancellationToken);
            keys = keys.Where(TokenPrefixPredicate).ToArray();
            foreach (var key in keys)
            {
                var token = await KeyValueStorage.GetAsync<TToken>(key, cancellationToken);
                if (token is not null && token.Subject == subject)
                {
                    yield return token;
                }
            }
        }
    }

    /// <inheritdoc/>
    public virtual ValueTask<string?> GetApplicationIdAsync(TToken token, CancellationToken cancellationToken)
    {
        if (token is null)
        {
            throw new ArgumentNullException(nameof(token));
        }

        if (token.ApplicationId == null)
        {
            return new(result: null);
        }

        return new(token.ApplicationId);
    }

    /// <inheritdoc/>
    public virtual async ValueTask<TResult?> GetAsync<TState, TResult>(
        Func<IQueryable<TToken>, TState, IQueryable<TResult>> query,
        TState state, CancellationToken cancellationToken)
    {
        if (query is null)
        {
            throw new ArgumentNullException(nameof(query));
        }

        var keys = await KeyValueStorage.GetKeysAsync(cancellationToken);
        keys = keys.Where(TokenPrefixPredicate).ToArray();
        foreach (var key in keys)
        {
            var token = await KeyValueStorage.GetAsync<TToken>(key, cancellationToken);
            if (token is not null)
            {
                var result = query(new[] { token }.AsQueryable(), state).FirstOrDefault();
                if (result is not null)
                {
                    return result;
                }
            }
        }
        return default;
    }

    /// <inheritdoc/>
    public virtual ValueTask<string?> GetAuthorizationIdAsync(TToken token, CancellationToken cancellationToken)
    {
        if (token is null)
        {
            throw new ArgumentNullException(nameof(token));
        }

        if (token.AuthorizationId == null)
        {
            return new(result: null);
        }

        return new(token.AuthorizationId);
    }

    /// <inheritdoc/>
    public virtual ValueTask<DateTimeOffset?> GetCreationDateAsync(TToken token, CancellationToken cancellationToken)
    {
        if (token is null)
        {
            throw new ArgumentNullException(nameof(token));
        }

        if (token.CreationDate is null)
        {
            return new(result: null);
        }

        return new(DateTime.SpecifyKind(token.CreationDate.Value, DateTimeKind.Utc));
    }

    /// <inheritdoc/>
    public virtual ValueTask<DateTimeOffset?> GetExpirationDateAsync(TToken token, CancellationToken cancellationToken)
    {
        if (token is null)
        {
            throw new ArgumentNullException(nameof(token));
        }

        if (token.ExpirationDate is null)
        {
            return new(result: null);
        }

        return new(DateTime.SpecifyKind(token.ExpirationDate.Value, DateTimeKind.Utc));
    }

    /// <inheritdoc/>
    public virtual ValueTask<string?> GetIdAsync(TToken token, CancellationToken cancellationToken)
    {
        if (token is null)
        {
            throw new ArgumentNullException(nameof(token));
        }

        return new(token.Id);
    }

    /// <inheritdoc/>
    public virtual ValueTask<string?> GetPayloadAsync(TToken token, CancellationToken cancellationToken)
    {
        if (token is null)
        {
            throw new ArgumentNullException(nameof(token));
        }

        return new(token.Payload);
    }

    /// <inheritdoc/>
    public virtual ValueTask<ImmutableDictionary<string, JsonElement>> GetPropertiesAsync(TToken token, CancellationToken cancellationToken)
    {
        if (token is null)
        {
            throw new ArgumentNullException(nameof(token));
        }

        if (token.Properties is null)
        {
            return new(ImmutableDictionary.Create<string, JsonElement>());
        }

        return ValueTask.FromResult(token.Properties);
    }

    /// <inheritdoc/>
    public virtual ValueTask<DateTimeOffset?> GetRedemptionDateAsync(TToken token, CancellationToken cancellationToken)
    {
        if (token is null)
        {
            throw new ArgumentNullException(nameof(token));
        }

        if (token.RedemptionDate is null)
        {
            return new(result: null);
        }

        return new(DateTime.SpecifyKind(token.RedemptionDate.Value, DateTimeKind.Utc));
    }

    /// <inheritdoc/>
    public virtual ValueTask<string?> GetReferenceIdAsync(TToken token, CancellationToken cancellationToken)
    {
        if (token is null)
        {
            throw new ArgumentNullException(nameof(token));
        }

        return new(token.ReferenceId);
    }

    /// <inheritdoc/>
    public virtual ValueTask<string?> GetStatusAsync(TToken token, CancellationToken cancellationToken)
    {
        if (token is null)
        {
            throw new ArgumentNullException(nameof(token));
        }

        return new(token.Status);
    }

    /// <inheritdoc/>
    public virtual ValueTask<string?> GetSubjectAsync(TToken token, CancellationToken cancellationToken)
    {
        if (token is null)
        {
            throw new ArgumentNullException(nameof(token));
        }

        return new(token.Subject);
    }

    /// <inheritdoc/>
    public virtual ValueTask<string?> GetTypeAsync(TToken token, CancellationToken cancellationToken)
    {
        if (token is null)
        {
            throw new ArgumentNullException(nameof(token));
        }

        return new(token.Type);
    }

    /// <inheritdoc/>
    public virtual ValueTask<TToken> InstantiateAsync(CancellationToken cancellationToken)
    {
        try
        {
            return new(Activator.CreateInstance<TToken>());
        }

        catch (MemberAccessException exception)
        {
            return new(Task.FromException<TToken>(
                new InvalidOperationException(SR.GetResourceString(SR.ID0248), exception)));
        }
    }

    /// <inheritdoc/>
    public virtual async IAsyncEnumerable<TToken> ListAsync(
        int? count, int? offset, [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var keys = await KeyValueStorage.GetKeysAsync(cancellationToken);
        var query = keys.Where(TokenPrefixPredicate);

        if (offset.HasValue)
        {
            query = query.Skip(offset.Value);
        }

        if (count.HasValue)
        {
            query = query.Take(count.Value);
        }

        keys = query.ToArray();

        foreach (var key in keys)
        {
            var token = await KeyValueStorage.GetAsync<TToken>(key, cancellationToken);
            if (token is not null)
            {
                yield return token;
            }
        }
    }

    /// <inheritdoc/>
    public virtual IAsyncEnumerable<TResult> ListAsync<TState, TResult>(
        Func<IQueryable<TToken>, TState, IQueryable<TResult>> query,
        TState state, CancellationToken cancellationToken)
    {
        if (query is null)
        {
            throw new ArgumentNullException(nameof(query));
        }

        return ExecuteAsync(cancellationToken);

        async IAsyncEnumerable<TResult> ExecuteAsync([EnumeratorCancellation] CancellationToken cancellationToken)
        {
            var keys = await KeyValueStorage.GetKeysAsync(cancellationToken);
            keys = keys.Where(TokenPrefixPredicate).ToArray();

            foreach (var key in keys)
            {
                var token = await KeyValueStorage.GetAsync<TToken>(key, cancellationToken);
                if (token is not null)
                {
                    var result = query(new[] { token }.AsQueryable(), state).FirstOrDefault();
                    if (result is not null)
                    {
                        yield return result;
                    }
                }
            }
        }
    }

    /// <inheritdoc/>
    public virtual async ValueTask<long> PruneAsync(DateTimeOffset threshold, CancellationToken cancellationToken)
    {
        var result = 0L;

        // Note: directly deleting the resulting set of an aggregate query is not supported by MongoDB.
        // To work around this limitation, the token identifiers are stored in an intermediate list
        // and delete requests are sent to remove the documents corresponding to these identifiers.

        var keys = await KeyValueStorage.GetKeysAsync(cancellationToken);
        keys = keys.Where(TokenPrefixPredicate).ToArray();
        foreach (var key in keys)
        {
            var token = await KeyValueStorage.GetAsync<TToken>(key, cancellationToken);
            if (token is not null && (token.CreationDate < threshold.UtcDateTime ||
                                      (token.Status != Statuses.Inactive && token.Status != Statuses.Valid) ||
                                      token.ExpirationDate < DateTime.UtcNow))
            {
                var identifier = Options.CurrentValue.TokenIdentifierPrefix + token.Id;
                await KeyValueStorage.ClearAsync(identifier, cancellationToken);
                result++;
            }
        }

        return result;
    }

    /// <inheritdoc/>
    public virtual ValueTask SetApplicationIdAsync(TToken token, string? identifier, CancellationToken cancellationToken)
    {
        if (token is null)
        {
            throw new ArgumentNullException(nameof(token));
        }

        token.ApplicationId = identifier;

        return default;
    }

    /// <inheritdoc/>
    public virtual ValueTask SetAuthorizationIdAsync(TToken token, string? identifier, CancellationToken cancellationToken)
    {
        if (token is null)
        {
            throw new ArgumentNullException(nameof(token));
        }

        token.AuthorizationId = identifier;

        return default;
    }

    /// <inheritdoc/>
#pragma warning disable CA1716 // Identifiers should not match keywords
    public virtual ValueTask SetCreationDateAsync(TToken token, DateTimeOffset? date, CancellationToken cancellationToken)
    {
        if (token is null)
        {
            throw new ArgumentNullException(nameof(token));
        }

        token.CreationDate = date?.UtcDateTime;

        return default;
    }

    /// <inheritdoc/>
    public virtual ValueTask SetExpirationDateAsync(TToken token, DateTimeOffset? date, CancellationToken cancellationToken)
    {
        if (token is null)
        {
            throw new ArgumentNullException(nameof(token));
        }

        token.ExpirationDate = date?.UtcDateTime;

        return default;
    }
#pragma warning restore CA1716 // Identifiers should not match keywords

    /// <inheritdoc/>
    public virtual ValueTask SetPayloadAsync(TToken token, string? payload, CancellationToken cancellationToken)
    {
        if (token is null)
        {
            throw new ArgumentNullException(nameof(token));
        }

        token.Payload = payload;

        return default;
    }

    /// <inheritdoc/>
    public virtual ValueTask SetPropertiesAsync(TToken token,
        ImmutableDictionary<string, JsonElement> properties, CancellationToken cancellationToken)
    {
        if (token is null)
        {
            throw new ArgumentNullException(nameof(token));
        }

        token.Properties = properties;

        return default;
    }

    /// <inheritdoc/>
#pragma warning disable CA1716 // Identifiers should not match keywords
    public virtual ValueTask SetRedemptionDateAsync(TToken token, DateTimeOffset? date, CancellationToken cancellationToken)
    {
        if (token is null)
        {
            throw new ArgumentNullException(nameof(token));
        }

        token.RedemptionDate = date?.UtcDateTime;

        return default;
    }
#pragma warning restore CA1716 // Identifiers should not match keywords

    /// <inheritdoc/>
    public virtual ValueTask SetReferenceIdAsync(TToken token, string? identifier, CancellationToken cancellationToken)
    {
        if (token is null)
        {
            throw new ArgumentNullException(nameof(token));
        }

        token.ReferenceId = identifier;

        return default;
    }

    /// <inheritdoc/>
    public virtual ValueTask SetStatusAsync(TToken token, string? status, CancellationToken cancellationToken)
    {
        if (token is null)
        {
            throw new ArgumentNullException(nameof(token));
        }

        token.Status = status;

        return default;
    }

    /// <inheritdoc/>
    public virtual ValueTask SetSubjectAsync(TToken token, string? subject, CancellationToken cancellationToken)
    {
        if (token is null)
        {
            throw new ArgumentNullException(nameof(token));
        }

        token.Subject = subject;

        return default;
    }

    /// <inheritdoc/>
    public virtual ValueTask SetTypeAsync(TToken token, string? type, CancellationToken cancellationToken)
    {
        if (token is null)
        {
            throw new ArgumentNullException(nameof(token));
        }

        token.Type = type;

        return default;
    }

    /// <inheritdoc/>
    public virtual ValueTask UpdateAsync(TToken token, CancellationToken cancellationToken)
    {
        if (token is null)
        {
            throw new ArgumentNullException(nameof(token));
        }

        // Generate a new concurrency token and attach it
        // to the token before persisting the changes.
        var timestamp = token.ConcurrencyToken;
        token.ConcurrencyToken = Guid.NewGuid().ToString();

        var identifier = Options.CurrentValue.TokenIdentifierPrefix + token.Id;
        return KeyValueStorage.SetAsync(identifier, token, cancellationToken);
    }

    public async ValueTask<long> RevokeAsync(string? subject, string? client, string? status, string? type, CancellationToken cancellationToken)
    {
        var keys = await KeyValueStorage.GetKeysAsync(cancellationToken);
        keys = keys.Where(TokenPrefixPredicate).ToArray();
        long count = 0L;

        foreach (var key in keys)
        {
            var token = await KeyValueStorage.GetAsync<TToken>(key, cancellationToken);
            if (token is null)
            {
                continue;
            }

            var matches =
                (subject is null || token.Subject == subject) &&
                (client is null || token.AuthorizationId == client) &&
                (status is null || token.Status == status) &&
                (type is null || token.Type == type);

            if (matches)
            {
                token.Status = Statuses.Revoked;
                var tokenIdentifier = Options.CurrentValue.TokenIdentifierPrefix + token.Id;
                await KeyValueStorage.SetAsync(tokenIdentifier, token, cancellationToken);
                count++;
            }
        }

        return count;
    }

    /// <inheritdoc/>
    public virtual async ValueTask<long> RevokeByApplicationIdAsync(string identifier, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(identifier))
        {
            throw new ArgumentException(SR.GetResourceString(SR.ID0195), nameof(identifier));
        }
        var keys = await KeyValueStorage.GetKeysAsync(cancellationToken);
        keys = keys.Where(TokenPrefixPredicate).ToArray();
        long count = 0L;
        foreach (var key in keys)
        {
            var token = await KeyValueStorage.GetAsync<TToken>(key, cancellationToken);
            if (token is not null && token.ApplicationId == identifier)
            {
                token.Status = Statuses.Revoked;
                var tokenIdentifier = Options.CurrentValue.TokenIdentifierPrefix + token.Id;
                await KeyValueStorage.SetAsync(tokenIdentifier, token, cancellationToken);
                count++;
            }
        }
        return count;
    }
    public virtual async ValueTask<long> RevokeByAuthorizationIdAsync(string identifier, CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(identifier))
        {
            throw new ArgumentException(SR.GetResourceString(SR.ID0195), nameof(identifier));
        }

        var keys = await KeyValueStorage.GetKeysAsync(cancellationToken);
        keys = keys.Where(TokenPrefixPredicate).ToArray();
        long count = 0L;
        foreach (var key in keys)
        {
            var token = await KeyValueStorage.GetAsync<TToken>(key, cancellationToken);
            if (token is not null && token.AuthorizationId == identifier)
            {
                token.Status = Statuses.Revoked;
                var tokenIdentifier = Options.CurrentValue.TokenIdentifierPrefix + token.Id;
                await KeyValueStorage.SetAsync(tokenIdentifier, token, cancellationToken);
                count++;
            }
        }
        return count;
    }
    public virtual async ValueTask<long> RevokeBySubjectAsync(string subject, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(subject))
        {
            throw new ArgumentException(SR.GetResourceString(SR.ID0195), nameof(subject));
        }

        var keys = await KeyValueStorage.GetKeysAsync(cancellationToken);
        keys = keys.Where(TokenPrefixPredicate).ToArray();
        long count = 0L;
        foreach (var key in keys)
        {
            var token = await KeyValueStorage.GetAsync<TToken>(key, cancellationToken);
            if (token is not null && token.Subject == subject)
            {
                token.Status = Statuses.Revoked;
                var tokenIdentifier = Options.CurrentValue.TokenIdentifierPrefix + token.Id;
                await KeyValueStorage.SetAsync(tokenIdentifier, token, cancellationToken);
                count++;
            }
        }
        return count;
    }
}
