using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Neeo.Api;

public interface IDeviceSubscriptions
{
    Task<string[]> GetSubscriptionsAsync(string deviceAdapterName, CancellationToken cancellationToken = default);
}

internal sealed class DeviceSubscriptions : IDeviceSubscriptions
{
    private readonly IApiClient _client;
    private readonly ILogger<DeviceSubscriptions> _logger;
    private readonly string _sdkAdapterName;

    public DeviceSubscriptions(ISdkEnvironment environment, IApiClient client, ILogger<DeviceSubscriptions> logger)
    {
        this._client = client;
        this._logger = logger;
        this._sdkAdapterName = environment.SdkAdapterName;
    }

    public Task<string[]> GetSubscriptionsAsync(string deviceAdapterName, CancellationToken cancellationToken)
    {
        string path = string.Format(UrlPaths.SubscriptionsFormat, this._sdkAdapterName, deviceAdapterName);
        return GetSubscriptionsAsync(Constants.RetryCount);

        async Task<string[]> GetSubscriptionsAsync(int retryCount)
        {
            try
            {
                return await this._client.GetAsync<string[]>(path, cancellationToken);
            }
            catch (Exception e) when (retryCount > 0)
            {
                this._logger.LogWarning("Failed to get subscriptions ({message}) - retrying in {seconds}s.", e.Message, Constants.RetryDelayMilliseconds / 1000d);
                await Task.Delay(Constants.RetryDelayMilliseconds, cancellationToken).ConfigureAwait(false);
                return await GetSubscriptionsAsync(retryCount - 1);
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