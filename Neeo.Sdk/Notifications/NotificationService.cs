using System;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using BitFaster.Caching.Lru;
using Microsoft.Extensions.Logging;
using Neeo.Sdk.Devices;
using Neeo.Sdk.Utilities;

namespace Neeo.Sdk.Notifications;

/// <summary>
/// Interface for the service responsible for sending update notifications to the NEEO Brain.
/// </summary>
public interface INotificationService
{
    /// <summary>
    /// Send a notification to the NEEO Brain that a change in a component value has occurred.
    /// </summary>
    /// <param name="adapter">The adapter for the device with an updated component value.</param>
    /// <param name="notification">The notification to send to the Brain.</param>
    /// <param name="cancellationToken">Token to monitor for cancellation requests.</param>
    /// <returns><see cref="Task"/> to represent the asynchronous operation.</returns>
    /// <remarks>
    /// This method is only used to send power notifications.
    /// </remarks>
    Task SendNotificationAsync(IDeviceAdapter adapter, Notification notification, CancellationToken cancellationToken = default);

    /// <summary>
    /// Send a notification to the NEEO Brain that a change in a component's associated sensor value has occurred.
    /// </summary>
    /// <param name="adapter">The adapter for the device with an updated power state.</param>
    /// <param name="notification">The notification to send to the Brain.</param>
    /// <param name="cancellationToken">Token to monitor for cancellation requests.</param>
    /// <returns><see cref="Task"/> to represent the asynchronous operation.</returns>
    Task SendSensorNotificationAsync(IDeviceAdapter adapter, Notification notification, CancellationToken cancellationToken = default);
}

internal sealed class NotificationService : INotificationService, IDisposable
{
    private readonly ActionBlock<Message> _actionBlock;
    private readonly ConcurrentLru<string, object> _cache = new(Constants.MaxCachedEntries);
    private readonly CancellationTokenSource _cancellationSource = new();
    private readonly IApiClient _client;
    private readonly ILogger<NotificationService> _logger;
    private readonly INotificationMapping _notificationMapping;

    public NotificationService(IApiClient client, INotificationMapping notificationMapping, ILogger<NotificationService> logger)
    {
        (this._client, this._notificationMapping, this._logger) = (client, notificationMapping, logger);
        this._actionBlock = new(this.SendAsync, new()
        {
            MaxDegreeOfParallelism = Constants.MaxConcurrency,
            BoundedCapacity = Constants.MaxConcurrency,
            CancellationToken = this._cancellationSource.Token,
        });
    }

    public void Dispose()
    {
        this._cancellationSource.Cancel();
        this._actionBlock.Complete();
    }

    public Task SendNotificationAsync(IDeviceAdapter adapter, Notification notification, CancellationToken cancellationToken = default) => this.SendNotificationAsync(
        adapter,
        notification,
        false,
        cancellationToken
    );

    public Task SendSensorNotificationAsync(IDeviceAdapter adapter, Notification notification, CancellationToken cancellationToken = default) => this.SendNotificationAsync(
        adapter,
        notification,
        true,
        cancellationToken
    );

    private bool IsDuplicate(Message message)
    {
        (string key, object data) = message.ExtractTypeAndData();
        return this._cache.TryGet(key, out object? value) && value.Equals(data);
    }

    private async Task SendAsync(Message message)
    {
        if (this.IsDuplicate(message))
        {
            (string key, object value) = message.ExtractTypeAndData();
            this._logger.LogWarning("Ignored duplicate message: {key}={value}", key, JsonSerializer.Serialize(value, JsonSerialization.Options));
            return;
        }
        this._logger.LogDebug("Sending {message}", message);
        try
        {
            if (await this._client.PostAsync(UrlPaths.Notifications, message, this._cancellationSource.Token).ConfigureAwait(false))
            {
                this.UpdateCache(message);
            }
        }
        catch (Exception e)
        {
            this._logger.LogError(e, "Failed to send notification.");
        }
    }

    private async Task SendNotificationAsync(IDeviceAdapter adapter, Notification notification, bool isSensorNotification, CancellationToken cancellationToken)
    {
        (string deviceId, string component, object value) = notification;
        if (deviceId == null || component == null || value == null)
        {
            throw new ArgumentException("Invalid notification data.", nameof(notification));
        }
        this._logger.LogInformation("Send notification:{notification}", notification);
        if (await this._notificationMapping.GetNotificationKeysAsync(adapter, deviceId, component, cancellationToken).ConfigureAwait(false) is not { Length: > 0 } keys)
        {
            return;
        }
        Parallel.ForEach(keys, new() { CancellationToken = cancellationToken }, notificationKey =>
        {
            Message message = isSensorNotification
                ? Message.SensorUpdate(notificationKey, value)
                : new(notificationKey, value);
            if (!this._actionBlock.Post(message))
            {
                this._logger.LogWarning("Failed to send notification:{message}", message);
            }
        });
    }

    private void UpdateCache(Message message)
    {
        (string key, object data) = message.ExtractTypeAndData();
        this._cache.AddOrUpdate(key, data);
    }

    private static class Constants
    {
        public const string DeviceSensorUpdateKey = "DEVICE_SENSOR_UPDATE";
        public const int MaxCachedEntries = 50;
        public const int MaxConcurrency = 20;
    }

    public readonly record struct Message(string Type, object Data)
    {
        public static Message SensorUpdate(string notificationKey, object value) => new(Constants.DeviceSensorUpdateKey, new SensorData(notificationKey, value));

        public (string, object) ExtractTypeAndData()
        {
            return this.Data is SensorData { SensorEventKey: { } key, SensorValue: { } value }
                ? (key, value)
                : (this.Type, this.Data);
        }

        private sealed record class SensorData(string SensorEventKey, object SensorValue);
    }
}
