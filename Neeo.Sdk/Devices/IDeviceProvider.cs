namespace Neeo.Sdk.Devices;

public interface IDeviceProvider
{
    IDeviceBuilder DeviceBuilder { get; }
}