using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.Logging;

using Uno.Extensions.Logging;

using FastAsyncLock = Uno.Extensions.Threading.FastAsyncLock;

namespace Uno.Extensions.Storage.KeyValueStorage;

internal record InMemoryKeyValueStorage(ILogger<InMemoryKeyValueStorage> Logger) : IKeyValueStorage
{
    public const string Name = "InMemory";

    private readonly FastAsyncLock @lock = new FastAsyncLock();
    private readonly Dictionary<string, object> values = new Dictionary<string, object>();

    /// <inheritdoc />
    public bool IsEncrypted => false;

    /// <inheritdoc />
    public async ValueTask ClearAsync(string? name, CancellationToken ct)
    {
        if (Logger.IsEnabled(LogLevel.Debug))
        {
            Logger.LogDebugMessage($"Clearing value for key '{name}'.");
        }

        using (await @lock.LockAsync(ct))
        {
            if (name is not null)
            {
                values.Remove(name);
            }
            else
            {
                values.Clear();
            }
        }

        if (Logger.IsEnabled(LogLevel.Information))
        {
            Logger.LogInformationMessage($"Cleared value for key '{name}'.");
        }
    }

    /// <inheritdoc />
    public async ValueTask<T?> GetAsync<T>(string name, CancellationToken ct)
    {
        if (Logger.IsEnabled(LogLevel.Debug))
        {
            Logger.LogDebugMessage($"Getting value for key '{name}'.");
        }

        using (await @lock.LockAsync(ct))
        {
            if (values.TryGetValue(name, out var obj))
            {
                var value = (T)obj;

                if (Logger.IsEnabled(LogLevel.Information))
                {
                    Logger.LogInformationMessage($"Retrieved value for key '{name}'.");
                }

                return value;
            }

            if (Logger.IsEnabled(LogLevel.Information))
            {
                Logger.LogInformationMessage($"Key '{name}' not found.");
            }

            return default(T?);
        }
    }

    /// <inheritdoc />
    public async ValueTask SetAsync<T>(string name, T value, CancellationToken ct) where T : notnull
    {
        if (Logger.IsEnabled(LogLevel.Debug))
        {
            Logger.LogDebugMessage($"Setting value for key '{name}'.");
        }

        using (await @lock.LockAsync(ct))
        {
            values[name] = value;
        }

        if (Logger.IsEnabled(LogLevel.Information))
        {
            Logger.LogInformationMessage($"Value for key '{name}' set.");
        }
    }

    /// <inheritdoc />
    public async ValueTask<string[]> GetKeysAsync(CancellationToken ct)
    {
        using (await @lock.LockAsync(ct))
        {
            return values.Keys.ToArray();
        }
    }

}
