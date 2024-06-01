using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Neeo.Sdk.Devices;
using Neeo.Sdk.Notifications;
using Neeo.Sdk.Utilities;
using Xunit;

namespace Neeo.Sdk.Tests.Notifications;

using Message = NotificationService.Message;

public sealed class NotificationServiceTests : IDisposable
{
    private readonly List<Message> _messages = [];
    private readonly Mock<INotificationMapping> _mockNotificationMapping = new(MockBehavior.Strict);
    private readonly NotificationService _notificationService;

    /// <summary>
    /// A task that completes when <see cref="ApiClient.PostAsync"/> is called,
    /// since <see cref="ActionBlock{T}" /> runs in its own task scheduler.
    /// </summary>
    private readonly Task _postAsyncCompleted;

    public NotificationServiceTests()
    {
        this.SetNotificationKeys(ValueTask.FromResult(new[] { Constants.NotificationKey }));
        Mock<IApiClient> mockClient = new(MockBehavior.Strict);
        TaskCompletionSource tcs = new();
        mockClient
            .Setup(client => client.PostAsync(UrlPaths.Notifications, Capture.In(this._messages), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true)
            .Callback(tcs.SetResult);
        this._postAsyncCompleted = tcs.Task;
        this._notificationService = new(mockClient.Object, this._mockNotificationMapping.Object, NullLogger<NotificationService>.Instance);
    }

    public void Dispose() => this._notificationService.Dispose();

    [Fact]
    public async Task SendNotificationAsync_should_send_correct_message()
    {
        await this._notificationService.SendNotificationAsync(
            this.CreateDeviceAdapter(),
            new(Constants.DeviceId, Constants.ComponentName, Constants.Value)
        );

        Message message = await this.GetMessageAsync();
        Assert.Equal((Constants.NotificationKey, Constants.Value), message.ExtractTypeAndData());
        Assert.Equal(Constants.NotificationKey, message.Type);
    }

    [Fact]
    public Task SendNotificationAsync_should_throw_if_property_is_null() => Assert.ThrowsAsync<ArgumentException>(() => this._notificationService.SendNotificationAsync(
                this.CreateDeviceAdapter(),
                default
            ));

    [Fact]
    public async Task SendSensorNotificationAsync_should_send_correct_message()
    {
        await this._notificationService.SendSensorNotificationAsync(
            this.CreateDeviceAdapter(),
            new(Constants.DeviceId, Constants.ComponentName, Constants.Value)
        );

        Message message = await this.GetMessageAsync();
        Assert.Equal((Constants.NotificationKey, Constants.Value), message.ExtractTypeAndData());
        Assert.Equal("DEVICE_SENSOR_UPDATE", message.Type);
    }

    [Fact]
    public Task SendSensorNotificationAsync_should_throw_if_property_is_null() => Assert.ThrowsAsync<ArgumentException>(() => this._notificationService.SendSensorNotificationAsync(
                        this.CreateDeviceAdapter(),
                        notification: new()
                    ));

    private IDeviceAdapter CreateDeviceAdapter()
    {
        string adapterName = $"adapter{this._messages.Count}";
        Mock<IDeviceAdapter> mockAdapter = new(MockBehavior.Strict);
        mockAdapter.Setup(adapter => adapter.AdapterName).Returns(adapterName);
        mockAdapter.Setup(adapter => adapter.DeviceName).Returns(adapterName);
        return mockAdapter.Object;
    }

    private Task<Message> GetMessageAsync() => this._postAsyncCompleted.ContinueWith(_ => this._messages.Single(), TaskContinuationOptions.ExecuteSynchronously);

    private void SetNotificationKeys(ValueTask<string[]> keys) => this._mockNotificationMapping
        .Setup(mapping => mapping.GetNotificationKeysAsync(It.IsAny<IDeviceAdapter>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
        .Returns(keys);

    private static class Constants
    {
        public const string ComponentName = "component";
        public const string DeviceId = "deviceId";
        public const string NotificationKey = "key";
        public const string Value = "value";
    }
}
