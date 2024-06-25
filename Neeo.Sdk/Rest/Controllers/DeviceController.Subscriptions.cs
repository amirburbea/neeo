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
    public async Task<ActionResult> SubscribeAsync(string adapterName, string deviceId, CancellationToken cancellationToken)
    {
        if (await database.GetAdapterAsync(adapterName, cancellationToken) is not { } adapter)
        {
            return this.NotFound();
        }
        logger.LogInformation("{name} {adapter}:{deviceId}.", nameof(ISubscriptionFeature.NotifyDeviceAddedAsync), adapter.DeviceName, deviceId);
        if (adapter.GetFeature(ComponentType.Subscription) is ISubscriptionFeature feature)
        {
            await feature.NotifyDeviceAddedAsync(deviceId, cancellationToken);
        }
        return this.Ok();
    }

    [HttpGet("{adapterName}/unsubscribe/{deviceId}")]
    public async Task<ActionResult> UnsubscribeAsync(string adapterName, string deviceId, CancellationToken cancellationToken)
    {
        if (await database.GetAdapterAsync(adapterName, cancellationToken) is not { } adapter)
        {
            return this.NotFound();
        }
        logger.LogInformation("{name} {adapter}:{deviceId}.", nameof(ISubscriptionFeature.NotifyDeviceRemovedAsync), adapter.DeviceName, deviceId);
        if (adapter.GetFeature(ComponentType.Subscription) is ISubscriptionFeature feature)
        {
            await feature.NotifyDeviceRemovedAsync(deviceId, cancellationToken);
        }
        return this.Ok();
    }
}
