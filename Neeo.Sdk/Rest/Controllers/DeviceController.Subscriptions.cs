using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Neeo.Sdk.Devices;
using Neeo.Sdk.Devices.Features;

namespace Neeo.Sdk.Rest.Controllers;

internal partial class DeviceController
{
    [HttpGet("{adapterName}/subscribe/{deviceId}/{_}")]
    public async Task<ActionResult> SubscribeAsync(string adapterName, string deviceId)
    {
        if (await this._database.GetAdapterAsync(adapterName) is not { } adapter)
        {
            return this.NotFound();
        }
        if (adapter.GetFeature(ComponentType.Subscription) is ISubscriptionFeature feature)
        {
            await feature.NotifyDeviceAddedAsync(deviceId);
        }
        this._logger.LogInformation("Device added {adapter}:{deviceId}.", adapter.AdapterName, deviceId);
        return this.Ok();
    }

    [HttpGet("{adapterName}/unsubscribe/{deviceId}")]
    public async Task<ActionResult> UnsubscribeAsync(string adapterName, string deviceId)
    {
        if (await this._database.GetAdapterAsync(adapterName) is not { } adapter)
        {
            return this.NotFound();
        }
        if (adapter.GetFeature(ComponentType.Subscription) is ISubscriptionFeature feature)
        {
            await feature.NotifyDeviceRemovedAsync(deviceId);
        }
        this._logger.LogInformation("Device removed {adapter}:{deviceId}.", adapter.AdapterName, deviceId);
        return this.Ok();
    }
}