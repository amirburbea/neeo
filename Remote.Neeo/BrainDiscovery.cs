using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Zeroconf;

namespace Remote.Neeo
{
    public static class BrainDiscovery
    {
        public static async Task<IReadOnlyList<BrainDescriptor>> DiscoverBrainsAsync()
        {
            const string serviceName = "_neeo._tcp.local.";
            return (await ZeroconfResolver.ResolveAsync(serviceName).ConfigureAwait(false))
                .Select(host =>
                {
                    IService service = host.Services[serviceName];
                    IReadOnlyDictionary<string, string> properties = service.Properties[0];
                    return new BrainDescriptor(
                        host.IPAddress,
                        service.Port,
                        host.DisplayName,
                        properties["hon"],
                        properties["rel"],
                        properties["reg"],
                        DateTime.ParseExact(
                            properties["upd"],
                            "yyyy-M-d",
                            CultureInfo.InvariantCulture,
                            DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal
                        )
                    );
                })
                .ToList();
        }

        public static async Task<BrainDescriptor?> GetBrainAsync(Func<BrainDescriptor, bool> predicate)
        {
            return (await BrainDiscovery.DiscoverBrainsAsync().ConfigureAwait(false)).FirstOrDefault(predicate);
        }
    }
}
