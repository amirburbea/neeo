using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Moq.Protected;
using Neeo.Sdk;
using Neeo.Sdk.Devices;
using Neeo.Sdk.Notifications;
using Xunit;

namespace Neeo.Sdk.Tests.Notifications;

public sealed class NotificationServiceTests : IDisposable
{
    private readonly List<byte[]> _capturedBodies = [];

    private readonly HttpClient _httpClient;

    private readonly Mock<INotificationMapping> _mockNotificationMapping = new(MockBehavior.Strict);

    private readonly NotificationService _notificationService;

    private readonly Task _postCompleted;

    public NotificationServiceTests()
    {
        this._mockNotificationMapping
            .Setup(mapping => mapping.GetNotificationKeysAsync(It.IsAny<IDeviceAdapter>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([Constants.NotificationKey]);
        TaskCompletionSource tcs = new();
        Mock<HttpMessageHandler> handlerMock = new(MockBehavior.Strict);
        handlerMock
            .Protected()
            .As<IMessageHandlerMockedMethods>()
            .Setup(handler => handler.Dispose(true));
        handlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .Returns(async (HttpRequestMessage request, CancellationToken cancellationToken) =>
            {
                byte[] bytes = await request.Content!.ReadAsByteArrayAsync(cancellationToken).ConfigureAwait(false);
                lock (this._capturedBodies)
                {
                    this._capturedBodies.Add(bytes);
                }

                tcs.TrySetResult();
                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = JsonContent.Create(new SuccessResponse(true), options: JsonSerializerOptions.Web),
                };
            });
        this._httpClient = new HttpClient(handlerMock.Object);
        Mock<IHttpClientFactory> mockFactory = new(MockBehavior.Strict);
        mockFactory.Setup(f => f.CreateClient(NotificationService.NotificationsHttpClientName)).Returns(this._httpClient);
        Mock<IBrain> mockBrain = new(MockBehavior.Strict);
        mockBrain.Setup(brain => brain.ServiceEndPoint).Returns(new IPEndPoint(IPAddress.Loopback, 3000));
        this._postCompleted = tcs.Task;
        this._notificationService = new(
            mockFactory.Object,
            mockBrain.Object,
            this._mockNotificationMapping.Object,
            NullLogger<NotificationService>.Instance
        );
    }

    private interface IMessageHandlerMockedMethods
    {
        void Dispose(bool disposing);

        Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken);
    }

    public void Dispose()
    {
        this._notificationService.Dispose();
        this._httpClient.Dispose();
    }

    [Fact]
    public async Task SendNotificationAsync_should_send_correct_message()
    {
        await this._notificationService.SendNotificationAsync(this.CreateDeviceAdapter(), 
            new(Constants.DeviceId, Constants.ComponentName, Constants.Value), TestContext.Current.CancellationToken);

        await this._postCompleted;
        using JsonDocument doc = JsonDocument.Parse(this.GetCapturedBody());
        JsonElement root = doc.RootElement;
        Assert.Equal(Constants.NotificationKey, root.GetProperty("type").GetString());
        Assert.Equal(Constants.Value, root.GetProperty("data").GetString());
    }

    [Fact]
    public Task SendNotificationAsync_should_throw_if_property_is_null()
    {
        return Assert.ThrowsAsync<ArgumentException>(() => this._notificationService.SendNotificationAsync(this.CreateDeviceAdapter(), default, TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task SendSensorNotificationAsync_should_send_correct_message()
    {
        await this._notificationService.SendSensorNotificationAsync(this.CreateDeviceAdapter(),
            new(Constants.DeviceId, Constants.ComponentName, Constants.Value), TestContext.Current.CancellationToken);

        await this._postCompleted;
        using JsonDocument doc = JsonDocument.Parse(this.GetCapturedBody());
        JsonElement root = doc.RootElement;
        Assert.Equal("DEVICE_SENSOR_UPDATE", root.GetProperty("type").GetString());
        JsonElement data = root.GetProperty("data");
        Assert.Equal(Constants.NotificationKey, data.GetProperty("sensorEventKey").GetString());
        Assert.Equal(Constants.Value, data.GetProperty("sensorValue").GetString());
    }

    [Fact]
    public Task SendSensorNotificationAsync_should_throw_if_property_is_null()
    {
        return Assert.ThrowsAsync<ArgumentException>(() => this._notificationService.SendSensorNotificationAsync(this.CreateDeviceAdapter(), notification: new(), cancellationToken: TestContext.Current.CancellationToken));
    }

    private IDeviceAdapter CreateDeviceAdapter()
    {
        string adapterName = $"adapter{this._capturedBodies.Count}";
        Mock<IDeviceAdapter> mockAdapter = new(MockBehavior.Strict);
        mockAdapter.Setup(adapter => adapter.AdapterName).Returns(adapterName);
        mockAdapter.Setup(adapter => adapter.DeviceName).Returns(adapterName);
        return mockAdapter.Object;
    }

    private byte[] GetCapturedBody()
    {
        lock (this._capturedBodies)
        {
            return this._capturedBodies.Count == 1
                ? this._capturedBodies[0]
                : throw new InvalidOperationException($"Expected 1 captured body, got {this._capturedBodies.Count}.");
        }
    }

    private static class Constants
    {
        public const string ComponentName = "component";
        public const string DeviceId = "deviceId";
        public const string NotificationKey = "key";
        public const string Value = "value";
    }
}
