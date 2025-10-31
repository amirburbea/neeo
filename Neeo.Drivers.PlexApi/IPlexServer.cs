using System;
using System.Threading;
using System.Threading.Tasks;

namespace Neeo.Drivers.PlexApi;

public interface IPlexServer : IDisposable
{
    PlexServerInfo Info { get; }

    string ServerName { get; }

    PlexPlayerInfo? SelectedPlayer { get; }

    IObservable<PlexPlayerInfo> SelectedPlayerChanged { get; }

    Task<PlexPlayerInfo[]> GetPlayersAsync(CancellationToken cancellationToken = default);

    Task SelectPlayerAsync(string machineIdentifier, CancellationToken cancellationToken = default);
}
