using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Neeo.Sdk.Devices;
using Neeo.Sdk.Devices.Features;
using Neeo.Sdk.Utilities;

namespace Neeo.Sdk.Rest;

/// <summary>
/// Notifies adapters supporting subscriptions of their subscribed devices.
/// </summary>
internal sealed class SubscriptionsNotifier(
    IApiClient client,
    IDeviceDatabase database,
    ISdkEnvironment environment,
    ILogger<SubscriptionsNotifier> logger
) : IHostedService
{
    public Task StartAsync(CancellationToken cancellationToken)
    {
        return Parallel.ForEachAsync(database.Adapters, cancellationToken, InitializeAsync);

        async ValueTask InitializeAsync(IDeviceAdapter adapter, CancellationToken cancellationToken)
        {
            if (adapter.GetFeature(ComponentType.Subscription) is not ISubscriptionFeature feature)
            {
                return;
            }
            logger.LogInformation("Notifying subscriptions for {Manufacturer} {DeviceName}...", adapter.Manufacturer, adapter.DeviceName);
            string path = string.Format(BrainUrlPaths.SubscriptionsFormat, environment.SdkAdapterName, adapter.AdapterName);
            string[] deviceIds = await client.GetAsync<string[]>(path, cancellationToken).ConfigureAwait(false);
            await feature.NotifyDeviceListAsync(deviceIds, cancellationToken).ConfigureAwait(false);
        }
    }

    Task IHostedService.StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
