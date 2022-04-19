using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Neeo.Sdk.Devices;
using Neeo.Sdk.Devices.Setup;
using Neeo.Sdk.Devices.Features;

namespace Neeo.Sdk.Rest.Controllers;

internal partial class DeviceController
{
    [HttpGet("{adapterName}/discover")]
    public async Task<ActionResult<DiscoveredDevice[]>> DiscoverAsync(string adapterName, CancellationToken cancellationToken = default)
    {
        if (await this._database.GetAdapterAsync(adapterName) is not { } adapter || adapter.GetFeature(ComponentType.Discovery) is not IDiscoveryFeature feature)
        {
            return this.NotFound();
        }
        this._logger.LogInformation("Beginning discovery for {adapter}.", adapter.AdapterName);
        DiscoveredDevice[] devices = await feature.DiscoverAsync(cancellationToken: cancellationToken);
        if (feature.EnableDynamicDeviceBuilder && devices.Length > 1)
        {
            foreach (DiscoveredDevice device in devices)
            {
                if (device.DeviceBuilder is { } builder)
                {
                    string key = this._dynamicDevices.ComputeKey(adapter.AdapterName, device.Id);
                    this._dynamicDevices.RegisterDiscoveredDevice(key, builder.BuildAdapter());
                }
            }
        }
        return devices;
    }
}