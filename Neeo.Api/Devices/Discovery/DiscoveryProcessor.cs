using System.Threading.Tasks;

namespace Neeo.Api.Devices.Discovery;

public delegate Task<DiscoveryResult[]> DiscoveryProcessor(string? deviceId = default);
