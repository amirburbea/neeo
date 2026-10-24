using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
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

internal sealed class NotificationService : INotificationService, IDisposable
{
    /// <summary>
    /// Named <see cref="HttpClient"/> from <see cref="IHttpClientFactory"/> used only for notification POSTs
    /// (separate connection pool from general Brain API traffic).
    /// </summary>
    public const string NotificationsHttpClientName = "Neeo.Notifications";

    private static readonly MediaTypeHeaderValue _jsonUtf8 = new("application/json") { CharSet = "utf-8" };

    private readonly ConcurrentLru<string, object> _cache = new(Constants.MaxCachedEntries);
    private readonly CancellationTokenSource _cancellationSource = new();
    private readonly Channel<Message> _channel = Channel.CreateBounded<Message>(
        options: new(Constants.MaxQueuedNotifications) { FullMode = BoundedChannelFullMode.Wait, SingleReader = true }
    );
    private readonly HttpClient _httpClient;
    private readonly ILogger<NotificationService> _logger;
    private readonly Uri _notificationBaseUri;
    private readonly INotificationMapping _notificationMapping;
    private readonly Task _processingTask;

    public NotificationService(
        IHttpClientFactory httpClientFactory,
        IBrain brain,
        INotificationMapping notificationMapping,
        ILogger<NotificationService> logger
    )
    {
        this._httpClient = httpClientFactory.CreateClient(NotificationService.NotificationsHttpClientName);
        this._notificationBaseUri = new($"http://{brain.ServiceEndPoint}");
        (this._notificationMapping, this._logger) = (notificationMapping, logger);
        this._processingTask = Task.Run(ProcessMessagesAsync, this._cancellationSource.Token);

        async Task ProcessMessagesAsync()
        {
            try
            {
                await foreach (Message message in this._channel.Reader.ReadAllAsync(this._cancellationSource.Token).ConfigureAwait(false))
                {
                    await this.SendAsync(message).ConfigureAwait(false);
                }
            }
            catch (OperationCanceledException ex) when (ex.CancellationToken == this._cancellationSource.Token)
            {
                // Expected during shutdown
            }
        }
    }

    public void Dispose()
    {
        this._channel.Writer.Complete();
        this._cancellationSource.Cancel();
        try
        {
            if (!this._processingTask.Wait(TimeSpan.FromSeconds(5)))
            {
                this._logger.LogWarning("Notification worker did not shut down within {TimeoutSeconds}s.", 5);
            }
        }
        catch (AggregateException ex) when (ex.InnerExceptions.All(static e => e is OperationCanceledException))
        {
            return;
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

    private async Task<bool> PostNotificationAsync(Message message, CancellationToken cancellationToken)
    {
        Uri uri = new UriBuilder(this._notificationBaseUri) { Path = BrainUrlPaths.Notifications }.Uri;
        using HttpRequestMessage request = new(HttpMethod.Post, uri)
        {
            Content = JsonContent.Create(message, AppJsonSerializerContext.Default.Message, NotificationService._jsonUtf8),
            Headers =
            {
                Accept = { new("application/json") }
            }
        };
        using HttpResponseMessage response = await this._httpClient
            .SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken)
            .ConfigureAwait(false);
        if (!response.IsSuccessStatusCode)
        {
            string contents = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
            throw new WebException($"Server returned status {(int)response.StatusCode} ({response.StatusCode}). {contents}");
        }
        SuccessResponse result = (await response.Content.ReadFromJsonAsync(AppJsonSerializerContext.Default.SuccessResponse, cancellationToken).ConfigureAwait(false))!;
        return result.Success;
    }

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
            if (!this.ShouldSuppressAsDuplicate(key, value))
            {
                await this._channel.Writer.WriteAsync(new(key, value, isSensorNotification), cancellationToken).ConfigureAwait(false);
            }
        }
    }

    private async Task SendAsync(Message message)
    {
        (string key, object data) = message.CacheData;
        if (this.ShouldSuppressAsDuplicate(key, data))
        {
            // This message is a duplicate of a notification message recently sent.
            return;
        }
        try
        {
            if (await this.PostNotificationAsync(message, this._cancellationSource.Token).ConfigureAwait(false))
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

    private bool ShouldSuppressAsDuplicate(string notificationEventKey, object data)
    {
        return this._cache.TryGet(notificationEventKey, out object? cached) && cached.Equals(data);
    }

    public readonly struct Message(string type, object data, bool isSensorNotification)
    {
        [JsonIgnore]
        public (string, object) CacheData { get; } = (type, data);

        public object Data { get; } = isSensorNotification ? new SensorData(type, data) : data;
        public string Type { get; } = isSensorNotification ? Constants.DeviceSensorUpdateKey : type;
        public readonly record struct SensorData(string SensorEventKey, object SensorValue);
    }

    private static class Constants
    {
        public const string DeviceSensorUpdateKey = "DEVICE_SENSOR_UPDATE";
        public const int MaxCachedEntries = 50;
        public const int MaxQueuedNotifications = 2048;
    }
}
