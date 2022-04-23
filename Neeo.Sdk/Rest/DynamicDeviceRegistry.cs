using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Neeo.Sdk.Devices;
using Neeo.Sdk.Devices.Features;

namespace Neeo.Sdk.Rest;

/// <summary>
/// Describes a class responsible for maintaining a registry of discovered dynamic devices.
/// </summary>
public interface IDynamicDeviceRegistry
{
    ValueTask<IDeviceAdapter?> GetDiscoveredDeviceAsync(IDeviceAdapter rootAdapter, string deviceId);

    void RegisterDiscoveredDevice(IDeviceAdapter rootAdapter, string deviceId, IDeviceBuilder builder);
}

internal sealed class DynamicDeviceRegistry : IDynamicDeviceRegistry
{
    private readonly Dictionary<string, IDeviceAdapter> _discoveredDevices = new();
    private readonly ReaderWriterLockSlim _lock = new();
    private readonly ILogger<DynamicDeviceRegistry> _logger;

    public DynamicDeviceRegistry(ILogger<DynamicDeviceRegistry> logger) => this._logger = logger;

    public async ValueTask<IDeviceAdapter?> GetDiscoveredDeviceAsync(IDeviceAdapter rootAdapter, string deviceId)
    {
        string key = DynamicDeviceRegistry.ComputeKey(rootAdapter.AdapterName, deviceId);
        try
        {
            this._lock.EnterReadLock();
            if (this._discoveredDevices.TryGetValue(key, out IDeviceAdapter? discoveredDevice))
            {
                return discoveredDevice;
            }
        }
        finally
        {
            this._lock.ExitReadLock();
        }
        if (rootAdapter.GetFeature(ComponentType.Discovery) is IDiscoveryFeature { EnableDynamicDeviceBuilder: true } feature &&
            await feature.DiscoverAsync(deviceId).ConfigureAwait(false) is { Length: 1 } devices &&
            devices[0].DeviceBuilder is { } builder)
        {
            this.RegisterDiscoveredDevice(key, rootAdapter = builder.BuildAdapter());
            return rootAdapter;
        }
        return default;
    }

    public void RegisterDiscoveredDevice(IDeviceAdapter rootAdapter, string deviceId, IDeviceBuilder builder) => this.RegisterDiscoveredDevice(
        DynamicDeviceRegistry.ComputeKey(rootAdapter.AdapterName, deviceId),
        builder.BuildAdapter()
    );

    private static string ComputeKey(string rootAdapterName, string deviceId) => $"{rootAdapterName}|{deviceId}";

    private void RegisterDiscoveredDevice(string key, IDeviceAdapter adapter)
    {
        int count;
        try
        {
            this._lock.EnterWriteLock();
            this._discoveredDevices.Add(key, adapter);
            count = this._discoveredDevices.Count;
        }
        finally
        {
            this._lock.ExitWriteLock();
        }
        this._logger.LogInformation("Added device, currently registered {count} dynamic device(s).", count);
    }
}