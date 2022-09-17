using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Neeo.Sdk.Devices;
using Neeo.Sdk.Devices.Features;

namespace Neeo.Sdk.Rest.Controllers;

internal partial class DeviceController
{
    [HttpGet("{adapterName}/subscribe/{deviceId}/{_}")]
    public Task<ActionResult> SubscribeAsync(string adapterName, string deviceId, CancellationToken cancellationToken) => this.HandleSubscriptionsAsync(
        adapterName,
        deviceId,
        static feature => feature.OnDeviceAdded,
        nameof(this.SubscribeAsync),
        cancellationToken
    );

    [HttpGet("{adapterName}/unsubscribe/{deviceId}")]
    public Task<ActionResult> UnsubscribeAsync(string adapterName, string deviceId, CancellationToken cancellationToken) => this.HandleSubscriptionsAsync(
        adapterName,
        deviceId,
        static feature => feature.OnDeviceRemoved,
        nameof(this.UnsubscribeAsync),
        cancellationToken
    );

    private async Task<ActionResult> HandleSubscriptionsAsync(string adapterName, string deviceId, Func<ISubscriptionFeature, DeviceSubscriptionHandler> handlerProjection, string methodName, CancellationToken cancellationToken)
    {
        if (await this.GetAdapterAsync(adapterName, cancellationToken) is not { } adapter)
        {
            return this.NotFound();
        }
        this._logger.LogInformation("{method} {adapter}:{deviceId}.", methodName, adapter.DeviceName, deviceId);
        if (adapter.GetFeature(ComponentType.Subscription) is ISubscriptionFeature feature && handlerProjection(feature) is { } subscriptionHandler)
        {
            await subscriptionHandler(deviceId);
        }
        return this.Ok();
    }
}