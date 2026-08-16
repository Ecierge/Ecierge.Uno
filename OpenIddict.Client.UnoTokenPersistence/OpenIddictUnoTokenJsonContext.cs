using System.Text.Json.Serialization;

namespace OpenIddict.Client.UnoTokenPersistence;

/// <summary>
/// Source-generated <see cref="JsonSerializerContext"/> for <see cref="OpenIddictUnoToken"/>.
/// </summary>
/// <remarks>
/// <see cref="Uno.Extensions.Storage.KeyValueStorage.ApplicationDataKeyValueStorage"/> serializes
/// <see cref="OpenIddictUnoToken"/> through the host's <c>ISerializer</c>, which requires an explicit
/// <see cref="System.Text.Json.Serialization.Metadata.JsonTypeInfo"/> when reflection-based serialization
/// is disabled (see <c>IHostBuilder.UseSerialization</c>). Register
/// <see cref="OpenIddictUnoTokenJsonContext.Default"/> with the host's <c>IServiceCollection.AddJsonTypeInfo()</c>.
/// </remarks>
[JsonSourceGenerationOptions(
    PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
    PropertyNameCaseInsensitive = true,
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull)]
[JsonSerializable(typeof(OpenIddictUnoToken))]
public partial class OpenIddictUnoTokenJsonContext : JsonSerializerContext
{
}
