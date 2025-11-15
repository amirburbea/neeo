using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Neeo.Drivers.Plex;

using PlayerStatus = (PlexPlayerData Player, bool IsOnline);

public interface IPlexPlayerManager : IDisposable
{
    IReadOnlyDictionary<string, PlayerStatus> Players { get; }

    IObservable<IReadOnlyDictionary<string, PlayerStatus>> PlayersChanged { get; }

    PlayerStatus? SelectedPlayer { get; }

    string? SelectedPlayerId { get; }

    IObservable<PlayerStatus?> SelectedPlayerChanged { get; }

    Task InitializeAsync();

    Task RefreshAsync(CancellationToken cancellationToken = default);

    void SetSelectedPlayer(PlexPlayerData? playerData);

    PlexPlayerData? GetPlayerData(string machineIdentifier);
}

public interface IPlexPlayerManagerFactory
{
    IPlexPlayerManager Create(IPlexPlayerDiscovery playerDiscovery);
}

internal sealed class PlexPlayerManager : IPlexPlayerManager
{
    private readonly CancellationTokenSource _cancellationSource = new();
    private readonly TaskCompletionSource _initializationSource = new();
    private readonly BehaviorSubject<ImmutableDictionary<string, PlayerStatus>> _players = new([]);
    private readonly Lazy<Task> _pollingTask;
    private readonly IPlexPlayerDiscovery _playerDiscovery;
    private readonly ILogger<PlexPlayerManager> _logger;
    private readonly BehaviorSubject<string?> _selectedPlayerId = new(null);

    private PlexPlayerManager(IPlexPlayerDiscovery playerDiscovery, ILogger<PlexPlayerManager> logger)
    {
        this._playerDiscovery = playerDiscovery;
        this._logger = logger;
        this._pollingTask = new(this.PollAsync, LazyThreadSafetyMode.ExecutionAndPublication);
    }

    IReadOnlyDictionary<string, PlayerStatus> IPlexPlayerManager.Players => this._players.Value;

    PlayerStatus? IPlexPlayerManager.SelectedPlayer => PlexPlayerManager.GetSelectedPlayer(
        this._players.Value,
        this._selectedPlayerId.Value
    );

    PlexPlayerData? IPlexPlayerManager.GetPlayerData(string machineIdentifier) => this._players.Value.TryGetValue(
        machineIdentifier,
        out PlayerStatus tuple
    ) ? tuple.Player : default(PlexPlayerData?);

    IObservable<IReadOnlyDictionary<string, PlayerStatus>> IPlexPlayerManager.PlayersChanged => this._players;

    IObservable<PlayerStatus?> IPlexPlayerManager.SelectedPlayerChanged => this._players.CombineLatest(
        this._selectedPlayerId,
        PlexPlayerManager.GetSelectedPlayer
    );

    string? IPlexPlayerManager.SelectedPlayerId => this._selectedPlayerId.Value;

    public Task InitializeAsync()
    {
        _ = this._pollingTask.Value; // Ensure created.
        return this._initializationSource.Task;
    }

    public void Dispose()
    {
        this._cancellationSource.Cancel();
        this._initializationSource.TrySetCanceled(default);
        this._cancellationSource.Dispose();
        this._players.Dispose();
        this._selectedPlayerId.Dispose();
    }

    public async Task RefreshAsync(CancellationToken cancellationToken = default) => this.UpdatePlayers(
        await this._playerDiscovery.DiscoverPlayersAsync(cancellationToken).ConfigureAwait(false)
    );

    public void SetSelectedPlayer(PlexPlayerData? playerData)
    {
        ImmutableDictionary<string, (PlexPlayerData Player, bool IsOnline)> previousPlayers = this._players.Value;
        if (playerData is { } player && !previousPlayers.ContainsKey(player.MachineIdentifier))
        {
            this._players.OnNext(previousPlayers.Add(player.MachineIdentifier, (player, false)));
        }
        string? playerId = playerData?.MachineIdentifier;
        if (this._selectedPlayerId.Value != playerId)
        {
            this._selectedPlayerId.OnNext(playerId);
        }
    }

    private static PlayerStatus? GetSelectedPlayer(IReadOnlyDictionary<string, PlayerStatus> players, string? id)
    {
        return id is null || !players.TryGetValue(id, out PlayerStatus status) ? default(PlayerStatus?) : status;
    }

    private async Task PollAsync()
    {
        if (this._cancellationSource.IsCancellationRequested)
        {
            this._initializationSource.TrySetCanceled();
            return;
        }
        while (!this._cancellationSource.IsCancellationRequested)
        {
            try
            {
                this.UpdatePlayers(await this._playerDiscovery.DiscoverPlayersAsync(this._cancellationSource.Token).ConfigureAwait(false));
                this._initializationSource.TrySetResult();
            }
            catch (OperationCanceledException)
            {
                this._initializationSource.TrySetCanceled(this._cancellationSource.Token);
                return;
            }
            catch (Exception ex)
            {
                this._logger.LogError(ex, "An error occured while polling for players.");
                this._initializationSource.TrySetException(ex);
            }
            await Task.Delay(TimeSpan.FromSeconds(30d), this._cancellationSource.Token).ConfigureAwait(false);
        }
    }

    private void UpdatePlayers(PlexPlayerData[] discoveredPlayers)
    {
        ImmutableDictionary<string, PlayerStatus> previous = this._players.Value;
        if (previous.Count != 0)
        {
            Dictionary<string, PlexPlayerData> onlinePlayers = discoveredPlayers.ToDictionary(player => player.MachineIdentifier);
            if (!IsChanged(previous, onlinePlayers))
            {
                return;
            }
            Dictionary<string, PlayerStatus> next = [];
            foreach ((string id, (PlexPlayerData previousPlayer, _)) in previous)
            {
                next.Add(
                    id,
                    onlinePlayers.TryGetValue(id, out PlexPlayerData onlinePlayer)
                        ? (onlinePlayer, true)
                        : (previousPlayer, false)
                );
            }
            foreach (PlexPlayerData onlinePlayer in discoveredPlayers)
            {
                if (!next.ContainsKey(onlinePlayer.MachineIdentifier))
                {
                    next.Add(onlinePlayer.MachineIdentifier, (onlinePlayer, true));
                }
            }
            this._players.OnNext(next.ToImmutableDictionary());
        }
        else if (discoveredPlayers.Length != 0)
        {
            this._players.OnNext(discoveredPlayers.ToImmutableDictionary(player => player.MachineIdentifier, player => (player, true)));
        }

        static bool IsChanged(
            IReadOnlyDictionary<string, PlayerStatus> current,
            IReadOnlyDictionary<string, PlexPlayerData> onlinePlayers
        )
        {
            foreach ((string id, (PlexPlayerData player, bool isOnline)) in current)
            {
                if (isOnline ? !onlinePlayers.TryGetValue(id, out PlexPlayerData data) || !data.Equals(player) : onlinePlayers.ContainsKey(id))
                {
                    return true;
                }
            }
            return onlinePlayers.Keys.Any(key => !current.ContainsKey(key));
        }
    }

    public sealed class Factory(ILogger<PlexPlayerManager> logger) : IPlexPlayerManagerFactory
    {
        public IPlexPlayerManager Create(IPlexPlayerDiscovery discovery) => new PlexPlayerManager(discovery, logger);
    }
}
