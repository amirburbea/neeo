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
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns><see cref="Task"/> to indicate completion.</returns>
    Task SendNotificationAsync(string componentName, object value, string deviceId = "default", CancellationToken cancellationToken = default);

    /// <summary>
    /// Sends a notification to the NEEO Brain that the power state
    /// has changed on a device with the given <paramref name="deviceId"/>.
    /// </summary>
    /// <param name="powerState">The power state (where <see langword="true"/> indicates power on).</param>
    /// <param name="deviceId">The unique identifier of the device. Typically, this will be &quot;default&quot; for most (but not all) drivers.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns><see cref="Task"/> to indicate completion.</returns>
    Task SendPowerNotificationAsync(bool powerState, string deviceId = "default", CancellationToken cancellationToken = default);
}

internal sealed class DeviceNotifier : IDeviceNotifier
{
    private readonly string _deviceAdapterName;
    private readonly INotificationService _notificationService;

    public DeviceNotifier(
        INotificationService notificationService,
        string deviceAdapterName,
        bool supportsPowerNotifications
    ) => (this._notificationService, this._deviceAdapterName, this.SupportsPowerNotifications) = (notificationService, deviceAdapterName, supportsPowerNotifications);

    public bool SupportsPowerNotifications { get; }

    public Task SendNotificationAsync(
        string componentName,
        object value,
        string uniqueDeviceId,
        CancellationToken cancellationToken
    ) => this._notificationService.SendSensorNotificationAsync(new(uniqueDeviceId, componentName, value), this._deviceAdapterName, cancellationToken);

    public Task SendPowerNotificationAsync(bool powerState, string uniqueDeviceId, CancellationToken cancellationToken) => !this.SupportsPowerNotifications
        ? throw new NotSupportedException("The device did not register a power state sensor.")
        : this._notificationService.SendNotificationAsync(
            new(uniqueDeviceId, Constants.PowerSensorName, BooleanBoxes.GetBox(powerState)),
            this._deviceAdapterName,
            cancellationToken
        );
}