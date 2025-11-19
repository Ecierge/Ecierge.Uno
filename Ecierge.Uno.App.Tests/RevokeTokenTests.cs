namespace Ecierge.Uno.App.Tests;

using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

using OpenIddict.Abstractions;
using OpenIddict.Client.UnoTokenPersistence;

using global::Uno.Extensions;
using global::Uno.Extensions.Storage.KeyValueStorage;
using NUnit.Framework;

public class RevokeTokenTests
{
    private ServiceProvider provider = null!;

    [SetUp]
    public void Setup()
    {
        var services = new ServiceCollection();

        services.AddLogging(builder =>
        {
            builder.AddConsole();
            builder.AddDebug();
            builder.SetMinimumLevel(LogLevel.Debug);
        });

        services.AddMemoryCache();
        services.SetDefaultInstance<IKeyValueStorage, InMemoryKeyValueStorage>();
        services.AddScoped<IOpenIddictTokenStore<OpenIddictUnoToken>, OpenIddictUnoTokenStore<OpenIddictUnoToken>>();

        provider = services.BuildServiceProvider();
    }

    public record RevokeTokenTestCase(string? Subject, string? ApplicationId, string? Status, string? Type);

    private readonly static RevokeTokenTestCase[] revokeTokenTestCases = new[]
    {
        new RevokeTokenTestCase(null, "test-Auth", OpenIddictConstants.Statuses.Valid, OpenIddictConstants.TokenTypes.Bearer),
        new RevokeTokenTestCase("test-user", null, OpenIddictConstants.Statuses.Valid, OpenIddictConstants.TokenTypes.Bearer),
        new RevokeTokenTestCase("test-user", "test-Auth", null, OpenIddictConstants.TokenTypes.Bearer),
        new RevokeTokenTestCase("test-user", "test-Auth", OpenIddictConstants.Statuses.Valid, null)
    };

    [TestCaseSource(nameof(revokeTokenTestCases))]
    public async Task RevokeAsyncTest(RevokeTokenTestCase ca)
    {
        var loggerFactory = provider.GetRequiredService<ILoggerFactory>();
        var logger = loggerFactory.CreateLogger<InMemoryKeyValueStorage>();

        var tokenStore = provider.GetRequiredService<IOpenIddictTokenStore<OpenIddictUnoToken>>();

        var token1 = new OpenIddictUnoToken
        {
            Subject = ca.Subject,
            ApplicationId = ca.ApplicationId,
            Status = ca.Status,
            Type = ca.Type
        };

        var token2 = new OpenIddictUnoToken
        {
            Subject = ca.Subject,
            ApplicationId = ca.ApplicationId,
            Status = ca.Status,
            Type = ca.Type
        };

        var token3 = new OpenIddictUnoToken
        {
            ApplicationId = "test-AppOther",
            Subject = "test-userOther",
            Status = OpenIddictConstants.Statuses.Valid,
            Type = OpenIddictConstants.TokenTypes.NotApplicable
        };
        await tokenStore.CreateAsync(token1, CancellationToken.None);
        await tokenStore.CreateAsync(token2, CancellationToken.None);
        await tokenStore.CreateAsync(token3, CancellationToken.None);

        var revokedCount = await tokenStore.RevokeAsync(ca.Subject, ca.ApplicationId, ca.Status, ca.Type, CancellationToken.None);

        Assert.That(revokedCount, Is.EqualTo(2));
    }

    [Test]
    public async Task RevokeByApplicationIdAsyncTest()
    {
        var loggerFactory = provider.GetRequiredService<ILoggerFactory>();
        var logger = loggerFactory.CreateLogger<InMemoryKeyValueStorage>();

        var tokenStore = provider.GetRequiredService<IOpenIddictTokenStore<OpenIddictUnoToken>>();

        var token1 = new OpenIddictUnoToken
        {
            ApplicationId = "test-App",
            AuthorizationId = "test-Auth",
            Subject = "test-user",
            Status = OpenIddictConstants.Statuses.Valid,
            Type = OpenIddictConstants.TokenTypes.Bearer
        };

        var token2 = new OpenIddictUnoToken
        {
            ApplicationId = "test-App",
            AuthorizationId = "test-Auth",
            Subject = "test-user",
            Status = OpenIddictConstants.Statuses.Valid,
            Type = OpenIddictConstants.TokenTypes.Bearer
        };

        var token3 = new OpenIddictUnoToken
        {
            ApplicationId = "test-AppOther",
            AuthorizationId = "test-AuthOther",
            Subject = "test-user",
            Status = OpenIddictConstants.Statuses.Valid,
            Type = OpenIddictConstants.TokenTypes.Bearer
        };
        await tokenStore.CreateAsync(token1, CancellationToken.None);
        await tokenStore.CreateAsync(token2, CancellationToken.None);
        await tokenStore.CreateAsync(token3, CancellationToken.None);

        var revokedCount = await tokenStore.RevokeByApplicationIdAsync("test-App", CancellationToken.None);

        Assert.That(revokedCount, Is.EqualTo(2));
    }

    [Test]
    public async Task RevokeByAuthorizationIdAsyncTest()
    {
        var loggerFactory = provider.GetRequiredService<ILoggerFactory>();
        var logger = loggerFactory.CreateLogger<InMemoryKeyValueStorage>();

        var tokenStore = provider.GetRequiredService<IOpenIddictTokenStore<OpenIddictUnoToken>>();

        var token1 = new OpenIddictUnoToken
        {
            AuthorizationId = "test-Auth",
            Subject = "test-user",
            Status = OpenIddictConstants.Statuses.Valid,
            Type = OpenIddictConstants.TokenTypes.Bearer
        };

        var token2 = new OpenIddictUnoToken
        {
            AuthorizationId = "test-Auth",
            Subject = "test-user",
            Status = OpenIddictConstants.Statuses.Valid,
            Type = OpenIddictConstants.TokenTypes.Bearer
        };

        var token3 = new OpenIddictUnoToken
        {
            AuthorizationId = "test-AuthOther",
            Subject = "test-user",
            Status = OpenIddictConstants.Statuses.Valid,
            Type = OpenIddictConstants.TokenTypes.Bearer
        };
        await tokenStore.CreateAsync(token1, CancellationToken.None);
        await tokenStore.CreateAsync(token2, CancellationToken.None);
        await tokenStore.CreateAsync(token3, CancellationToken.None);

        var revokedCount = await tokenStore.RevokeByAuthorizationIdAsync("test-Auth", CancellationToken.None);

        Assert.That(revokedCount, Is.EqualTo(2));
    }

    [Test]
    public async Task RevokeBySubjectAsyncTest()
    {
        var loggerFactory = provider.GetRequiredService<ILoggerFactory>();
        var logger = loggerFactory.CreateLogger<InMemoryKeyValueStorage>();

        var tokenStore = provider.GetRequiredService<IOpenIddictTokenStore<OpenIddictUnoToken>>();

        var token1 = new OpenIddictUnoToken
        {
            Subject = "test-user",
            Status = OpenIddictConstants.Statuses.Valid,
            Type = OpenIddictConstants.TokenTypes.Bearer
        };

        var token2 = new OpenIddictUnoToken
        {
            Subject = "test-user",
            Status = OpenIddictConstants.Statuses.Valid,
            Type = OpenIddictConstants.TokenTypes.Bearer
        };

        var token3 = new OpenIddictUnoToken
        {
            Subject = "test-other",
            Status = OpenIddictConstants.Statuses.Valid,
            Type = OpenIddictConstants.TokenTypes.Bearer
        };
        await tokenStore.CreateAsync(token1, CancellationToken.None);
        await tokenStore.CreateAsync(token2, CancellationToken.None);
        await tokenStore.CreateAsync(token3, CancellationToken.None);

        var revokedCount = await tokenStore.RevokeBySubjectAsync("test-user", CancellationToken.None);

        Assert.That(revokedCount, Is.EqualTo(2));
    }
}
