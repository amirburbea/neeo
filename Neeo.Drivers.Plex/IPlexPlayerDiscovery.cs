using System.Threading;
using System.Threading.Tasks;

namespace Neeo.Drivers.Plex;

public interface IPlexPlayerDiscovery
{
    Task<PlexPlayerData[]> DiscoverPlayersAsync(CancellationToken cancellationToken = default);
}
