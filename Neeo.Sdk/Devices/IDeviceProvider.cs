namespace Neeo.Sdk.Devices;

/// <summary>
/// Describes a class responsible for providing an <see cref="IDeviceBuilder"/> for use in the REST server.
/// </summary>
public interface IDeviceProvider
{
    /// <summary>
    /// Gets the device builder.
    /// </summary>
    IDeviceBuilder DeviceBuilder { get; }
}
