using System;

namespace Remote.Neeo.Devices
{
    public sealed class DeviceSubscriptionController
    {
        public DeviceSubscriptionController(DeviceSubscriptionHandler deviceAdded, DeviceSubscriptionHandler deviceRemoved, DeviceListInitializer initializeDeviceList)
        {
            this.DeviceAdded = deviceAdded ?? throw new ArgumentNullException(nameof(deviceAdded));
            this.DeviceRemoved = deviceRemoved ?? throw new ArgumentNullException(nameof(deviceRemoved));
            this.InitializeDeviceList = initializeDeviceList ?? throw new ArgumentNullException(nameof(initializeDeviceList));
        }

        public DeviceSubscriptionHandler DeviceAdded { get; }

        public DeviceSubscriptionHandler DeviceRemoved { get; }

        public DeviceListInitializer InitializeDeviceList { get; }
    }
}