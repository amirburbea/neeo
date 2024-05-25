using System.Text.Json.Serialization;

namespace Neeo.Sdk.Devices.Setup;

/// <summary>
/// Represents a device found during the discovery phase.
/// </summary>
/// <param name="Id">The discovered device identifier.</param>
/// <param name="Name">The name of the discovered device.</param>
/// <param name="Reachable">A value indicating if the device is currently reachable.</param>
/// <param name="Room">The room this device should be placed in.</param>
/// <param name="DeviceBuilder">
/// When `enableDynamicDeviceBuilder` was configured via a call to <see cref="IDeviceBuilder.EnableDiscovery" />,
/// represents the individual dynamic device (which needn't be similar to the discovering adapter).
/// </param>
public readonly record struct DiscoveredDevice(
    string Id,
    string Name,
    bool? Reachable = default,
    string? Room = default,
    [property: JsonIgnore] IDeviceBuilder? DeviceBuilder = default
);
