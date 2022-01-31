using Neeo.Sdk.Devices;

namespace Remote.HodgePodge;

public interface IExampleDeviceProvider
{
    IDeviceBuilder ProvideDevice();
}