using System;
using System.Threading;
using System.Threading.Tasks;
using Neeo.Api.Notifications;
using Neeo.Api.Utilities;

namespace Neeo.Api.Devices;

public interface IDeviceNotifier
{
    bool SupportsPowerNotifications { get; }

    Task SendNotificationAsync(Notification message, CancellationToken cancellationToken = default);

    Task SendPowerNotificationAsync(string uniqueDeviceId, bool powerState, CancellationToken cancellationToken = default);
}

internal sealed class DeviceNotifier : IDeviceNotifier
{
    private readonly string _deviceAdapterName;
    private readonly INotificationService _notificationService;

    public DeviceNotifier(string deviceAdapterName, bool supportsPowerNotifications, INotificationService notificationService)
    {
        (this._deviceAdapterName, this._notificationService) = (deviceAdapterName, notificationService);
        this.SupportsPowerNotifications = supportsPowerNotifications;
    }

    public bool SupportsPowerNotifications { get; }

    public Task SendNotificationAsync(
        Notification message,
        CancellationToken cancellationToken
    ) => this._notificationService.SendSensorNotificationAsync(message, this._deviceAdapterName, cancellationToken);

    public Task SendPowerNotificationAsync(string uniqueDeviceId, bool powerState, CancellationToken cancellationToken) => !this.SupportsPowerNotifications
        ? throw new NotSupportedException("The device did not register a power state sensor.")
        : this._notificationService.SendNotificationAsync(
            new(uniqueDeviceId, Constants.PowerSensorName, BooleanBoxes.GetBox(powerState)),
            this._deviceAdapterName,
            cancellationToken
        );
}