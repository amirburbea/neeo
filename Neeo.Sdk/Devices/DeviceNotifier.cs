using System;
using System.Threading;
using System.Threading.Tasks;
using Neeo.Sdk.Notifications;
using Neeo.Sdk.Utilities;

namespace Neeo.Sdk.Devices;

/// <summary>
/// An object provided as a parameter to the <see cref="DeviceNotifierCallback"/> which can be used to send notifications to
/// the NEEO Brain about changes to the states/values of the various components defined on the device.
/// </summary>
public interface IDeviceNotifier
{
    /// <summary>
    /// Gets a value indicating if the device supports power notifications.
    /// This value will be <see langword="true"/> for devices where a call was made to <see cref="IDeviceBuilder.AddPowerStateSensor"/> and <see langword="false"/> otherwise.
    /// </summary>
    bool SupportsPowerNotifications { get; }

    /// <summary>
    /// Sends a notification to the NEEO Brain that the <paramref name="value"/> of a component
    /// has changed on a device with the given <paramref name="deviceId"/>.
    /// </summary>
    /// <param name="componentName">The name of the component on which to register the change.</param>
    /// <param name="value">The updated value.</param>
    /// <param name="deviceId">The unique identifier of the device. Typically, this will be &quot;default&quot; for most (but not all) drivers.</param>
    /// <param name="cancellationToken">Token to monitor for cancellation requests.</param>
    /// <returns><see cref="Task"/> to indicate completion.</returns>
    Task SendNotificationAsync(string componentName, object value, string deviceId = "default", CancellationToken cancellationToken = default);

    /// <summary>
    /// Sends a notification to the NEEO Brain that the <paramref name="value"/> of a component
    /// has changed on a device with the given <paramref name="deviceId"/>.
    /// <para />
    /// This overload (though not required to be used) contains a minor performance enhancement for boolean values.
    /// </summary>
    /// <param name="componentName">The name of the component on which to register the change.</param>
    /// <param name="value">The updated value.</param>
    /// <param name="deviceId">The unique identifier of the device. Typically, this will be &quot;default&quot; for most (but not all) drivers.</param>
    /// <param name="cancellationToken">Token to monitor for cancellation requests.</param>
    /// <returns><see cref="Task"/> to indicate completion.</returns>
    Task SendNotificationAsync(string componentName, bool value, string deviceId = "default", CancellationToken cancellationToken = default) => this.SendNotificationAsync(
        componentName,
        BooleanBoxes.GetBox(value),
        deviceId,
        cancellationToken
    );

    /// <summary>
    /// Sends a notification to the NEEO Brain that the power state
    /// has changed on a device with the given <paramref name="deviceId"/>.
    /// </summary>
    /// <param name="powerState">The power state (where <see langword="true"/> indicates power on).</param>
    /// <param name="deviceId">The unique identifier of the device. Typically, this will be &quot;default&quot; for most (but not all) drivers.</param>
    /// <param name="cancellationToken">Token to monitor for cancellation requests.</param>
    /// <returns><see cref="Task"/> to indicate completion.</returns>
    Task SendPowerNotificationAsync(bool powerState, string deviceId = "default", CancellationToken cancellationToken = default);
}

internal sealed class DeviceNotifier : IDeviceNotifier
{
    private readonly IDeviceAdapter _adapter;
    private readonly INotificationService _notificationService;

    public DeviceNotifier(IDeviceAdapter adapter, INotificationService notificationService, bool supportsPowerNotifications)
    {
        (this._adapter, this._notificationService, this.SupportsPowerNotifications) = (adapter, notificationService, supportsPowerNotifications);
    }

    public bool SupportsPowerNotifications { get; }

    public Task SendNotificationAsync(string componentName, object value, string deviceId, CancellationToken cancellationToken)
    {
        return this._notificationService.SendSensorNotificationAsync(
            this._adapter,
            new(deviceId, componentName, value),
            cancellationToken
        );
    }

    public Task SendPowerNotificationAsync(bool powerState, string deviceId, CancellationToken cancellationToken)
    {
        if (!this.SupportsPowerNotifications)
        {
            throw new NotSupportedException("The device did not register a power state sensor.");
        }
        return this._notificationService.SendNotificationAsync(
            this._adapter,
            new(deviceId, Constants.PowerSensorName, BooleanBoxes.GetBox(powerState)),
            cancellationToken
        );
    }
}