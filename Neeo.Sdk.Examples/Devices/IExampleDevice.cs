using Neeo.Sdk.Devices;

namespace Neeo.Sdk.Examples.Devices;

public interface IExampleDevice
{
    IDeviceBuilder Builder { get; }
}