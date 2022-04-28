using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Neeo.Sdk.Devices;
using Neeo.Sdk.Devices.Features;

namespace Neeo.Sdk.Rest.Controllers;

internal partial class DeviceController
{
    [HttpGet("{adapterName}/subscribe/{deviceId}/{_}")]
    public Task<ActionResult> SubscribeAsync(string adapterName, string deviceId) => this.HandleSubscriptionsAsync(
        adapterName,
        deviceId,
        static feature => feature.OnDeviceAdded,
        nameof(this.SubscribeAsync)
    );

    [HttpGet("{adapterName}/unsubscribe/{deviceId}")]
    public Task<ActionResult> UnsubscribeAsync(string adapterName, string deviceId) => this.HandleSubscriptionsAsync(
        adapterName,
        deviceId,
        static feature => feature.OnDeviceRemoved,
        nameof(this.UnsubscribeAsync)
    );

    private async Task<ActionResult> HandleSubscriptionsAsync(string adapterName, string deviceId, Func<ISubscriptionFeature, DeviceSubscriptionHandler> handlerProjection, string method)
    {
        if (await this._database.GetAdapterAsync(adapterName) is not { } adapter)
        {
            return this.NotFound();
        }
        this._logger.LogInformation("{method} {adapter}:{deviceId}.", method, adapter.DeviceName, deviceId);
        if (adapter.GetFeature(ComponentType.Subscription) is ISubscriptionFeature feature && handlerProjection(feature) is { } subscriptionHandler)
        {
            await subscriptionHandler(deviceId);
        }
        return this.Ok();
    }
}