using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Neeo.Sdk.Devices.Features;

namespace Neeo.Sdk.Devices;

/// <summary>
/// A service that upon the start of the integration server, will query for added devices based on the device adapters
/// that support subscriptions and notify them accordingly.
/// </summary>
internal sealed class SubscriptionsNotifier : IHostedService
{
    private readonly IApiClient _client;
    private readonly IDeviceDatabase _database;
    private readonly ILogger<SubscriptionsNotifier> _logger;
    private readonly string _sdkAdapterName;

    public SubscriptionsNotifier(IApiClient client, IDeviceDatabase database, ISdkEnvironment environment, ILogger<SubscriptionsNotifier> logger)
    {
        (this._database, this._client, this._logger, this._sdkAdapterName) = (database, client, logger, environment.SdkAdapterName);
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        return Parallel.ForEachAsync(this._database.Adapters, cancellationToken, this.NotifySubscriptionsAsync);
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    private async ValueTask NotifySubscriptionsAsync(IDeviceAdapter adapter, CancellationToken cancellationToken)
    {
        if (adapter.GetFeature(ComponentType.Subscription) is not ISubscriptionFeature feature)
        {
            return;
        }
        this._logger.LogInformation("Getting current subscriptions for {manufacturer} {device}...", adapter.Manufacturer, adapter.DeviceName);
        string path = string.Format(UrlPaths.SubscriptionsFormat, this._sdkAdapterName, adapter.AdapterName);
        await this._client.GetAsync(path, (string[] deviceIds) => feature.InitializeDeviceListAsync(deviceIds), cancellationToken).Unwrap().ConfigureAwait(false);
    }
}