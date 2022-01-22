using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Neeo.Api.Notifications;

public interface INotificationService
{
    Task<bool> SendNotificationAsync(Notification notification, string deviceAdapter, CancellationToken cancellationToken = default);

    Task<bool> SendSensorNotificationAsync(Notification notification, string deviceAdapter, CancellationToken cancellationToken = default);
}

internal sealed class NotificationService : INotificationService
{
    private readonly Dictionary<string, object> _cache = new();
    private readonly IApiClient _client;
    private readonly ILogger<INotificationService> _logger;
    private readonly INotificationMapping _notificationMapping;
    private int _queueSize;

    public NotificationService(IApiClient client, INotificationMapping notificationMapping, ILogger<INotificationService> logger)
    {
        (this._client, this._notificationMapping, this._logger) = (client, notificationMapping, logger);
    }

    public Task<bool> SendNotificationAsync(
        Notification message,
        string deviceId,
        CancellationToken cancellationToken
    ) => this.SendNotificationAsync(message, deviceId, false, cancellationToken);

    public Task<bool> SendSensorNotificationAsync(
        Notification message,
        string deviceId,
        CancellationToken cancellationToken
    ) => this.SendNotificationAsync(message, deviceId, true, cancellationToken);

    private static (string, object) ExtractTypeAndData(Message message) => message.Data is SensorUpdate update
        ? (update.SensorEventKey, update.SensorValue)
        : (message.Type, message.Data);

    private void DecreaseQueueSize()
    {
        if (this._queueSize != 0)
        {
            this._queueSize--;
        }
    }

    private bool IsDuplicate(Message message)
    {
        (string key, object data) = NotificationService.ExtractTypeAndData(message);
        return this._cache.GetValueOrDefault(key) == data;
    }

    private async Task<bool> SendAsync(Message message, CancellationToken cancellationToken)
    {
        if (message.Type == null)
        {
            this._logger.LogWarning("Ignored: Uninitialized message.");
            return false;
        }
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
            bool success = await this._client.PostAsync(UrlPaths.Notifications, message, static (SuccessResult result) => result.Success, cancellationToken).ConfigureAwait(false);
            if (!success)
            {
                this._logger.LogWarning("Failed to send notification - Brain rejected.");
                return false;
            }
            this.UpdateCache(message);
            return true;
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

    private async Task<bool> SendNotificationAsync(Notification notification, string deviceAdapter, bool isSensorNotification, CancellationToken cancellationToken)
    {
        if (notification.UniqueDeviceId is null || notification.Component is null || notification.Value is null)
        {
            this._logger.LogWarning("Invalid notification:{message}.", notification);
            return false;
        }
        this._logger.LogInformation("Send notification:{message}", notification);
        string[] notificationKeys = await this._notificationMapping.GetNotificationKeysAsync(
            deviceAdapter,
            notification.UniqueDeviceId,
            notification.Component,
            cancellationToken
        ).ConfigureAwait(false);
        return notificationKeys.Length switch
        {
            0 => false,
            1 => await this.SendAsync(FormatNotification(notificationKeys[0]), cancellationToken).ConfigureAwait(false),
            _ => Array.TrueForAll(
                await Task.WhenAll(Array.ConvertAll(notificationKeys, key => this.SendAsync(FormatNotification(key), cancellationToken))).ConfigureAwait(false),
                static success => success
            )
        };

        Message FormatNotification(string notificationKey) => isSensorNotification
            ? new(Constants.DeviceSensorUpdateKey, new SensorUpdate(notificationKey, notification.Value))
            : new(notificationKey, notification.Value);
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

    private record struct SensorUpdate(string SensorEventKey, object SensorValue);
}