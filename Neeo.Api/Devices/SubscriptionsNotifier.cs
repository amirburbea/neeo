using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Neeo.Api.Devices.Controllers;

namespace Neeo.Api.Devices;

internal sealed class SubscriptionsNotifier
{
    private readonly IApiClient _client;
    private readonly IDeviceDatabase _database;
    private readonly ILogger<SubscriptionsNotifier> _logger;
    private readonly string _sdkAdapterName;

    public SubscriptionsNotifier(
        IApiClient client,
        IDeviceDatabase database,
        SdkEnvironment environment,
        ILogger<SubscriptionsNotifier> logger
    ) => (this._database, this._client, this._logger, this._sdkAdapterName) = (database, client, logger, environment.SdkAdapterName);

    public Task NotifySubscriptionsAsync(CancellationToken cancellationToken)
    {
        List<Task> tasks = new();
        foreach (IDeviceAdapter adapter in this._database.Adapters)
        {
            if (adapter.GetCapabilityHandler(ComponentType.Subscription) is ISubscriptionController controller)
            {
                tasks.Add(this.NotifySubscriptionsAsync(adapter, controller, cancellationToken));
            }
        }
        return tasks.Count switch
        {
            0 => Task.CompletedTask,
            1 => tasks[0],
            _ => Task.WhenAll(tasks)
        };
    }

    private async Task NotifySubscriptionsAsync(IDeviceAdapter adapter, ISubscriptionController controller, CancellationToken cancellationToken)
    {
        this._logger.LogInformation("Getting current subscriptions for {manufacturer} {device}...", adapter.Manufacturer, adapter.DeviceName);
        string path = string.Format(UrlPaths.SubscriptionsFormat, this._sdkAdapterName, adapter.AdapterName);
        string[] deviceIds = await FetchSubscriptionsAsync(Constants.RetryCount).ConfigureAwait(false);
        await controller.InitializeDeviceList(deviceIds).ConfigureAwait(false);

        async Task<string[]> FetchSubscriptionsAsync(int retryCount)
        {
            try
            {
                return await this._client.GetAsync<string[]>(path, cancellationToken);
            }
            catch (Exception e) when (retryCount > 0)
            {
                this._logger.LogWarning("Failed to get subscriptions ({message}) - retrying in {seconds}s.", e.Message, Constants.RetryDelayMilliseconds / 1000d);
                await Task.Delay(Constants.RetryDelayMilliseconds, cancellationToken).ConfigureAwait(false);
                return await FetchSubscriptionsAsync(retryCount - 1);
            }
            catch (Exception e)
            {
                this._logger.LogError(e, "Failed to get subscriptions.");
                throw;
            }
        }
    }

    private static class Constants
    {
        public const int RetryCount = 2;
        public const int RetryDelayMilliseconds = 2500;
    }
}