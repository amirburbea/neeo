using System;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
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
    /// <remarks>This method is only used to send power notifications.</remarks>
    Task SendNotificationAsync(IDeviceAdapter adapter, Notification notification, CancellationToken cancellationToken = default);

    /// <summary>
    /// Send a notification to the NEEO Brain that a change in a component's associated sensor value
    /// has occurred.
    /// </summary>
    /// <param name="adapter">The adapter for the device with an updated power state.</param>
    /// <param name="notification">The notification to send to the Brain.</param>
    /// <param name="cancellationToken">Token to monitor for cancellation requests.</param>
    /// <returns><see cref="Task"/> to represent the asynchronous operation.</returns>
    Task SendSensorNotificationAsync(IDeviceAdapter adapter, Notification notification, CancellationToken cancellationToken = default);
}

internal sealed class NotificationService : INotificationService
{
    private readonly ConcurrentLru<string, object> _cache = new(Constants.MaxCachedEntries);
    private readonly CancellationTokenSource _cancellationSource = new();
    private readonly Channel<Message> _channel;
    private readonly IApiClient _client;
    private readonly ILogger<NotificationService> _logger;
    private readonly INotificationMapping _notificationMapping;
    private readonly Task[] _processingTasks;

    public NotificationService(IApiClient client, INotificationMapping notificationMapping, ILogger<NotificationService> logger)
    {
        (this._client, this._notificationMapping, this._logger) = (client, notificationMapping, logger);
        this._channel = Channel.CreateBounded<Message>(options: new(Constants.MaxQueuedNotifications) { FullMode = BoundedChannelFullMode.Wait });
        this._processingTasks = new Task[Constants.MaxConcurrentWorkers];
        for (int i = 0; i < Constants.MaxConcurrentWorkers; i++)
        {
            this._processingTasks[i] = Task.Run(ProcessMessagesAsync, this._cancellationSource.Token);
        }

        async Task ProcessMessagesAsync()
        {
            await foreach (Message message in this._channel.Reader.ReadAllAsync(this._cancellationSource.Token).ConfigureAwait(false))
            {
                await this.SendAsync(message).ConfigureAwait(false);
            }
        }
    }

    public void Dispose()
    {
        this._channel.Writer.Complete();
        this._cancellationSource.Cancel();
        try
        {
            Task.WaitAll(this._processingTasks, TimeSpan.FromSeconds(5));
        }
        catch (OperationCanceledException)
        {
            // Expected during shutdown
        }
        finally
        {
            this._cancellationSource.Dispose();
        }
    }

    public Task SendNotificationAsync(IDeviceAdapter adapter, Notification notification, CancellationToken cancellationToken = default) => this.QueueNotificationAsync(
        adapter,
        notification,
        isSensorNotification: false,
        cancellationToken
    );

    public Task SendSensorNotificationAsync(IDeviceAdapter adapter, Notification notification, CancellationToken cancellationToken = default) => this.QueueNotificationAsync(
        adapter,
        notification,
        isSensorNotification: true,
        cancellationToken
    );

    private async Task QueueNotificationAsync(IDeviceAdapter adapter, Notification notification, bool isSensorNotification, CancellationToken cancellationToken)
    {
        (string deviceId, string component, object value) = notification;
        if (deviceId == null || component == null || value == null)
        {
            throw new ArgumentException("Invalid notification data.", nameof(notification));
        }
        if (this._logger.IsEnabled(LogLevel.Information))
        {
            this._logger.LogInformation("Send notification: {Notification}", notification);
        }
        string[] keys = await this._notificationMapping
            .GetNotificationKeysAsync(adapter, deviceId, component, cancellationToken)
            .ConfigureAwait(false);
        foreach (string key in keys)
        {
            await this._channel.Writer.WriteAsync(new(key, value, isSensorNotification), cancellationToken).ConfigureAwait(false);
        }
    }

    private async Task SendAsync(Message message)
    {
        (string key, object data) = message.CacheData;
        if (this._cache.TryGet(key, out object? value) && value.Equals(data))
        {
            // This message is a duplicate of a notification message recently sent.
            return;
        }
        try
        {
            if (await this._client.PostAsync(BrainUrlPaths.Notifications, message, this._cancellationSource.Token).ConfigureAwait(false))
            {
                this._cache.AddOrUpdate(key, data);
            }
        }
        catch (OperationCanceledException)
        {
            // Ignore cancellation during shutdown
        }
        catch (Exception e)
        {
            this._logger.LogError(e, "Failed to send notification.");
        }
    }

    private static class Constants
    {
        public const string DeviceSensorUpdateKey = "DEVICE_SENSOR_UPDATE";
        public const int MaxCachedEntries = 50;
        public const int MaxConcurrentWorkers = 5;
        public const int MaxQueuedNotifications = 100;
    }

    public readonly struct Message(string type, object data, bool isSensorNotification)
    {
        [JsonIgnore]
        public (string, object) CacheData { get; } = (type, data);

        public string Type { get; } = isSensorNotification ? Constants.DeviceSensorUpdateKey : type;

        public object Data { get; } = isSensorNotification ? new SensorData(type, data) : data;

        public readonly record struct SensorData(string SensorEventKey, object SensorValue);
    }
}
