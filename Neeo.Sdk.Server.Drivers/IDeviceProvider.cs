using Neeo.Sdk.Devices;

namespace Neeo.Sdk.Server.Drivers;

/// <summary>
/// Describes a class responsible for providing an <see cref="IDeviceBuilder"/> for use in starting the REST server.
/// </summary>
public interface IDeviceProvider
{
    /// <summary>
    /// Gets the device builder.
    /// </summary>
    IDeviceBuilder DeviceBuilder { get; }
}
