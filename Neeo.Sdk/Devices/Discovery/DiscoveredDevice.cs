using System.Text.Json.Serialization;

namespace Neeo.Sdk.Devices.Discovery;

/// <summary>
/// Represents a device found during the discovery phase.
/// </summary>
/// <param name="Id">The discovered device identifier.</param>
/// <param name="Name">The discovered device identifier.</param>
/// <param name="Reachable">The discovered device identifier.</param>
/// <param name="Room">The discovered device identifier.</param>
/// <param name="DeviceBuilder">
/// When `enableDynamicDeviceBuilder` was configured via a call to <see cref="IDeviceBuilder.EnableDiscovery" />,
/// represents the individual dynamic device (which needn't be similar to the discovering adapter).
/// </param>
public record struct DiscoveredDevice(
    string Id,
    string Name,
    bool? Reachable = default,
    string? Room = default,
    [property: JsonIgnore] IDeviceBuilder? DeviceBuilder = default
);