using System;
using System.Collections.Generic;
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
        List<Task> tasks = new();
        foreach (IDeviceAdapter adapter in this._database.Adapters)
        {
            if (adapter.GetFeature(ComponentType.Subscription) is ISubscriptionFeature feature)
            {
                tasks.Add(this.NotifySubscriptionsAsync(adapter.AdapterName, feature, cancellationToken));
            }
        }
        return tasks.Count switch
        {
            0 => Task.CompletedTask,
            1 => tasks[0],
            _ => Task.WhenAll(tasks)
        };
    }

    Task IHostedService.StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    private async Task NotifySubscriptionsAsync(string adapterName, ISubscriptionFeature feature, CancellationToken cancellationToken)
    {
        // Ensure device driver is initialized.
        IDeviceAdapter adapter = await this._database.GetAdapterAsync(adapterName).ConfigureAwait(false);
        this._logger.LogInformation("Getting current subscriptions for {manufacturer} {device}...", adapter.Manufacturer, adapter.DeviceName);
        string path = string.Format(UrlPaths.SubscriptionsFormat, this._sdkAdapterName, adapter.AdapterName);
        TaskCompletionSource<string[]> taskCompletionSource = new();
        await FetchSubscriptionsAsync().ConfigureAwait(false);
        string[] deviceIds = await taskCompletionSource.Task.ConfigureAwait(false);
        await feature.InitializeDeviceList(deviceIds).ConfigureAwait(false);

        async Task FetchSubscriptionsAsync()
        {
            for (int i = 0; !cancellationToken.IsCancellationRequested && !taskCompletionSource.Task.IsCompleted && i <= Constants.MaxRetries; i++)
            {
                try
                {
                    taskCompletionSource.TrySetResult(await this._client.GetAsync<string[]>(path, cancellationToken).ConfigureAwait(false));
                }
                catch (Exception e) when (i == Constants.MaxRetries)
                {
                    this._logger.LogError(e, "Failed to get subscriptions.");
                    taskCompletionSource.TrySetException(e);
                }
                catch (Exception e)
                {
                    this._logger.LogWarning("Failed to get subscriptions ({message}) - retrying in {seconds}s.", e.Message, Constants.RetryDelayMilliseconds / 1000d);
                    await Task.Delay(Constants.RetryDelayMilliseconds, cancellationToken).ConfigureAwait(false);
                }
            }
        }
    }

    private static class Constants
    {
        public const int MaxRetries = 2;
        public const int RetryDelayMilliseconds = 2500;
    }
}