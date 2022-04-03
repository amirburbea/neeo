using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Neeo.Sdk.Notifications;

/// <summary>
/// Interface for the service responsible for sending update notifications to the NEEO Brain.
/// </summary>
public interface INotificationService
{
    /// <summary>
    /// Send a notification to the NEEO Brain that a change in a component value has occurred.
    /// </summary>
    /// <param name="notification">The notification to send to the Brain.</param>
    /// <param name="deviceAdapterName">The adapter name of the device with an updated component value.</param>
    /// <param name="cancellationToken">Token to monitor for cancellation requests.</param>
    /// <returns><see cref="Task"/> to represent the asynchronous operation.</returns>
    /// <remarks>
    /// This method is only used to send power notifications.
    /// </remarks>
    Task SendNotificationAsync(Notification notification, string deviceAdapterName, CancellationToken cancellationToken = default);

    /// <summary>
    /// Send a notification to the NEEO Brain that a change in a component's associated sensor value has occurred.
    /// </summary>
    /// <param name="notification">The notification to send to the Brain.</param>
    /// <param name="deviceAdapterName">The adapter name of the device with an updated component value.</param>
    /// <param name="cancellationToken">Token to monitor for cancellation requests.</param>
    /// <returns><see cref="Task"/> to represent the asynchronous operation.</returns>
    Task SendSensorNotificationAsync(Notification notification, string deviceAdapterName, CancellationToken cancellationToken = default);
}

internal sealed class NotificationService : INotificationService
{
    private readonly Dictionary<string, object> _cache = new();
    private readonly IApiClient _client;
    private readonly ILogger<NotificationService> _logger;
    private readonly INotificationMapping _notificationMapping;
    private int _queueSize;

    public NotificationService(IApiClient client, INotificationMapping notificationMapping, ILogger<NotificationService> logger)
    {
        (this._client, this._notificationMapping, this._logger) = (client, notificationMapping, logger);
    }

    public Task SendNotificationAsync(Notification notification, string deviceAdapterName, CancellationToken cancellationToken)
    {
        return this.SendNotificationAsync(notification, deviceAdapterName, false, cancellationToken);
    }

    public Task SendSensorNotificationAsync(Notification notification, string deviceAdapterName, CancellationToken cancellationToken)
    {
        return this.SendNotificationAsync(notification, deviceAdapterName, true, cancellationToken);
    }

    private static (string, object) ExtractTypeAndData(Message message)
    {
        return message.Data is SensorData { SensorEventKey: { } eventKey, SensorValue: { } value } ? (eventKey, value) : (message.Type, message.Data);
    }

    private void DecreaseQueueSize()
    {
        if (this._queueSize > 0)
        {
            this._queueSize--;
        }
    }

    private bool IsDuplicate(Message message)
    {
        (string key, object data) = NotificationService.ExtractTypeAndData(message);
        return this._cache.TryGetValue(key, out object? value) && value.Equals(data);
    }

    private async Task<bool> SendAsync(Message message, CancellationToken cancellationToken)
    {
        if (this.IsDuplicate(message))
        {
            this._logger.LogWarning("Ignored: Duplicate message.");
            return false;
        }
        if (this._queueSize >= Constants.MaxQueueSize)
        {
            this._logger.LogDebug("Ignored: Max Queue Size Reached.");
            return false;
        }
        this._logger.LogDebug("Sending {message}", message);
        this._queueSize++;
        try
        {
            if (await this._client.PostAsync(UrlPaths.Notifications, message, static (SuccessResponse response) => response.Success, cancellationToken).ConfigureAwait(false))
            {
                this.UpdateCache(message);
                return true;
            }
            this._logger.LogWarning("Failed to send notification - Brain rejected.");
            return false;
        }
        catch (Exception e)
        {
            this._logger.LogError(e, "Failed to send notification.");
            return false;
        }
        finally
        {
            this.DecreaseQueueSize();
        }
    }

    private async Task SendNotificationAsync(Notification notification, string deviceAdapterName, bool isSensorNotification, CancellationToken cancellationToken)
    {
        (string deviceId, string component, object value) = notification;
        if (deviceId is null || component is null || value is null)
        {
            this._logger.LogWarning("Invalid notification:{message}.", notification);
            return;
        }
        this._logger.LogInformation("Send notification:{message}", notification);
        string[] keys = await this._notificationMapping.GetNotificationKeysAsync(deviceAdapterName, deviceId, component, cancellationToken).ConfigureAwait(false);
        await (keys.Length switch
        {
            0 => Task.CompletedTask,
            1 => this.SendAsync(FormatNotification(keys[0]), cancellationToken),
            _ => Task.WhenAll(Array.ConvertAll(keys, key => this.SendAsync(FormatNotification(key), cancellationToken))),
        }).ConfigureAwait(false);

        Message FormatNotification(string notificationKey) => isSensorNotification
            ? new(Constants.DeviceSensorUpdateKey, new SensorData(notificationKey, value))
            : new(notificationKey, value);
    }

    private void UpdateCache(Message message)
    {
        (string key, object data) = NotificationService.ExtractTypeAndData(message);
        if (this._cache.Count >= Constants.MaxCachedEntries)
        {
            this._logger.LogInformation("Clearing notification cache. Cache size exceeded.");
            this._cache.Clear();
        }
        this._cache[key] = data;
    }

    private static class Constants
    {
        public const string DeviceSensorUpdateKey = "DEVICE_SENSOR_UPDATE";
        public const int MaxCachedEntries = 50;
        public const int MaxQueueSize = 20;
    }

    private record struct Message(string Type, object Data);

    private record struct SensorData(string SensorEventKey, object SensorValue);
}