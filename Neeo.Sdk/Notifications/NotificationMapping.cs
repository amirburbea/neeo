using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Neeo.Sdk.Devices;
using Neeo.Sdk.Utilities;

namespace Neeo.Sdk.Notifications;

/// <summary>
/// Interface for a service that can get (and cache) the notification keys for a device and component.
/// </summary>
public interface INotificationMapping
{
    /// <summary>
    /// Given an adapter, device identifier and component name, get the associated notification keys
    /// from the NEEO Brain.
    /// </summary>
    /// <param name="adapter">The device adapter.</param>
    /// <param name="deviceId">The device identifier.</param>
    /// <param name="componentName">The name of the component.</param>
    /// <param name="cancellationToken">Token to monitor for cancellation requests.</param>
    /// <returns><see cref="ValueTask"/> to represent the asynchronous operation.</returns>
    ValueTask<string[]> GetNotificationKeysAsync(IDeviceAdapter adapter, string deviceId, string componentName, CancellationToken cancellationToken = default);
}

internal sealed class NotificationMapping(
    IApiClient client,
    ISdkEnvironment environment,
    ILogger<NotificationMapping> logger
) : INotificationMapping
{
    private readonly ConcurrentDictionary<string, Task<Dictionary<string, string[]>?>> _cache = new(StringComparer.Ordinal);

    public async ValueTask<string[]> GetNotificationKeysAsync(IDeviceAdapter adapter, string deviceId, string componentName, CancellationToken cancellationToken)
    {
        string cacheKey = string.Concat(adapter.AdapterName, "|", deviceId);
        if (this._cache.GetValueOrDefault(cacheKey) is not { IsCompletedSuccessfully: true } loadTask)
        {
            return await this.ResolveKeysAsync(adapter, deviceId, componentName, cacheKey, cancellationToken).ConfigureAwait(false);
        }
        if (loadTask.Result is not { Count: > 0 } dictionary)
        {
            return [];
        }
        if (dictionary.TryGetValue(componentName, out string[]? keys))
        {
            return keys;
        }
        logger.LogWarning("Component {deviceName} {component} not found.", adapter.DeviceName, componentName);
        this._cache.TryRemove(cacheKey, out _);
        return [];
    }

    private async Task<Dictionary<string, string[]>?> FetchNotificationKeysAsync(string adapterName, string deviceId, CancellationToken cancellationToken)
    {
        string url = string.Format(BrainUrlPaths.NotificationKeyFormat, environment.SdkAdapterName, adapterName, deviceId);
        Entry[] entries = await client.GetAsync<Entry[]>(url, cancellationToken).ConfigureAwait(false);
        if (entries.Length == 0)
        {
            return null;
        }
        // entries.Length is a maximal case, there will likely be some grouping.
        Dictionary<string, List<string>> groupedEventKeys = new(entries.Length, StringComparer.Ordinal);
        foreach (Entry entry in entries)
        {
            if (groupedEventKeys.TryGetValue(entry.Name, out List<string>? list))
            {
                list.Add(entry.EventKey);
            }
            else
            {
                groupedEventKeys.Add(entry.Name, [entry.EventKey]);
            }
        }
        Dictionary<string, string[]> output = new(groupedEventKeys.Count, StringComparer.Ordinal);
        foreach ((string key, List<string> value) in groupedEventKeys)
        {
            output.Add(key, [.. value]);
        }
        return output;
    }

    private async Task<Dictionary<string, string[]>?> LoadKeysAsync(string cacheKey, string adapterName, string deviceId, CancellationToken cancellationToken)
    {
        try
        {
            return await this.FetchNotificationKeysAsync(adapterName, deviceId, cancellationToken).ConfigureAwait(false);
        }
        catch
        {
            this._cache.TryRemove(cacheKey, out _);
            throw;
        }
    }

    private async Task<string[]> ResolveKeysAsync(IDeviceAdapter adapter, string deviceId, string componentName, string cacheKey, CancellationToken cancellationToken)
    {
        this.TryRemoveFaultedOrCanceled(cacheKey);
        Task<Dictionary<string, string[]>?> loadTask = this._cache.GetOrAdd(
            cacheKey,
            key => this.LoadKeysAsync(key, adapter.AdapterName, deviceId, cancellationToken)
        );
        if (await loadTask.ConfigureAwait(false) is not { Count: not 0 } keysByName)
        {
            return [];
        }
        else if (keysByName.TryGetValue(componentName, out string[]? keys))
        {
            return keys;
        }
        logger.LogWarning("Component {deviceName} {component} not found.", adapter.DeviceName, componentName);
        this._cache.TryRemove(cacheKey, out _);
        return [];
    }

    private void TryRemoveFaultedOrCanceled(string cacheKey)
    {
        if (this._cache.TryGetValue(cacheKey, out Task<Dictionary<string, string[]>?>? existing) && existing.IsCompleted && !existing.IsCompletedSuccessfully)
        {
            this._cache.TryRemove(cacheKey, out _);
        }
    }

    public readonly record struct Entry(string EventKey, string Name);
}
