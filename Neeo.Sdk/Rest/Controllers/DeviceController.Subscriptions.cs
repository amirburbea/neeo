using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Neeo.Sdk.Devices.Components;
using Neeo.Sdk.Devices.Features;

namespace Neeo.Sdk.Rest.Controllers;

internal partial class DeviceController
{
    [HttpGet("{adapterName}/subscribe/{deviceId}/{_}")]
    public Task<ActionResult> SubscribeAsync(string adapterName, string deviceId) => this.HandleSubscriptionActionAsync(
        adapterName,
        deviceId,
        static (feature, deviceId) => feature.OnDeviceAddedAsync(deviceId),
        nameof(this.SubscribeAsync)
    );

    [HttpGet("{adapterName}/unsubscribe/{deviceId}")]
    public Task<ActionResult> UnsubscribeAsync(string adapterName, string deviceId) => this.HandleSubscriptionActionAsync(
        adapterName,
        deviceId,
        static (feature, deviceId) => feature.OnDeviceRemovedAsync(deviceId),
        nameof(this.UnsubscribeAsync)
    );

    private async Task<ActionResult> HandleSubscriptionActionAsync(string adapterName, string deviceId, Func<ISubscriptionFeature, string, Task> featureProjection, string methodName)
    {
        if (await this.GetAdapterAsync(adapterName) is not { } adapter)
        {
            return this.NotFound();
        }
        this._logger.LogInformation("{method} {adapter}:{deviceId}.", methodName, adapter.DeviceName, deviceId);
        if (adapter.GetFeature(ComponentType.Subscription) is ISubscriptionFeature feature)
        {
            await featureProjection(feature, deviceId);
        }
        return this.Ok();
    }
}
