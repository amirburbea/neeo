using System;
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
    void RegisterDiscoveredDevice(IDeviceAdapter rootAdapter, string deviceId, IDeviceBuilder builder);
}

internal sealed class DynamicDeviceRegistry : IDynamicDeviceRegistry
{
    private readonly Dictionary<string, IDeviceAdapter> _discoveredDevices = new();
    private readonly ReaderWriterLockSlim _lock = new();
    private readonly ILogger<DynamicDeviceRegistry> _logger;

    public DynamicDeviceRegistry(ILogger<DynamicDeviceRegistry> logger) => this._logger = logger;

    public async ValueTask<IDeviceAdapter?> GetDiscoveredDeviceAsync(IDeviceAdapter rootAdapter, string deviceId, CancellationToken cancellationToken = default)
    {
        if (rootAdapter.GetFeature(ComponentType.Discovery) is not IDiscoveryFeature { EnableDynamicDeviceBuilder: true } feature)
        {
            return default;
        }
        string key = DynamicDeviceRegistry.ComputeKey(rootAdapter.AdapterName, deviceId);
        try
        {
            this._lock.EnterReadLock();
            if (this._discoveredDevices.GetValueOrDefault(key) is { } discoveredDevice)
            {
                return discoveredDevice;
            }
        }
        finally
        {
            this._lock.ExitReadLock();
        }
        if (await feature.DiscoverAsync(deviceId, cancellationToken).ConfigureAwait(false) is not { Length: 1 } devices || devices[0] is not { DeviceBuilder: { } builder })
        {
            return default;
        }
        IDeviceAdapter adapter = builder.BuildAdapter();
        this.RegisterDiscoveredDevice(key, adapter);
        return adapter;
    }

    public void RegisterDiscoveredDevice(IDeviceAdapter rootAdapter, string deviceId, IDeviceBuilder builder)
    {
        if (rootAdapter.GetFeature(ComponentType.Discovery) is not IDiscoveryFeature { EnableDynamicDeviceBuilder: true })
        {
            throw new ArgumentException("Device adapter must support discovery with EnableDynamicDeviceBuilder.", nameof(rootAdapter));
        }
        this.RegisterDiscoveredDevice(
            DynamicDeviceRegistry.ComputeKey(rootAdapter.AdapterName, deviceId),
            builder.BuildAdapter()
        );
    }

    private static string ComputeKey(string rootAdapterName, string deviceId) => $"{rootAdapterName}|{deviceId}";

    private void RegisterDiscoveredDevice(string key, IDeviceAdapter device)
    {
        if (!device.DeviceCapabilities.Contains(DeviceCapability.DynamicDevice))
        {
            throw new ArgumentException("Dynamically defined devices must have DeviceCharacteristic.DynamicDevice", nameof(device));
        }
        int count;
        try
        {
            this._lock.EnterWriteLock();
            this._discoveredDevices.Add(key, device);
            count = this._discoveredDevices.Count;
        }
        finally
        {
            this._lock.ExitWriteLock();
        }
        this._logger.LogInformation("Added device, currently registered {count} dynamic device(s).", count);
    }
}