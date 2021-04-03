using System;

namespace Remote.Neeo.Devices
{
    public sealed class PowerNotifications
    {
        public PowerNotifications(DeviceAction powerOnNotification, DeviceAction powerOffNotification)
        {
            (this.PowerOnNotification, this.PowerOffNotification) = (
                powerOnNotification ?? throw new ArgumentNullException(nameof(powerOnNotification)),
                powerOffNotification ?? throw new ArgumentNullException(nameof(powerOffNotification))
            );
        }

        public DeviceAction PowerOffNotification { get; }

        public DeviceAction PowerOnNotification { get; }
    }
}