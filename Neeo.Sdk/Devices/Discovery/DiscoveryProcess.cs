using System.Threading.Tasks;

namespace Neeo.Sdk.Devices.Discovery;

public delegate Task<DiscoveryResult[]> DiscoveryProcess(string? optionalDeviceId);

public record struct DiscoveryResult(
    string Id,
    string Name,
    bool? Reachable = default,
    string? Room = default,
    IDeviceBuilder? Device = default
);