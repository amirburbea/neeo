namespace Neeo.Sdk.Devices;

public interface IDeviceProvider
{
    IDeviceBuilder ProvideDevice();
}