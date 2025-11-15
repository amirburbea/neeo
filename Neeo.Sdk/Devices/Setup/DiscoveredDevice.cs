namespace Neeo.Sdk.Devices.Setup;

/// <summary>
/// Represents a device found during the discovery phase.
/// </summary>
/// <param name="id">The discovered device identifier.</param>
/// <param name="name">The name of the discovered device.</param>
/// <param name="reachable">A value indicating if the device is currently reachable.</param>
/// <param name="room">The room in which this device is located.</param>
/// <param name="deviceBuilder">
/// When `enableDynamicDeviceBuilder` was configured via a call to <see cref="IDeviceBuilder.EnableDiscovery"/>,
/// represents the individual dynamic device (which needn't be similar to the discovering adapter).
/// </param>
public readonly struct DiscoveredDevice(
    string id,
    string name,
    bool? reachable = default,
    string? room = default,
    IDeviceBuilder? deviceBuilder = default
)
{
    /// <summary>
    /// When `enableDynamicDeviceBuilder` was configured via a call to <see
    /// cref="IDeviceBuilder.EnableDiscovery"/>, represents the individual dynamic device (which
    /// needn't be similar to the discovering adapter).
    /// </summary>
    public IDeviceBuilder? DeviceBuilder => deviceBuilder;

    /// <summary>
    /// Gets the discovered device identifier.
    /// </summary>
    public string Id => id;

    /// <summary>
    /// Gets the name of the discovered device.
    /// </summary>
    public string Name => name;

    /// <summary>
    /// Gets a value indicating if the device is currently reachable.
    /// </summary>
    public bool? Reachable => reachable;

    /// <summary>
    /// Gets the room in which this device is located.
    /// </summary>
    public string? Room => room;

    /// <summary>
    /// Deconstructs the discovered device.
    /// </summary>
    public void Deconstruct(out string id, out string name, out bool? reachable, out string? room, out IDeviceBuilder? deviceBuilder) => (
        id,
        name,
        reachable,
        room,
        deviceBuilder
    ) = (this.Id, this.Name, this.Reachable, this.Room, this.DeviceBuilder);
}
