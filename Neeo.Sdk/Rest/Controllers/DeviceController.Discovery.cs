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
    public async Task<ActionResult<DiscoveryResult[]>> DiscoverAsync([ModelBinder(typeof(AdapterBinder))] IDeviceAdapter adapter)
    {
        if (adapter.GetCapabilityHandler(ComponentType.Discovery) is not IDiscoveryController controller)
        {
            throw new NotSupportedException();
        }
        this._logger.LogInformation("Beginning discovery for {adapter}.", adapter.AdapterName);
        return await controller.DiscoverAsync();
    }
}