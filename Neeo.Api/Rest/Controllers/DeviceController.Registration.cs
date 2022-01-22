using System;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Neeo.Api.Devices;
using Neeo.Api.Devices.Controllers;
using Neeo.Api.Devices.Discovery;

namespace Neeo.Api.Rest.Controllers;

internal partial class DeviceController
{
    [HttpGet("{adapter}/registered")]
    public async Task<ActionResult<IsRegisteredResponse>> QueryIsRegisteredAsync([ModelBinder(typeof(AdapterBinder))] IDeviceAdapter adapter)
    {
        if (adapter.GetCapabilityHandler(ComponentType.Registration) is not { Controller: IRegistrationController controller })
        {
            throw new NotSupportedException();
        }
        this._logger.LogInformation("Querying registration for {adapter}...", adapter.AdapterName);
        return await controller.QueryIsRegisteredAsync();
    }

    [HttpPost("{adapterName}/register")]
    public async Task<ActionResult<SuccessResult>> RegisterAsync([ModelBinder(typeof(AdapterBinder))] IDeviceAdapter adapter, [FromBody] JsonElement credentials)
    {
        if (adapter.GetCapabilityHandler(ComponentType.Registration) is not { Controller: IRegistrationController controller })
        {
            throw new NotSupportedException();
        }
        this._logger.LogInformation("Registering {adapter}...", adapter.AdapterName);
        return await controller.RegisterAsync(credentials);
    }
}