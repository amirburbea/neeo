using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Neeo.Api.Devices;
using Neeo.Api.Devices.Controllers;

namespace Neeo.Api.Rest.Controllers;

internal partial class DeviceController
{
    [HttpGet("{adapter}/{componentName}/{deviceId}")]
    public async Task<ActionResult<object>> GetValueAsync(
        [ModelBinder(typeof(AdapterBinder))] IDeviceAdapter adapter,
        [ModelBinder(typeof(ComponentNameBinder))] string componentName,
        string deviceId
    )
    {
        if (this.HttpContext.Items[nameof(ICapabilityHandler)] is not ICapabilityHandler { ComponentType: var type, Controller: var controller })
        {
            throw new();
        }
        return controller switch
        {
            IValueController valueController => await valueController.GetValueAsync(deviceId),
            IButtonController buttonController => await buttonController.TriggerAsync(deviceId),
            _ => throw new("Type " + type + " not expected.")
        };
    }
}