namespace Uno.Extensions.Storage.KeyValueStorage;

using System.Diagnostics.CodeAnalysis;

public static class KeyValueStorageExtensions
{
    public static async ValueTask<string[]> GetNormalizedKeysAsync([NotNull] this IKeyValueStorage storage, CancellationToken ct)
    {
        var keys = await storage.GetKeysAsync(ct);
        return keys.Select(key => key.TrimEnd("_ADCSSS", StringComparison.InvariantCulture)).ToArray();
    }
}
