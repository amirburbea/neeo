﻿using System.Threading;
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

    public SubscriptionsNotifier(
        IApiClient client,
        IDeviceDatabase database,
        ISdkEnvironment environment,
        ILogger<SubscriptionsNotifier> logger
    ) => (this._database, this._client, this._logger, this._sdkAdapterName) = (database, client, logger, environment.AdapterName);

    Task IHostedService.StartAsync(CancellationToken cancellationToken) => Parallel.ForEachAsync(
        this._database.GetAdaptersWithSubscription(),
        cancellationToken,
        async (adapterName, cancellationToken) => await this.NotifySubscriptionsAsync(adapterName, cancellationToken).ConfigureAwait(false)
    );

    Task IHostedService.StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    private async Task NotifySubscriptionsAsync(IDeviceAdapter adapter, CancellationToken cancellationToken)
    {
        if (adapter.GetFeature(ComponentType.Subscription) is not ISubscriptionFeature feature)
        {
            return;
        }
        this._logger.LogInformation("Getting current subscriptions for {manufacturer} {device}...", adapter.Manufacturer, adapter.DeviceName);
        string path = string.Format(UrlPaths.SubscriptionsFormat, this._sdkAdapterName, adapter.AdapterName);
        string[] deviceIds = await this._client.GetAsync<string[]>(path, cancellationToken).ConfigureAwait(false);
        await feature.InitializeDeviceListAsync(deviceIds).ConfigureAwait(false);
    }
}