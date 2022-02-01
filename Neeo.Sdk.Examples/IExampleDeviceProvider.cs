using Neeo.Sdk.Devices;

namespace Neeo.Sdk.Examples;

public interface IExampleDeviceProvider
{
    IDeviceBuilder Provide();
}