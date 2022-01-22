using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Neeo.Api.Notifications;

public interface INotificationMapping
{
    ValueTask<string[]> GetNotificationKeysAsync(string deviceAdapter, string uniqueDeviceId, string componentName, CancellationToken cancellationToken = default);
}

internal sealed class NotificationMapping : INotificationMapping
{
    private readonly Dictionary<(string,string), Entry[]> _cache = new();
    private readonly IApiClient _client;
    private readonly ILogger<NotificationMapping> _logger;
    private readonly string _pathPrefix;

    public NotificationMapping(ISdkEnvironment environment, IApiClient client, ILogger<NotificationMapping> logger)
    {
        this._pathPrefix = string.Format(UrlPaths.NotificationKeyFormat, environment.SdkAdapterName);
        this._client = client;
        this._logger = logger;
    }

    public async ValueTask<string[]> GetNotificationKeysAsync(string deviceAdapter, string uniqueDeviceId, string componentName, CancellationToken cancellationToken)
    {
        (string, string) cacheKey = (deviceAdapter, uniqueDeviceId);
        if (!this._cache.TryGetValue(cacheKey, out Entry[]? entries))
        {
            this._cache[cacheKey] = entries = await this.FetchEntriesAsync(deviceAdapter, uniqueDeviceId, cancellationToken).ConfigureAwait(false);
        }
        if (NotificationMapping.FindNotificationKeys(entries, componentName) is { Length: > 0 } keys)
        {
            return keys;
        }
        this._logger.LogInformation("Component {component} not found.", componentName);
        this._cache.Remove(cacheKey);
        return Array.Empty<string>();
    }

    private static string[] FindNotificationKeys(Entry[] entries, string componentName)
    {
        return Find(entry => entry.Name == componentName) is { Length: > 0 } matches
            ? matches
            : Find(entry => entry.Label == componentName);

        string[] Find(Predicate<Entry> predicate)
        {
            int index = Array.FindIndex(entries, predicate);
            if (index == -1)
            {
                return Array.Empty<string>();
            }
            if (index == 0 && entries.Length == 1)
            {
                return new[] { entries[0].EventKey };
            }
            List<string> list = new(entries.Length - index) { entries[index].EventKey };
            for (; index < entries.Length; index++)
            {
                Entry entry = entries[index];
                if (predicate(entry))
                {
                    list.Add(entry.EventKey);
                }
            }
            return list.ToArray();
        }
    }

    private async Task<Entry[]> FetchEntriesAsync(string deviceAdapter, string uniqueDeviceId, CancellationToken cancellationToken)
    {
        Entry[] entries = await this._client.GetAsync<Entry[]>($"{this._pathPrefix}/{deviceAdapter}/{uniqueDeviceId}", cancellationToken).ConfigureAwait(false);
        return Array.FindAll(entries, static entry => entry.EventKey is not null);
    }

    private record struct Entry(string Name, string EventKey, string? Label);
}