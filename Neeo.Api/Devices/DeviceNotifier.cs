using System;
using System.Threading;
using System.Threading.Tasks;
using Neeo.Api.Notifications;
using Neeo.Api.Utilities;

namespace Neeo.Api.Devices;

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
    /// 
    /// </summary>
    /// <param name="componentName"></param>
    /// <param name="value"></param>
    /// <param name="uniqueDeviceId"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    Task SendNotificationAsync(string componentName, object value, string uniqueDeviceId = "default", CancellationToken cancellationToken = default);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="powerState"></param>
    /// <param name="uniqueDeviceId"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    Task SendPowerNotificationAsync(bool powerState, string uniqueDeviceId = "default", CancellationToken cancellationToken = default);
}

internal sealed class DeviceNotifier : IDeviceNotifier
{
    private readonly string _deviceAdapterName;
    private readonly INotificationService _notificationService;

    public DeviceNotifier(
        INotificationService notificationService, 
        string deviceAdapterName, 
        bool supportsPowerNotifications
    ) =>  (this._notificationService, this._deviceAdapterName, this.SupportsPowerNotifications) = (notificationService, deviceAdapterName, supportsPowerNotifications);

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