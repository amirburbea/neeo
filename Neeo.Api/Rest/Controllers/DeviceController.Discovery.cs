using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Neeo.Api.Devices;

namespace Neeo.Api.Rest.Controllers;

partial class DeviceController
{
    [HttpGet("/{adapterName}/discover")]
    public async Task DiscoverAsync(string adapterName)
    {
        //await this.ProcessAdapterName(adapterName);
        //this.GetAdapter().GetCapabilityHandler(ComponentType.Discovery) is not { Controller: IDiscoveryController controller }

    }
}
