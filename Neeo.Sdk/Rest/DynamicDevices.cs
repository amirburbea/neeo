using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Neeo.Sdk.Devices;
using Neeo.Sdk.Devices.Features;

namespace Neeo.Sdk.Rest;

public interface IDynamicDevices
{
    ValueTask<IDeviceAdapter?> GetDiscoveredDeviceAsync(IDeviceAdapter rootAdapter, string deviceId);

    void RegisterDiscoveredDevice(string deviceId, IDeviceAdapter adapter);
}

internal sealed class DynamicDevices : IDynamicDevices
{
    private readonly Dictionary<string, IDeviceAdapter> _discoveredDevices = new();
    private readonly ReaderWriterLockSlim _lock = new();
    private readonly ILogger<DynamicDevices> _logger;

    public DynamicDevices(ILogger<DynamicDevices> logger) => this._logger = logger;

    public async ValueTask<IDeviceAdapter?> GetDiscoveredDeviceAsync(IDeviceAdapter adapter, string deviceId)
    {
        try
        {
            this._lock.EnterReadLock();
            if (this._discoveredDevices.GetValueOrDefault(deviceId) is { } discoveredDevice)
            {
                return discoveredDevice;
            }
        }
        finally
        {
            this._lock.ExitReadLock();
        }
        if (adapter.GetFeature(ComponentType.Discovery) is IDiscoveryFeature { EnableDynamicDeviceBuilder: true } feature && 
            await feature.DiscoverAsync(deviceId).ConfigureAwait(false) is { Length: 1 } devices && 
            devices[0].DeviceBuilder is { } builder)
        {
            this.RegisterDiscoveredDevice(deviceId, adapter = builder.BuildAdapter());
            return adapter;
        }
        return default;
    }

    public void RegisterDiscoveredDevice(string deviceId, IDeviceAdapter adapter)
    {
        int count;
        try
        {
            this._lock.EnterWriteLock();
            this._discoveredDevices.Add(deviceId, adapter);
            count = this._discoveredDevices.Count;
        }
        finally
        {
            this._lock.ExitWriteLock();
        }
        this._logger.LogInformation("Added device, currently registered {count} dynamic device(s).", count);
    }
}