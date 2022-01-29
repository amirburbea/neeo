using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Neeo.Sdk.Devices.Discovery;

public delegate Task<DiscoveredDevice[]> DiscoveryProcess(string? optionalDeviceId = default);

public record struct DiscoveredDevice(
    string Id,
    string Name,
    bool? Reachable = default,
    string? Room = default,
    [property: JsonIgnore] IDeviceBuilder? DeviceBuilder = default
);