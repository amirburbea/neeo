using System.Collections.Generic;
using System.Threading.Tasks;

namespace Remote.Neeo.Devices.Discovery
{
    public delegate Task<IReadOnlyCollection<DiscoveryResult>> DiscoveryController(string? deviceId);
}
