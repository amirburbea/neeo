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

internal sealed class NotificationMapping : INotificationMapping
{
    private readonly ConcurrentDictionary<string, EntryCache> _cache = new();
    private readonly IApiClient _client;
    private readonly ILogger<NotificationMapping> _logger;
    private readonly string _sdkAdapterName;

    public NotificationMapping(IApiClient client, ISdkEnvironment environment, ILogger<NotificationMapping> logger)
    {
        this._sdkAdapterName = environment.SdkAdapterName;
        this._client = client;
        this._logger = logger;
    }

    public async ValueTask<string[]> GetNotificationKeysAsync(IDeviceAdapter adapter, string deviceId, string componentName, CancellationToken cancellationToken)
    {
        string cacheKey = string.Concat(adapter.AdapterName, "|", deviceId);
        if (!this._cache.TryGetValue(cacheKey, out EntryCache? entries))
        {
            this._cache[cacheKey] = entries = new(await this.FetchEntriesAsync(adapter.AdapterName, deviceId, cancellationToken).ConfigureAwait(false));
        }
        if (entries.GetNotificationKeys(componentName) is { Length: not 0 } keys)
        {
            return keys;
        }
        this._logger.LogWarning("Component {deviceName} {component} not found.", adapter.DeviceName, componentName); // Maybe our definition is out of date?
        this._cache.TryRemove(cacheKey, out _);
        return Array.Empty<string>();
    }

    private Task<Entry[]> FetchEntriesAsync(string adapterName, string deviceId, CancellationToken cancellationToken) => this._client.GetAsync(
        string.Format(UrlPaths.NotificationKeyFormat, this._sdkAdapterName, adapterName, deviceId),
        static (Entry[] entries) => entries,
        cancellationToken
    );

    public readonly record struct Entry(string EventKey, string Name, string? Label);

    private sealed record EntryCache(Entry[] Entries)
    {
        private readonly Dictionary<string, string[]> _cache = new();

        public string[] GetNotificationKeys(string componentName)
        {
            if (!this._cache.TryGetValue(componentName, out string[]? keys))
            {
                this._cache.Add(componentName, keys = GetKeys());
            }
            return keys;

            string[] Find(Func<Entry, string?> projection)
            {
                int index = Array.FindIndex(this.Entries, Match);
                if (index == -1)
                {
                    return Array.Empty<string>();
                }
                if (index == this.Entries.Length - 1)
                {
                    return new[] { this.Entries[index].EventKey };
                }
                List<string> keys = new() { this.Entries[index++].EventKey };
                for (; index < this.Entries.Length; index++)
                {
                    Entry entry = this.Entries[index];
                    if (Match(entry))
                    {
                        keys.Add(entry.EventKey);
                    }
                }
                return keys.Count == 1 ? new[] { keys[0] } : keys.Distinct().ToArray();

                bool Match(Entry entry) => componentName == projection(entry);
            }

            string[] GetKeys()
            {
                if (Find(static entry => entry.Name) is not { Length: > 0 } matches)
                {
                    matches = Find(static entry => entry.Label);
                }
                return matches;
            }
        }
    }
}