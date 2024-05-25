namespace Neeo.Sdk.Devices;

/// <summary>
/// The entry point for creating a new Device.
/// </summary>
public static class Device
{
    /// <summary>
    /// Gets a fluent <see cref="IDeviceBuilder"/> for defining a NEEO device driver.
    /// </summary>
    /// <param name="name">The name of the device.</param>
    /// <param name="type">The type of device.</param>
    /// <param name="prefix">
    /// An optional prefix to attach to the internal name (defaults to the computer host name).
    /// <para />
    /// If the driver will be developed/debugged on one computer, but generally run from another (such as a Raspberry PI),
    /// it may be beneficial to specify a constant prefix.
    /// </param>
    /// <returns><see cref="IDeviceBuilder"/> for defining the NEEO device driver.</returns>
    public static IDeviceBuilder Create(string name, DeviceType type, string? prefix = default) => new DeviceBuilder(name, type, prefix);
}
