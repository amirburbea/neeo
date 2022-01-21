namespace Neeo.Api.Devices;

public interface IDeviceSubscriptionCallbacks
{
    DeviceListInitializer InitializeDeviceList { get; }

    DeviceAction OnDeviceAdded { get; }

    DeviceAction OnDeviceRemoved { get; }
}

internal sealed record class DeviceSubscriptionCallbacks(DeviceAction OnDeviceAdded, DeviceAction OnDeviceRemoved, DeviceListInitializer InitializeDeviceList) : IDeviceSubscriptionCallbacks;

