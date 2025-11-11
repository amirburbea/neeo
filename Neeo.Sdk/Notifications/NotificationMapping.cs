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
    private static readonly Func<Entry[], Dictionary<string, string[]>> _groupEntriesByName = entries => new(
        from entry in entries
        group entry by entry.Name into grouping
        select KeyValuePair.Create(grouping.Key, grouping.Select(static item => item.EventKey).ToArray())
    );

    private readonly ConcurrentDictionary<string, Dictionary<string, string[]>> _cache = new();

    public async ValueTask<string[]> GetNotificationKeysAsync(IDeviceAdapter adapter, string deviceId, string componentName, CancellationToken cancellationToken)
    {
        string cacheKey = string.Concat(adapter.AdapterName, "|", deviceId);
        if (this._cache.GetValueOrDefault(cacheKey) is not { } keysByName)
        {
            keysByName = this._cache[cacheKey] = await this.FetchNotificationKeysAsync(adapter.AdapterName, deviceId, cancellationToken).ConfigureAwait(false);
        }
        if (keysByName.TryGetValue(componentName, out string[]? keys))
        {
            return keys;
        }
        logger.LogWarning("Component {deviceName} {component} not found.", adapter.DeviceName, componentName);
        this._cache.TryRemove(cacheKey, out _); // Maybe our definition is out of date? Remove it.
        return [];
    }

    private Task<Dictionary<string, string[]>> FetchNotificationKeysAsync(string adapterName, string deviceId, CancellationToken cancellationToken)
    {
        string url = string.Format(BrainUrlPaths.NotificationKeyFormat, environment.SdkAdapterName, adapterName, deviceId);
        return client.GetAsync(url, NotificationMapping._groupEntriesByName, cancellationToken);
    }

    public readonly record struct Entry(string EventKey, string Name);
}
