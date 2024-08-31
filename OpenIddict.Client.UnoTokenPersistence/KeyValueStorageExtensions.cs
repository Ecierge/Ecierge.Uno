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

internal static class StringExtensions
{
    public static string TrimEnd(this string str, string suffix, StringComparison comparisonType)
    {
        if (str.EndsWith(suffix, comparisonType)) return str[..^suffix.Length];
        else return str;
    }
}
