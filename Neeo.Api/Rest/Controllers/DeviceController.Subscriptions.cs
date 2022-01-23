using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Neeo.Api.Devices;
using Neeo.Api.Devices.Controllers;

namespace Neeo.Api.Rest.Controllers;

internal partial class DeviceController
{
    [HttpGet("{adapter}/subscribe/{deviceId}/{_}")]
    public async Task<ActionResult<SuccessResult>> SubscribeAsync(
        [ModelBinder(typeof(AdapterBinder))] IDeviceAdapter adapter,
        string deviceId
    )
    {
        if (adapter.GetCapabilityHandler(ComponentType.Subscription) is { Controller: ISubscriptionController controller })
        {
            await controller.SubscribeAsync(deviceId);
        }
        return new SuccessResult();
    }

    [HttpGet("{adapter}/unsubscribe/{deviceId}")]
    public async Task<ActionResult<SuccessResult>> UnsubscribeAsync(
        [ModelBinder(typeof(AdapterBinder))] IDeviceAdapter adapter,
        string deviceId
    )
    {
        if (adapter.GetCapabilityHandler(ComponentType.Subscription) is { Controller: ISubscriptionController controller })
        {
            await controller.SubscribeAsync(deviceId);
        }
        return new SuccessResult();
    }
}