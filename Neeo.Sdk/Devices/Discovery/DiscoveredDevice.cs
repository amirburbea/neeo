using System.Text.Json.Serialization;

namespace Neeo.Sdk.Devices.Discovery;

public record struct DiscoveredDevice(
    string Id,
    string Name,
    bool? Reachable = default,
    string? Room = default,
    [property: JsonIgnore] IDeviceBuilder? DeviceBuilder = default
);