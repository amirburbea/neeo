using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Neeo.Sdk.Devices.Features;

namespace Neeo.Sdk.Devices;

/// <summary>
/// Describes a class responsible for maintaining a registry of discovered dynamic devices.
/// </summary>
public interface IDynamicDeviceRegistry
{
    /// <summary>
    /// Gets (or attemps to asynchronously discover and add) a discovered dynamic device.
    /// </summary>
    /// <param name="rootAdapter">The root device adapter.</param>
    /// <param name="deviceId">The identifier for the created device.</param>
    /// <param name="cancellationToken">Token to monitor for cancellation requests.</param>
    /// <returns><see cref="ValueTask"/> representing the potentially asynchronous operation.</returns>
    ValueTask<IDeviceAdapter?> GetDiscoveredDeviceAsync(IDeviceAdapter rootAdapter, string deviceId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds a dynamically discovered device to the registry.
    /// </summary>
    /// <param name="rootAdapter">The root device adapter.</param>
    /// <param name="deviceId">The identifier for the created device.</param>
    /// <param name="builder">The dynamic device builder.</param>
    IDeviceAdapter RegisterDiscoveredDevice(IDeviceAdapter rootAdapter, string deviceId, IDeviceBuilder builder);
}

internal sealed class DynamicDeviceRegistry(ILogger<DynamicDeviceRegistry> logger) : IDynamicDeviceRegistry
{
    private readonly ConcurrentDictionary<string, IDeviceAdapter> _discoveredDevices = new();
    private readonly ConcurrentDictionary<string, Lazy<Task<IDeviceAdapter?>>> _pendingDiscoveries = new();

    public ValueTask<IDeviceAdapter?> GetDiscoveredDeviceAsync(IDeviceAdapter rootAdapter, string deviceId, CancellationToken cancellationToken = default)
    {
        if (rootAdapter.GetFeature(ComponentType.Discovery) is not IDiscoveryFeature { EnableDynamicDeviceBuilder: true } feature)
        {
            return default;
        }
        string key = DynamicDeviceRegistry.ComputeKey(rootAdapter.AdapterName, deviceId);
        if (this._discoveredDevices.GetValueOrDefault(key) is { } discoveredDevice)
        {
            return new(discoveredDevice);
        }
        // Coalesce concurrent misses for the same key onto a single in-flight discovery call. The Lazy ensures only the
        // stored entry ever starts a discovery, and the entry is removed only once it has been stored and completed (even
        // when a driver's discovery completes synchronously). The discovery itself is not bound to any one caller's
        // token (a cancelled first caller would otherwise fail every waiter); each caller only stops waiting when its
        // own token is cancelled.
        Task<IDeviceAdapter?> discoveryTask = this._pendingDiscoveries.GetOrAdd(
            key,
            static (key, state) => state.Registry.CreatePendingDiscovery(state.Feature, key, state.DeviceId),
            (Registry: this, Feature: feature, DeviceId: deviceId)
        ).Value;
        return new(discoveryTask.WaitAsync(cancellationToken));
    }

    public IDeviceAdapter RegisterDiscoveredDevice(IDeviceAdapter rootAdapter, string deviceId, IDeviceBuilder builder)
    {
        if (rootAdapter.GetFeature(ComponentType.Discovery) is not IDiscoveryFeature { EnableDynamicDeviceBuilder: true })
        {
            throw new ArgumentException("Device adapter must support discovery with EnableDynamicDeviceBuilder.", nameof(rootAdapter));
        }
        IDeviceAdapter adapter = builder.BuildAdapter();
        this.RegisterDiscoveredDevice(DynamicDeviceRegistry.ComputeKey(rootAdapter.AdapterName, deviceId), adapter);
        return adapter;
    }

    private static string ComputeKey(string rootAdapterName, string deviceId) => $"{rootAdapterName}|{deviceId}";

    private Lazy<Task<IDeviceAdapter?>> CreatePendingDiscovery(IDiscoveryFeature feature, string key, string deviceId)
    {
        Lazy<Task<IDeviceAdapter?>>? pending = null;
        // Only evaluated (via .Value) after GetOrAdd has stored this entry, so the removal in the finally block below
        // always happens after insertion - even when the driver's discovery completes synchronously.
        return pending = new(() => this.DiscoverAndRegisterAsync(feature, key, deviceId, pending!));
    }

    private async Task<IDeviceAdapter?> DiscoverAndRegisterAsync(IDiscoveryFeature feature, string key, string deviceId, Lazy<Task<IDeviceAdapter?>> pending)
    {
        try
        {
            if (await feature.DiscoverAsync(deviceId, CancellationToken.None).ConfigureAwait(false) is not { DeviceBuilder: { } builder })
            {
                return null;
            }
            IDeviceAdapter adapter = builder.BuildAdapter();
            this.RegisterDiscoveredDevice(key, adapter);
            return adapter;
        }
        finally
        {
            this._pendingDiscoveries.TryRemove(KeyValuePair.Create(key, pending));
        }
    }

    private void RegisterDiscoveredDevice(string key, IDeviceAdapter device)
    {
        if (!device.DeviceCapabilities.Contains(DeviceCapability.DynamicDevice))
        {
            throw new ArgumentException("Dynamically defined devices must have DeviceCharacteristic.DynamicDevice", nameof(device));
        }
        this._discoveredDevices.TryAdd(key, device);
        logger.LogInformation("Added device, currently registered {count} dynamic device(s).", this._discoveredDevices.Count);
    }
}
