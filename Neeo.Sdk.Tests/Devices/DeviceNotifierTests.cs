using System;
using System.Collections.Generic;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Neeo.Sdk.Devices;
using Neeo.Sdk.Notifications;
using Neeo.Sdk.Utilities;
using Xunit;

namespace Neeo.Sdk.Tests.Devices;

public sealed class DeviceNotifierTests
{
    private readonly Mock<IDeviceAdapter> _adapter = new();
    private readonly Mock<INotificationService> _notificationService = new();

    private IDeviceNotifier CreateNotifier(bool supportsPowerNotifications = true, ILogger? logger = null) =>
        new DeviceNotifier(this._adapter.Object, this._notificationService.Object, supportsPowerNotifications, logger ?? NullLogger.Instance);

    [Fact]
    public async Task NotifyAsync_should_call_notification_service()
    {
        IDeviceNotifier notifier = this.CreateNotifier();

        await notifier.NotifyAsync("my-component", "my-value", "my-device", TestContext.Current.CancellationToken);

        this._notificationService.Verify(
            s => s.SendSensorNotificationAsync(
                this._adapter.Object,
                new Notification("my-device", "my-component", "my-value"),
                It.IsAny<CancellationToken>()
            ),
            Times.Once
        );
    }

    [Fact]
    public async Task NotifyAsync_bool_should_use_boolean_boxes()
    {
        IDeviceNotifier notifier = this.CreateNotifier();

        await notifier.NotifyAsync("my-component", true, "my-device", TestContext.Current.CancellationToken);

        this._notificationService.Verify(
            s => s.SendSensorNotificationAsync(
                this._adapter.Object,
                It.Is<Notification>(n => n.DeviceId == "my-device" && n.Component == "my-component" && ReferenceEquals(n.Value, BooleanBoxes.True)),
                It.IsAny<CancellationToken>()
            ),
            Times.Once
        );
    }

    [Fact]
    public async Task NotifyPowerStateAsync_should_throw_if_power_not_supported()
    {
        IDeviceNotifier notifier = this.CreateNotifier(supportsPowerNotifications: false);

        await Assert.ThrowsAsync<NotSupportedException>(() => notifier.NotifyPowerStateAsync(true, cancellationToken: TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task NotifyPowerStateAsync_should_call_notification_service_with_power_sensor()
    {
        IDeviceNotifier notifier = this.CreateNotifier();

        await notifier.NotifyPowerStateAsync(true, "my-device", TestContext.Current.CancellationToken);

        this._notificationService.Verify(
            s => s.SendNotificationAsync(
                this._adapter.Object,
                It.Is<Notification>(n => n.DeviceId == "my-device" && n.Component == Constants.PowerSensorName && ReferenceEquals(n.Value, BooleanBoxes.True)),
                It.IsAny<CancellationToken>()
            ),
            Times.Once
        );
    }

    [Fact]
    public async Task NotifyOnChange_should_forward_notifications_as_values_change()
    {
        IDeviceNotifier notifier = this.CreateNotifier();
        using Subject<string> subject = new();
        TaskCompletionSource firstNotification = new();
        TaskCompletionSource secondNotification = new();

        this._notificationService
            .Setup(s => s.SendSensorNotificationAsync(this._adapter.Object, It.IsAny<Notification>(), It.IsAny<CancellationToken>()))
            .Returns<IDeviceAdapter, Notification, CancellationToken>((_, n, _) =>
            {
                if ((string)n.Value == "value-1")
                {
                    firstNotification.TrySetResult();
                }
                else if ((string)n.Value == "value-2")
                {
                    secondNotification.TrySetResult();
                }
                return Task.CompletedTask;
            });

        using IDisposable subscription = notifier.NotifyOnChange("component", subject, "my-device");

        subject.OnNext("value-1");
        await firstNotification.Task.WaitAsync(TimeSpan.FromSeconds(2), TestContext.Current.CancellationToken);

        subject.OnNext("value-2");
        await secondNotification.Task.WaitAsync(TimeSpan.FromSeconds(2), TestContext.Current.CancellationToken);

        this._notificationService.Verify(
            s => s.SendSensorNotificationAsync(
                this._adapter.Object,
                new Notification("my-device", "component", "value-1"),
                It.IsAny<CancellationToken>()
            ),
            Times.Once
        );
        this._notificationService.Verify(
            s => s.SendSensorNotificationAsync(
                this._adapter.Object,
                new Notification("my-device", "component", "value-2"),
                It.IsAny<CancellationToken>()
            ),
            Times.Once
        );
    }

    [Fact]
    public async Task NotifyOnChange_should_ignore_consecutive_duplicates()
    {
        IDeviceNotifier notifier = this.CreateNotifier();
        using Subject<string> subject = new();
        List<string> recordedValues = [];
        TaskCompletionSource thirdNotification = new();

        this._notificationService
            .Setup(s => s.SendSensorNotificationAsync(this._adapter.Object, It.IsAny<Notification>(), It.IsAny<CancellationToken>()))
            .Returns<IDeviceAdapter, Notification, CancellationToken>((_, n, _) =>
            {
                recordedValues.Add((string)n.Value);
                if (recordedValues.Count == 3)
                {
                    thirdNotification.TrySetResult();
                }
                return Task.CompletedTask;
            });

        using IDisposable subscription = notifier.NotifyOnChange("component", subject);

        subject.OnNext("A");
        subject.OnNext("A");
        subject.OnNext("B");
        subject.OnNext("B");
        subject.OnNext("A");

        await thirdNotification.Task.WaitAsync(TimeSpan.FromSeconds(2), TestContext.Current.CancellationToken);

        Assert.Equal(["A", "B", "A"], recordedValues);
    }

    [Fact]
    public async Task NotifyOnChange_bool_should_use_boolean_boxes_without_boxing()
    {
        IDeviceNotifier notifier = this.CreateNotifier();
        using Subject<bool> subject = new();
        TaskCompletionSource notificationReceived = new();
        object? boxedValue = null;

        this._notificationService
            .Setup(s => s.SendSensorNotificationAsync(this._adapter.Object, It.IsAny<Notification>(), It.IsAny<CancellationToken>()))
            .Returns<IDeviceAdapter, Notification, CancellationToken>((_, n, _) =>
            {
                boxedValue = n.Value;
                notificationReceived.TrySetResult();
                return Task.CompletedTask;
            });

        using IDisposable subscription = notifier.NotifyOnChange("component", subject);

        subject.OnNext(true);
        await notificationReceived.Task.WaitAsync(TimeSpan.FromSeconds(2), TestContext.Current.CancellationToken);

        Assert.Same(BooleanBoxes.True, boxedValue);
    }

    [Fact]
    public async Task NotifyOnChange_should_cancel_inflight_when_newer_value_emitted()
    {
        IDeviceNotifier notifier = this.CreateNotifier();
        using Subject<int> subject = new();
        TaskCompletionSource sendStarted = new();
        TaskCompletionSource<bool> firstSendCancelled = new();
        TaskCompletionSource secondSendCompleted = new();

        this._notificationService
            .Setup(s => s.SendSensorNotificationAsync(this._adapter.Object, It.Is<Notification>(n => (int)n.Value == 1), It.IsAny<CancellationToken>()))
            .Returns<IDeviceAdapter, Notification, CancellationToken>(async (_, _, token) =>
            {
                sendStarted.TrySetResult();
                try
                {
                    await Task.Delay(Timeout.Infinite, token);
                }
                catch (OperationCanceledException)
                {
                    firstSendCancelled.TrySetResult(true);
                    throw;
                }
            });

        this._notificationService
            .Setup(s => s.SendSensorNotificationAsync(this._adapter.Object, It.Is<Notification>(n => (int)n.Value == 2), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask)
            .Callback(secondSendCompleted.SetResult);

        using IDisposable subscription = notifier.NotifyOnChange("component", subject);

        subject.OnNext(1);
        await sendStarted.Task.WaitAsync(TimeSpan.FromSeconds(2), TestContext.Current.CancellationToken);

        subject.OnNext(2);
        await secondSendCompleted.Task.WaitAsync(TimeSpan.FromSeconds(2), TestContext.Current.CancellationToken);

        bool wasCancelled = await firstSendCancelled.Task.WaitAsync(TimeSpan.FromSeconds(2), TestContext.Current.CancellationToken);
        Assert.True(wasCancelled);
    }

    [Fact]
    public async Task NotifyOnChange_should_continue_if_single_send_fails()
    {
        IDeviceNotifier notifier = this.CreateNotifier();
        using Subject<int> subject = new();
        TaskCompletionSource secondCompleted = new();

        this._notificationService
            .Setup(s => s.SendSensorNotificationAsync(this._adapter.Object, It.Is<Notification>(n => (int)n.Value == 1), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Simulated error"));

        this._notificationService
            .Setup(s => s.SendSensorNotificationAsync(this._adapter.Object, It.Is<Notification>(n => (int)n.Value == 2), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask)
            .Callback(secondCompleted.SetResult);

        using IDisposable subscription = notifier.NotifyOnChange("component", subject);

        subject.OnNext(1);
        // Give time for first send to fail
        await Task.Delay(50, TestContext.Current.CancellationToken);

        subject.OnNext(2);
        await secondCompleted.Task.WaitAsync(TimeSpan.FromSeconds(2), TestContext.Current.CancellationToken);

        this._notificationService.Verify(
            s => s.SendSensorNotificationAsync(this._adapter.Object, It.Is<Notification>(n => (int)n.Value == 2), It.IsAny<CancellationToken>()),
            Times.Once
        );
    }

    [Fact]
    public async Task NotifyOnChange_disposing_stops_further_notifications()
    {
        IDeviceNotifier notifier = this.CreateNotifier();
        using Subject<string> subject = new();
        TaskCompletionSource firstCompleted = new();

        this._notificationService
            .Setup(s => s.SendSensorNotificationAsync(this._adapter.Object, It.IsAny<Notification>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask)
            .Callback(firstCompleted.SetResult);

        IDisposable subscription = notifier.NotifyOnChange("component", subject);

        subject.OnNext("initial");
        await firstCompleted.Task.WaitAsync(TimeSpan.FromSeconds(2), TestContext.Current.CancellationToken);

        subscription.Dispose();

        subject.OnNext("after-dispose");
        await Task.Delay(50, TestContext.Current.CancellationToken);

        this._notificationService.Verify(
            s => s.SendSensorNotificationAsync(this._adapter.Object, It.Is<Notification>(n => (string)n.Value == "after-dispose"), It.IsAny<CancellationToken>()),
            Times.Never
        );
    }

    [Fact]
    public void NotifyOnChange_should_catch_and_log_error_if_stream_fails()
    {
        Mock<ILogger> mockLogger = new();
        IDeviceNotifier notifier = this.CreateNotifier(logger: mockLogger.Object);
        using Subject<string> subject = new();

        using IDisposable subscription = notifier.NotifyOnChange("component", subject, "my-device");

        Exception expectedException = new("Stream error");
        subject.OnError(expectedException);

        mockLogger.Verify(
            l => l.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => true),
                expectedException,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()
            ),
            Times.Once
        );
    }

    [Fact]
    public void NotifyPowerStateOnChange_should_throw_if_power_not_supported()
    {
        IDeviceNotifier notifier = this.CreateNotifier(supportsPowerNotifications: false);

        Assert.Throws<NotSupportedException>(() => notifier.NotifyPowerStateOnChange(Observable.Empty<bool>()));
    }

    [Fact]
    public async Task NotifyPowerStateOnChange_should_forward_power_notifications()
    {
        IDeviceNotifier notifier = this.CreateNotifier();
        using Subject<bool> subject = new();
        TaskCompletionSource firstCompleted = new();
        TaskCompletionSource secondCompleted = new();

        this._notificationService
            .Setup(s => s.SendNotificationAsync(this._adapter.Object, It.IsAny<Notification>(), It.IsAny<CancellationToken>()))
            .Returns<IDeviceAdapter, Notification, CancellationToken>((_, n, _) =>
            {
                if (ReferenceEquals(n.Value, BooleanBoxes.True))
                {
                    firstCompleted.TrySetResult();
                }
                else
                {
                    secondCompleted.TrySetResult();
                }
                return Task.CompletedTask;
            });

        using IDisposable subscription = notifier.NotifyPowerStateOnChange(subject, "my-device");

        subject.OnNext(true);
        await firstCompleted.Task.WaitAsync(TimeSpan.FromSeconds(2), TestContext.Current.CancellationToken);

        subject.OnNext(false);
        await secondCompleted.Task.WaitAsync(TimeSpan.FromSeconds(2), TestContext.Current.CancellationToken);

        this._notificationService.Verify(
            s => s.SendNotificationAsync(
                this._adapter.Object,
                It.Is<Notification>(n => n.DeviceId == "my-device" && n.Component == Constants.PowerSensorName && ReferenceEquals(n.Value, BooleanBoxes.True)),
                It.IsAny<CancellationToken>()
            ),
            Times.Once
        );

        this._notificationService.Verify(
            s => s.SendNotificationAsync(
                this._adapter.Object,
                It.Is<Notification>(n => n.DeviceId == "my-device" && n.Component == Constants.PowerSensorName && ReferenceEquals(n.Value, BooleanBoxes.False)),
                It.IsAny<CancellationToken>()
            ),
            Times.Once
        );
    }
}
