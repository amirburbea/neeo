using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Neeo.Api.Devices;
using Neeo.Api.Devices.Controllers;
using Neeo.Api.Devices.Discovery;

namespace Neeo.Api.Rest.Controllers;

internal partial class DeviceController
{
    [HttpGet("{adapter}/discover")]
    public async Task<ActionResult<DiscoveryResult[]>> DiscoverAsync([ModelBinder(typeof(AdapterBinder))] IDeviceAdapter adapter)
    {
        if (adapter.GetCapabilityHandler(ComponentType.Discovery) is not { Controller: IDiscoveryController controller })
        {
            throw new NotSupportedException();
        }
        this._logger.LogInformation("Beginning discovery for {adapter}.", adapter.AdapterName);
        return await controller.DiscoverAsync();
    }
}