using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Neeo.Sdk.Devices.Features;

namespace Neeo.Sdk.Devices;

internal sealed class SubscriptionsNotifier : IHostedService
{
    private readonly IApiClient _client;
    private readonly IDeviceDatabase _database;
    private readonly ILogger<SubscriptionsNotifier> _logger;
    private readonly string _sdkAdapterName;

    public SubscriptionsNotifier(
        IApiClient client,
        IDeviceDatabase database,
        ISdkEnvironment environment,
        ILogger<SubscriptionsNotifier> logger
    ) => (this._database, this._client, this._logger, this._sdkAdapterName) = (database, client, logger, environment.AdapterName);

    public Task StartAsync(CancellationToken cancellationToken)
    {
        List<Task> tasks = new(
            from adapter in this._database.Adapters
            let feature = adapter.GetFeature(ComponentType.Subscription) as ISubscriptionFeature
            where feature != null
            select this.NotifySubscriptionsAsync(adapter, feature, cancellationToken)
        );
        return tasks.Count switch
        {
            0 => Task.CompletedTask,
            1 => tasks[0],
            _ => Task.WhenAll(tasks)
        };
    }

    Task IHostedService.StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    private async Task NotifySubscriptionsAsync(IDeviceAdapter adapter, ISubscriptionFeature feature, CancellationToken cancellationToken)
    {
        await this._database.InitializeDeviceAsync(adapter).ConfigureAwait(false);
        this._logger.LogInformation("Getting current subscriptions for {manufacturer} {device}...", adapter.Manufacturer, adapter.DeviceName);
        string path = string.Format(UrlPaths.SubscriptionsFormat, this._sdkAdapterName, adapter.AdapterName);
        string[] deviceIds = await this._client.GetAsync<string[]>(path, cancellationToken).ConfigureAwait(false);
        await feature.InitializeDeviceListAsync(deviceIds).ConfigureAwait(false);
    }
}