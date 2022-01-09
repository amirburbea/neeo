using System.Threading.Tasks;

namespace Remote.Neeo.Devices.Discovery;

public delegate Task<DiscoveryResult[]> DiscoveryProcessor(string? deviceId = default);
