﻿using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Neeo.Sdk.Devices;
using Neeo.Sdk.Devices.Features;

namespace Neeo.Sdk.Rest.Controllers;

internal partial class DeviceController
{
    [HttpGet("{adapter}/subscribe/{deviceId}/{_}")]
    public async Task<ActionResult> SubscribeAsync(
        [ModelBinder(typeof(AdapterBinder))] IDeviceAdapter adapter,
        string deviceId // Note that this does NOT use the DeviceIdBinder.
    )
    {
        if (adapter.GetFeature(ComponentType.Subscription) is ISubscriptionFeature feature)
        {
            await feature.SubscribeAsync(deviceId);
        }
        this._logger.LogInformation("Device added {adapter}:{deviceId}.", adapter.AdapterName, deviceId);
        return this.Ok();
    }

    [HttpGet("{adapter}/unsubscribe/{deviceId}")]
    public async Task<ActionResult> UnsubscribeAsync(
        [ModelBinder(typeof(AdapterBinder))] IDeviceAdapter adapter,
        string deviceId // Note that this does NOT use the DeviceIdBinder.
    )
    {
        if (adapter.GetFeature(ComponentType.Subscription) is ISubscriptionFeature feature)
        {
            await feature.UnsubscribeAsync(deviceId);
        }
        this._logger.LogInformation("Device removed {adapter}:{deviceId}.", adapter.AdapterName, deviceId);
        return this.Ok();
    }
}