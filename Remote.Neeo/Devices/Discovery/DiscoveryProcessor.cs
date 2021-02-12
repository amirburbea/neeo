using System.Collections.Generic;
using System.Threading.Tasks;

namespace Remote.Neeo.Devices.Discovery
{
    public delegate Task<IReadOnlyCollection<DiscoveryResult>> DiscoveryProcessor(string? deviceId);
}
