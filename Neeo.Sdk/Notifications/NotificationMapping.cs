using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
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
    /// Given an adapter, device identifier and component name, get the associated notification keys from the NEEO Brain.
    /// </summary>
    /// <param name="adapter">The device adapter.</param>
    /// <param name="deviceId">The device identifier.</param>
    /// <param name="componentName">The name of the component.</param>
    /// <param name="cancellationToken">Token to monitor for cancellation requests.</param>
    /// <returns><see cref="ValueTask"/> to represent the asynchronous operation.</returns>
    ValueTask<string[]> GetNotificationKeysAsync(IDeviceAdapter adapter, string deviceId, string componentName, CancellationToken cancellationToken = default);
}

internal sealed class NotificationMapping(IApiClient client, ISdkEnvironment environment, ILogger<NotificationMapping> logger) : INotificationMapping
{
    private readonly ConcurrentDictionary<string, EntryCache> _cache = new();
    private readonly string _sdkAdapterName = environment.SdkAdapterName;

    public async ValueTask<string[]> GetNotificationKeysAsync(IDeviceAdapter adapter, string deviceId, string componentName, CancellationToken cancellationToken)
    {
        string cacheKey = string.Concat(adapter.AdapterName, "|", deviceId);
        if (!this._cache.TryGetValue(cacheKey, out EntryCache? entries))
        {
            this._cache[cacheKey] = entries = await this.FetchEntriesAsync(adapter.AdapterName, deviceId, cancellationToken).ConfigureAwait(false);
        }
        if (entries.GetNotificationKeys(componentName) is { Length: not 0 } keys)
        {
            return keys;
        }
        logger.LogWarning("Component {deviceName} {component} not found.", adapter.DeviceName, componentName); // Maybe our definition is out of date?
        this._cache.TryRemove(cacheKey, out _);
        return [];
    }

    private Task<EntryCache> FetchEntriesAsync(string adapterName, string deviceId, CancellationToken cancellationToken) => client.GetAsync(
        string.Format(UrlPaths.NotificationKeyFormat, this._sdkAdapterName, adapterName, deviceId),
        static (Entry[] entries) => new EntryCache(entries),
        cancellationToken
    );

    public readonly record struct Entry(string EventKey, string Name, string? Label);

    private sealed class EntryCache(Entry[] entries)
    {
        private readonly Dictionary<string, string[]> _cache = [];

        public string[] GetNotificationKeys(string componentName)
        {
            if (!this._cache.TryGetValue(componentName, out string[]? keys))
            {
                this._cache.Add(
                    componentName,
                    keys = Find(static entry => entry.Name) is { Length: > 0 } matches
                        ? matches
                        : Find(static entry => entry.Label)
                );
            }
            return keys;

            string[] Find(Func<Entry, string?> projection)
            {
                return [.. entries.Where(entry => componentName == projection(entry)).Select(entry => entry.EventKey).Distinct()];
            }
        }
    }
}
