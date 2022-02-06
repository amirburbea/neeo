using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Neeo.Sdk.Devices;
using Neeo.Sdk.Devices.Features;
using Neeo.Sdk.Devices.Discovery;
using System.Threading;

namespace Neeo.Sdk.Rest.Controllers;

internal partial class DeviceController
{
    [HttpGet("{adapter}/discover")]
    public async Task<ActionResult<DiscoveredDevice[]>> DiscoverAsync([ModelBinder(typeof(AdapterBinder))] IDeviceAdapter adapter, CancellationToken cancellationToken = default)
    {
        if (adapter.GetFeature(ComponentType.Discovery) is not IDiscoveryFeature feature)
        {
            throw new NotSupportedException();
        }
        this._logger.LogInformation("Beginning discovery for {adapter}.", adapter.AdapterName);
        DiscoveredDevice[] devices = await feature.DiscoverAsync(cancellationToken: cancellationToken);
        if (feature.EnableDynamicDeviceBuilder && devices.Length > 1)
        {
            foreach (DiscoveredDevice device in devices)
            {
                if (device.DeviceBuilder is { } builder)
                {
                    this._dynamicDeviceRegistrar.RegisterDiscoveredDevice(device.Id, builder.BuildAdapter());
                }
            }
        }
        return this.Ok(devices);
    }
}