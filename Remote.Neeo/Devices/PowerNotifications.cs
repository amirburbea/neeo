namespace Remote.Neeo.Devices;

public sealed class PowerNotifications
{
    public PowerNotifications(DeviceAction? powerOnNotification, DeviceAction? powerOffNotification)
    {
        (this.PowerOnNotification, this.PowerOffNotification) = (powerOnNotification, powerOffNotification);
    }

    public DeviceAction PowerOffNotification { get; }

    public DeviceAction PowerOnNotification { get; }
}
