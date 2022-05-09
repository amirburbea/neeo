﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Neeo.Sdk.Devices;
using Neeo.Sdk.Notifications;
using Xunit;

namespace Neeo.Sdk.Tests.Notifications;

using Message = NotificationService.Message;

public sealed class NotificationServiceTests : IDisposable
{
    private readonly List<Message> _messages = new();
    private readonly Mock<INotificationMapping> _mockNotificationMapping = new(MockBehavior.Strict);
    private readonly NotificationService _notificationService;

    /// <summary>
    /// A task that completes when <see cref="ApiClient.PostAsync"/> is called,
    /// since ActionBlock runs in its own task scheduler.
    /// </summary>
    private readonly Task _postAsyncCompleted;

    public NotificationServiceTests()
    {
        this.SetNotificationKeys(ValueTask.FromResult(new string[] { "key" }));
        Mock<IApiClient> mockClient = new(MockBehavior.Strict);
        TaskCompletionSource tcs = new();
        mockClient
            .Setup(client => client.PostAsync(UrlPaths.Notifications, Capture.In(this._messages), It.IsAny<Func<SuccessResponse, bool>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true)
            .Callback(tcs.SetResult);
        this._postAsyncCompleted = tcs.Task;
        this._notificationService = new(mockClient.Object, this._mockNotificationMapping.Object, NullLogger<NotificationService>.Instance);
    }

    public void Dispose() => this._notificationService.Dispose();

    [Fact]
    public async Task SendNotificationAsync_should_send_correct_message()
    {
        Notification notification = new("deviceId", "component", "value");
        await this._notificationService.SendNotificationAsync(this.CreateDeviceAdapter(), notification, default).ConfigureAwait(false);
        Message message = await this.GetMessageAsync().ConfigureAwait(false);
        Assert.Equal(("key", "value"), message.ExtractTypeAndData());
        Assert.Equal("key", message.Type);
    }

    [Fact]
    public Task SendNotificationAsync_should_throw_if_property_is_null() => Assert.ThrowsAsync<ArgumentException>(() => this._notificationService.SendNotificationAsync(
        this.CreateDeviceAdapter(),
        default,
        default
    ));

    [Fact]
    public async Task SendSensorNotificationAsync_should_send_correct_message()
    {
        Notification notification = new("deviceId", "component", "value");
        await this._notificationService.SendSensorNotificationAsync(this.CreateDeviceAdapter(), notification, default).ConfigureAwait(false);
        Message message = await this.GetMessageAsync().ConfigureAwait(false);
        Assert.Equal(("key", "value"), message.ExtractTypeAndData());
        Assert.Equal("DEVICE_SENSOR_UPDATE", message.Type);
    }

    [Fact]
    public Task SendSensorNotificationAsync_should_throw_if_property_is_null() => Assert.ThrowsAsync<ArgumentException>(() => this._notificationService.SendSensorNotificationAsync(
        this.CreateDeviceAdapter(),
        default,
        default
    ));

    private IDeviceAdapter CreateDeviceAdapter()
    {
        string adapterName = $"adapter{this._messages.Count}";
        Mock<IDeviceAdapter> mockAdapter = new(MockBehavior.Strict);
        mockAdapter.Setup(adapter => adapter.AdapterName).Returns(adapterName);
        mockAdapter.Setup(adapter => adapter.DeviceName).Returns(adapterName);
        return mockAdapter.Object;
    }

    private async Task<Message> GetMessageAsync()
    {
        await this._postAsyncCompleted.ConfigureAwait(false);
        return this._messages.Single();
    }

    private void SetNotificationKeys(ValueTask<string[]> keys) => this._mockNotificationMapping
        .Setup(mapping => mapping.GetNotificationKeysAsync(It.IsAny<IDeviceAdapter>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
        .Returns(keys);
}