using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Neeo.Sdk.Devices.Features;
using Neeo.Sdk.Utilities;

namespace Neeo.Sdk.Devices;

/// <summary>
/// A service that upon the start of the integration server, will query for added devices based on the device adapters
/// that support subscriptions and notify them accordingly.
/// </summary>
internal sealed class SubscriptionsNotifier(IApiClient client, IDeviceDatabase database, ISdkEnvironment environment, ILogger<SubscriptionsNotifier> logger) : IHostedService
{
    public Task StartAsync(CancellationToken cancellationToken)
    {
        return Parallel.ForEachAsync(database.Adapters, cancellationToken, NotifySubscriptionsAsync);

        async ValueTask NotifySubscriptionsAsync(IDeviceAdapter adapter, CancellationToken cancellationToken)
        {
            if (adapter.GetFeature(ComponentType.Subscription) is not ISubscriptionFeature feature)
            {
                return;
            }
            logger.LogInformation("Getting current subscriptions for {manufacturer} {device}...", adapter.Manufacturer, adapter.DeviceName);
            string path = string.Format(UrlPaths.SubscriptionsFormat, environment.SdkAdapterName, adapter.AdapterName);
            string[] deviceIds = await client.GetAsync(
                path,
                static (string[] ids) => ids,
                cancellationToken
            ).ConfigureAwait(false);
            await feature.NotifyDeviceListAsync(deviceIds, cancellationToken).ConfigureAwait(false);
        }
    }

    Task IHostedService.StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
