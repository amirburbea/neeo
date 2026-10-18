using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Logging;
using Neeo.Sdk.Devices;
using Neeo.Sdk.Devices.Features;

namespace Neeo.Sdk.Rest.Endpoints;

internal static partial class DeviceEndpoints
{
    private static void MapSubscriptions(RouteGroupBuilder group)
    {
        group.MapGet("{adapterName}/subscribe/{deviceId}/{_}", DeviceEndpoints.SubscribeAsync);
        group.MapGet("{adapterName}/unsubscribe/{deviceId}", DeviceEndpoints.UnsubscribeAsync);
    }

    private static async Task<Results<Ok, NotFound>> SubscribeAsync(
        string adapterName,
        string deviceId,
        IDeviceDatabase database,
        ILogger<LogCategory> logger,
        CancellationToken cancellationToken
    )
    {
        if (await database.GetAdapterAsync(adapterName, cancellationToken) is not { } adapter)
        {
            return TypedResults.NotFound();
        }
        logger.LogInformation("Subscribe - {Adapter}:{DeviceId}...", adapter.DeviceName, deviceId);
        if (adapter.GetFeature(ComponentType.Subscription) is ISubscriptionFeature feature)
        {
            await feature.NotifyDeviceAddedAsync(deviceId, cancellationToken);
        }
        return TypedResults.Ok();
    }

    private static async Task<Results<Ok, NotFound>> UnsubscribeAsync(
        string adapterName,
        string deviceId,
        IDeviceDatabase database,
        ILogger<LogCategory> logger,
        CancellationToken cancellationToken
    )
    {
        if (await database.GetAdapterAsync(adapterName, cancellationToken) is not { } adapter)
        {
            return TypedResults.NotFound();
        }
        logger.LogInformation("Unsubscribe - {Adapter}:{DeviceId}...", adapter.DeviceName, deviceId);
        if (adapter.GetFeature(ComponentType.Subscription) is ISubscriptionFeature feature)
        {
            await feature.NotifyDeviceRemovedAsync(deviceId, cancellationToken);
        }
        return TypedResults.Ok();
    }
}
