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
        static (feature, deviceId) => feature.NotifyDeviceAddedAsync(deviceId),
        nameof(ISubscriptionFeature.NotifyDeviceAddedAsync)
    );

    [HttpGet("{adapterName}/unsubscribe/{deviceId}")]
    public Task<ActionResult> UnsubscribeAsync(string adapterName, string deviceId) => this.HandleSubscriptionsAsync(
        adapterName,
        deviceId,
        static (feature, deviceId) => feature.NotifyDeviceRemovedAsync(deviceId),
        nameof(ISubscriptionFeature.NotifyDeviceRemovedAsync)
    );

    private async Task<ActionResult> HandleSubscriptionsAsync(string adapterName, string deviceId, Func<ISubscriptionFeature, string, Task> notifyAsync, string method)
    {
        this._logger.LogInformation("{method} {adapter}:{deviceId}.", method, adapterName, deviceId);
        if (await this._database.GetAdapterAsync(adapterName) is not { } adapter)
        {
            return this.NotFound();
        }
        if (adapter.GetFeature(ComponentType.Subscription) is ISubscriptionFeature feature)
        {
            await notifyAsync(feature, deviceId);
        }
        return this.Ok();
    }
}