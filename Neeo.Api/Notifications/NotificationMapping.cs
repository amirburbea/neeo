using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Neeo.Api.Notifications;

public interface INotificationMapping
{
    ValueTask<string[]> GetNotificationKeysAsync(string deviceAdapterName, string uniqueDeviceId, string componentName, CancellationToken cancellationToken = default);
}

internal sealed class NotificationMapping : INotificationMapping
{
    private readonly Dictionary<(string, string), EntryCache> _cache = new();
    private readonly IApiClient _client;
    private readonly ILogger<NotificationMapping> _logger;
    private readonly string _sdkAdapterName;

    public NotificationMapping(SdkEnvironment environment, IApiClient client, ILogger<NotificationMapping> logger)
    {
        this._sdkAdapterName = environment.SdkAdapterName;
        this._client = client;
        this._logger = logger;
    }

    public async ValueTask<string[]> GetNotificationKeysAsync(string deviceAdapterName, string uniqueDeviceId, string componentName, CancellationToken cancellationToken)
    {
        (string, string) cacheKey = (deviceAdapterName, uniqueDeviceId);
        if (!this._cache.TryGetValue(cacheKey, out EntryCache? entries))
        {
            this._cache[cacheKey] = entries = new(await this.FetchEntriesAsync(deviceAdapterName, uniqueDeviceId, cancellationToken).ConfigureAwait(false));
        }
        if (entries.GetNotificationKeys(componentName) is { Length: > 0 } keys)
        {
            return keys;
        }
        this._logger.LogInformation("Component {component} not found.", componentName);
        this._cache.Remove(cacheKey);
        return Array.Empty<string>();
    }

    private async Task<Entry[]> FetchEntriesAsync(string deviceAdapterName, string uniqueDeviceId, CancellationToken cancellationToken)
    {
        string path = string.Format(UrlPaths.NotificationKeyFormat, this._sdkAdapterName, deviceAdapterName, uniqueDeviceId);
        Entry[] entries = await this._client.GetAsync<Entry[]>(path, cancellationToken).ConfigureAwait(false);
        return Array.FindAll(entries, static entry => entry.EventKey is not null);
    }

    private record struct Entry(string Name, string EventKey, string? Label);

    private sealed record EntryCache
    {
        private readonly Entry[] _entries;
        private readonly Dictionary<string, string[]> _cache = new();

        public EntryCache(Entry[] entries) => this._entries = entries;

        public string[] GetNotificationKeys(string componentName)
        {
            if (!this._cache.TryGetValue(componentName, out string[]? keys))
            {
                this._cache.Add(
                    componentName, 
                    keys = this.Find(entry => entry.Name == componentName) is { Length: > 0 } matches 
                        ? matches 
                        : this.Find(entry => entry.Label == componentName)
                );
            }
            return keys;
        }

        private string[] Find(Predicate<Entry> predicate)
        {
            int index = Array.FindIndex(this._entries, predicate);
            if (index == -1)
            {
                return Array.Empty<string>();
            }
            HashSet<string> keys = new() { this._entries[index++].EventKey };
            for (; index < this._entries.Length; index++)
            {
                Entry entry = this._entries[index];
                if (predicate(entry))
                {
                    keys.Add(entry.EventKey);
                }
            }
            return keys.ToArray();
        }
    }
}