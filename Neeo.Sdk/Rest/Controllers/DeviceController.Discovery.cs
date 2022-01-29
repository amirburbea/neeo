using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Neeo.Sdk.Devices;
using Neeo.Sdk.Devices.Controllers;
using Neeo.Sdk.Devices.Discovery;

namespace Neeo.Sdk.Rest.Controllers;

internal partial class DeviceController
{
    [HttpGet("{adapter}/discover")]
    public async Task<ActionResult<DiscoveredDevice[]>> DiscoverAsync([ModelBinder(typeof(AdapterBinder))] IDeviceAdapter adapter)
    {
        if (adapter.GetFeature(ComponentType.Discovery) is not IDiscoveryFeature controller)
        {
            throw new NotSupportedException();
        }
        this._logger.LogInformation("Beginning discovery for {adapter}.", adapter.AdapterName);
        DiscoveredDevice[] devices =  await controller.DiscoverAsync();
        if (controller.EnableDynamicDeviceBuilder && devices.Length > 1)
        {
            foreach (DiscoveredDevice device in devices)
            {
                if (device.DeviceBuilder is { } builder)
                {
                    this._dynamicDeviceRegistrar.RegisterDiscoveredDevice(device.Id, builder.Build());
                }
            }
        }
        return devices;
    }
}