using System;
using System.Reactive;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Neeo.Sdk.Notifications;
using Neeo.Sdk.Utilities;

namespace Neeo.Sdk.Devices;

/// <summary>
/// An object provided as a parameter to the <see cref="DeviceNotifierCallback"/> which can be used
/// to send notifications to the NEEO Brain about changes to the states/values of the various
/// components defined on the device.
/// </summary>
/// <remarks>
/// Notifications are queued and sent in the background. Failing to reach the Brain is logged, not thrown.
/// </remarks>
public interface IDeviceNotifier
{
    /// <summary>
    /// Sends a notification to the NEEO Brain that the <paramref name="value"/> of a component has
    /// changed on a device with the given <paramref name="deviceId"/>.
    /// </summary>
    /// <param name="componentName">The name of the component on which to register the change.</param>
    /// <param name="value">The updated value.</param>
    /// <param name="deviceId">
    /// The unique identifier of the device. Typically, this will be "default" for most (but not
    /// all) drivers.
    /// </param>
    /// <param name="cancellationToken">Token to monitor for cancellation requests.</param>
    /// <returns><see cref="Task"/> to indicate completion.</returns>
    Task NotifyAsync(string componentName, object value, string deviceId = "default", CancellationToken cancellationToken = default);

    /// <summary>
    /// Sends a notification to the NEEO Brain that the <paramref name="value"/> of a component has
    /// changed on a device with the given <paramref name="deviceId"/>.
    /// <para/>
    /// This overload (though not required to be used) contains a minor performance enhancement for
    /// boolean values.
    /// </summary>
    /// <param name="componentName">The name of the component on which to register the change.</param>
    /// <param name="value">The updated value.</param>
    /// <param name="deviceId">
    /// The unique identifier of the device. Typically, this will be "default" for most (but not
    /// all) drivers.
    /// </param>
    /// <param name="cancellationToken">Token to monitor for cancellation requests.</param>
    /// <returns><see cref="Task"/> to indicate completion.</returns>
    Task NotifyAsync(string componentName, bool value, string deviceId = "default", CancellationToken cancellationToken = default) => this.NotifyAsync(
        componentName,
        BooleanBoxes.GetBox(value),
        deviceId,
        cancellationToken
    );

    /// <summary>
    /// Sends a notification to the NEEO Brain that the power state has changed on a device with the
    /// given <paramref name="deviceId"/>.
    /// </summary>
    /// <param name="powerState">
    /// The power state (where <see langword="true"/> indicates power on).
    /// </param>
    /// <param name="deviceId">
    /// The unique identifier of the device. Typically, this will be "default" for most (but not
    /// all) drivers.
    /// </param>
    /// <param name="cancellationToken">Token to monitor for cancellation requests.</param>
    /// <returns><see cref="Task"/> to indicate completion.</returns>
    Task NotifyPowerStateAsync(bool powerState, string deviceId = "default", CancellationToken cancellationToken = default);

    /// <summary>
    /// Subscribes to an observable stream of component values and forwards notifications to the NEEO Brain
    /// as values change.
    /// </summary>
    /// <typeparam name="TValue">The type of the component value.</typeparam>
    /// <param name="componentName">The name of the component on which to register the change.</param>
    /// <param name="values">The observable stream of values.</param>
    /// <param name="deviceId">
    /// The unique identifier of the device. Typically, this will be "default" for most (but not all) drivers.
    /// </param>
    /// <returns>An <see cref="IDisposable"/> to unsubscribe from the observable stream.</returns>
    IDisposable NotifyOnChange<TValue>(string componentName, IObservable<TValue> values, string deviceId = "default")
        where TValue : notnull;

    /// <summary>
    /// Subscribes to an observable stream of boolean component values and forwards notifications to the NEEO Brain
    /// as values change.
    /// <para/>
    /// This overload contains a performance enhancement for boolean values by avoiding heap allocations for boxing.
    /// </summary>
    /// <param name="componentName">The name of the component on which to register the change.</param>
    /// <param name="values">The observable stream of boolean values.</param>
    /// <param name="deviceId">
    /// The unique identifier of the device. Typically, this will be "default" for most (but not all) drivers.
    /// </param>
    /// <returns>An <see cref="IDisposable"/> to unsubscribe from the observable stream.</returns>
    IDisposable NotifyOnChange(string componentName, IObservable<bool> values, string deviceId = "default") => this.NotifyOnChange(
        componentName,
        values.Select(BooleanBoxes.GetBox),
        deviceId
    );

    /// <summary>
    /// Subscribes to an observable stream of power state changes and forwards notifications to the NEEO Brain.
    /// </summary>
    /// <param name="values">The observable stream of power states (where <see langword="true"/> indicates power on).</param>
    /// <param name="deviceId">
    /// The unique identifier of the device. Typically, this will be "default" for most (but not all) drivers.
    /// </param>
    /// <returns>An <see cref="IDisposable"/> to unsubscribe from the observable stream.</returns>
    IDisposable NotifyPowerStateOnChange(IObservable<bool> values, string deviceId = "default");
}

internal sealed class DeviceNotifier(
    IDeviceAdapter adapter,
    INotificationService notificationService,
    bool supportsPowerNotifications,
    ILogger logger
) : IDeviceNotifier
{
    public Task NotifyAsync(string componentName, object value, string deviceId = "default", CancellationToken cancellationToken = default)
    {
        return notificationService.SendSensorNotificationAsync(
            adapter,
            new(deviceId, componentName, value),
            cancellationToken
        );
    }

    public Task NotifyPowerStateAsync(bool powerState, string deviceId = "default", CancellationToken cancellationToken = default)
    {
        if (!supportsPowerNotifications)
        {
            throw new NotSupportedException("The device did not register a power state sensor.");
        }
        return notificationService.SendNotificationAsync(
            adapter,
            new(deviceId, Constants.PowerSensorName, BooleanBoxes.GetBox(powerState)),
            cancellationToken
        );
    }

    public IDisposable NotifyOnChange<TValue>(string componentName, IObservable<TValue> values, string deviceId = "default")
        where TValue : notnull
    {
        return this.SubscribeCore(
            values,
            (value, cancellationToken) => this.NotifyAsync(componentName, value, deviceId, cancellationToken),
            componentName,
            deviceId
        );
    }

    public IDisposable NotifyPowerStateOnChange(IObservable<bool> values, string deviceId = "default")
    {
        if (!supportsPowerNotifications)
        {
            throw new NotSupportedException("The device did not register a power state sensor.");
        }
        return this.SubscribeCore(
            values,
            (value, cancellationToken) => this.NotifyPowerStateAsync(value, deviceId, cancellationToken),
            Constants.PowerSensorName,
            deviceId
        );
    }

    private IDisposable SubscribeCore<TValue>(
        IObservable<TValue> values,
        Func<TValue, CancellationToken, Task> sendAsync,
        string description,
        string deviceId
    )
    {
        return values
            .DistinctUntilChanged()
            .Select(value => Observable.FromAsync(async cancellationToken =>
            {
                try
                {
                    await sendAsync(value, cancellationToken).ConfigureAwait(false);
                }
                catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
                {
                    // Superseded by a newer value or unsubscribed.
                }
                catch (Exception ex)
                {
                    logger.LogWarning(ex, "Failed to send '{Description}' notification for device '{DeviceId}'.", description, deviceId);
                }
            }))
            .Switch()
            .Catch((Exception ex) =>
            {
                logger.LogError(ex, "'{Description}' notification stream stopped unexpectedly for device '{DeviceId}'.", description, deviceId);
                return Observable.Empty<Unit>();
            })
            .Subscribe();
    }
}
