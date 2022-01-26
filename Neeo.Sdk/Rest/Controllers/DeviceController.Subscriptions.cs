using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Neeo.Sdk.Devices;
using Neeo.Sdk.Devices.Controllers;

namespace Neeo.Sdk.Rest.Controllers;

internal partial class DeviceController
{
    [HttpGet("{adapter}/subscribe/{deviceId}/{eventPrefix}")]
    public async Task<ActionResult<SuccessResult>> SubscribeAsync(
        [ModelBinder(typeof(AdapterBinder))] IDeviceAdapter adapter,
        string deviceId, // Note that this does NOT use the DeviceIdBinder.
        string eventPrefix
    )
    {
        if (adapter.GetCapabilityHandler(ComponentType.Subscription) is ISubscriptionController controller)
        {
            await controller.SubscribeAsync(deviceId);
        }
        this._logger.LogInformation("Device added {adapter}:{deviceId} ({eventPrefix}).", adapter.AdapterName, deviceId, eventPrefix);
        return new SuccessResult();
    }

    [HttpGet("{adapter}/unsubscribe/{deviceId}")]
    public async Task<ActionResult<SuccessResult>> UnsubscribeAsync(
        [ModelBinder(typeof(AdapterBinder))] IDeviceAdapter adapter,
        string deviceId // Note that this does NOT use the DeviceIdBinder.
    )
    {
        if (adapter.GetCapabilityHandler(ComponentType.Subscription) is ISubscriptionController controller)
        {
            await controller.SubscribeAsync(deviceId);
        }
        this._logger.LogInformation("Device removed {adapter}:{deviceId}.", adapter.AdapterName, deviceId);
        return new SuccessResult();
    }
}