using System;

namespace Remote.Neeo.Devices
{
    public sealed class DeviceSubscriptionCallbacks
    {
        public DeviceSubscriptionCallbacks(DeviceAddedHandler deviceAdded, DeviceRemovedHandler deviceRemoved, DeviceListInitializer initializeDeviceList)
        {
            this.DeviceAdded = deviceAdded ?? throw new ArgumentNullException(nameof(deviceAdded));
            this.DeviceRemoved = deviceRemoved ?? throw new ArgumentNullException(nameof(deviceRemoved));
            this.InitializeDeviceList = initializeDeviceList ?? throw new ArgumentNullException(nameof(initializeDeviceList));
        }

        public DeviceAddedHandler DeviceAdded { get; }

        public DeviceRemovedHandler DeviceRemoved { get; }

        public DeviceListInitializer InitializeDeviceList { get; }
    }
}