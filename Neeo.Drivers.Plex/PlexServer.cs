using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.WebSockets;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Neeo.Sdk.Utilities;

namespace Neeo.Drivers.Plex;

public interface IPlexServer : IDisposable
{
    MediaItem? ActiveMedia { get; }
    IObservable<MediaItem?> ActiveMediaChanged { get; }
    IObservable<Unit> Disposed { get; }
    PlexServerData Info { get; }
    bool IsConnected { get; }
    IObservable<bool> IsConnectedChanged { get; }
    string Name { get; }
    PlayState PlayState { get; }
    IObservable<PlayState> PlayStateChanged { get; }
    PlexPlayerInfo? SelectedPlayer { get; }
    IObservable<PlexPlayerInfo?> SelectedPlayerChanged { get; }

    Task<LibrarySection[]> GetLibrarySectionsAsync(CancellationToken cancellationToken = default);

    Task InitializeAsync(CancellationToken cancellationToken = default);

    Task<PlexPlayerInfo[]> ListPlayersAsync(bool refresh = false, CancellationToken cancellationToken = default);

    Task MoveAsync(Direction direction, CancellationToken cancellationToken = default);

    Task SelectPlayerAsync(string machineIdentifier, CancellationToken cancellationToken = default);

    Task SendNavigationCommandAsync(NavigationCommand command, CancellationToken cancellationToken = default);

    Task SendPlaybackCommandAsync(PlaybackCommand command, CancellationToken cancellationToken = default);
}

internal sealed partial class PlexServer : IPlexServer, IDisposable
{
    private readonly BehaviorSubject<MediaItem?> _activeMedia = new(null);
    private readonly CancellationTokenSource _cancellationTokenSource = new();
    private readonly Lazy<Task> _connection;
    private readonly IPlexServerDiscovery _discovery;
    private readonly CompositeDisposable _disposable;
    private readonly string _fileName;
    private readonly HttpClient _httpClient;
    private readonly TaskCompletionSource _initialization = new();
    private readonly BehaviorSubject<bool> _isConnected = new(false);
    private readonly Subject<Unit> _isDisposed = new();
    private readonly ReaderWriterLockSlim _lock = new();
    private readonly ILogger _logger;
    private readonly Dictionary<string, PlayerAvailability> _players = [];
    private readonly BehaviorSubject<PlayState> _playState = new(PlayState.Stopped);
    private readonly BehaviorSubject<int?> _ratingKey = new(null);
    private readonly BehaviorSubject<PlexPlayerInfo?> _selectedPlayer = new(null);
    private readonly IPlexSettingsManager _settingsManager;
    private readonly IPlexTokenStore _tokenStore;

    public PlexServer(
        string name,
        IPlexServerDiscovery discovery,
        IPlexTokenStore tokenStore,
        HttpClient httpClient,
        IPlexSettingsManager settingsManager,
        ILogger<PlexServer> logger
    )
    {
        this.Name = name;
        this._discovery = discovery;
        this._fileName = $"plex_{Convert.ToBase64String(Encoding.UTF8.GetBytes(discovery.Servers[name].MachineIdentifier)).TrimEnd('=')}.json";
        this._tokenStore = tokenStore;
        this._httpClient = httpClient;
        this._settingsManager = settingsManager;
        this._logger = logger;
        this._connection = new(this.ConnectAsync, true);
        this._disposable = new(
            this._activeMedia,
            this._isConnected,
            this._isDisposed,
            this._playState,
            this._ratingKey,
            this._selectedPlayer
        );
        this._ratingKey
            .DistinctUntilChanged()
            .TakeUntil(this.CancellationToken)
            .Select(ratingKey => ratingKey is { } key and not 0
                ? Observable.FromAsync((cancellationToken) => this.QueryMediaItemAsync(key, cancellationToken))
                : Observable.Return(default(MediaItem?)))
            .Switch()
            .Subscribe(this._activeMedia);
    }

    public MediaItem? ActiveMedia => this._activeMedia.Value;
    public IObservable<MediaItem?> ActiveMediaChanged => this._activeMedia;
    public IObservable<Unit> Disposed => this._isDisposed;
    public PlexServerData Info => this._discovery.Servers.GetValueOrDefault(this.Name);
    public bool IsConnected => this._isConnected.Value;
    public IObservable<bool> IsConnectedChanged => this._isConnected;
    public PlayState PlayState => this._playState.Value;
    public IObservable<PlayState> PlayStateChanged => this._playState;
    public PlexPlayerInfo? SelectedPlayer => this._selectedPlayer.Value;
    public IObservable<PlexPlayerInfo?> SelectedPlayerChanged => this._selectedPlayer;
    private IPAddress IPAddress => this._discovery.Servers[this.Name].IPAddress;
    public string Name { get; }
    private CancellationToken CancellationToken => this._cancellationTokenSource.Token;

    public void Dispose()
    {
        this._cancellationTokenSource.Cancel();
        this._isDisposed.OnNext(default);
        if (this._connection.IsValueCreated)
        {
            this._connection.Value.Wait();
        }
        this._disposable.Dispose();
        this._cancellationTokenSource.Dispose();
    }

    public async Task<LibrarySection[]> GetLibrarySectionsAsync(CancellationToken cancellationToken)
    {
        Response<LibrarySectionsMediaContainer> container = await this._httpClient.GetAsync<Response<LibrarySectionsMediaContainer>>(
            this.GetServerUri("library/sections"),
            this.AddTokenHeaders,
            cancellationToken
        ).ConfigureAwait(false);
        return container.MediaContainer.Sections;
    }

    public async Task InitializeAsync(CancellationToken cancellationToken)
    {
        if (!this._connection.Value.IsCompleted)
        {
            await this._initialization.Task.WaitAsync(cancellationToken).ConfigureAwait(false);
        }
    }

    public async Task<PlexPlayerInfo[]> ListPlayersAsync(bool refresh, CancellationToken cancellationToken)
    {
        if (refresh)
        {
            this.UpdatePlayers(await this.QueryPlayersAsync(cancellationToken).ConfigureAwait(false));
        }
        try
        {
            this._lock.EnterReadLock();
            return [.. this._players.Values.Select(info => info.Player)];
        }
        finally
        {
            this._lock.ExitReadLock();
        }
    }

    public Task MoveAsync(Direction direction, CancellationToken cancellationToken) => this.SendPlayerCommandAsync(
        $"navigation/move{Enum.GetName(direction)}",
        cancellationToken: cancellationToken
    );

    public Task SendNavigationCommandAsync(NavigationCommand command, CancellationToken cancellationToken)
    {
        return this.SendPlayerCommandAsync($"navigation/{TextAttribute.GetText(command)}", cancellationToken: cancellationToken);
    }

    public Task SendPlaybackCommandAsync(PlaybackCommand command, CancellationToken cancellationToken)
    {
        if (this._activeMedia.Value is not { Type: { } type })
        {
            return Task.CompletedTask;
        }
        return this.SendPlayerCommandAsync(
            $"playback/{TextAttribute.GetText(command)}",
            queryParameters: [("type", TextAttribute.GetText(type))],
            cancellationToken
        );
    }

    Task IPlexServer.SelectPlayerAsync(string machineIdentifier, CancellationToken cancellationToken)
    {
        return this.SelectPlayerAsync(machineIdentifier, cancellationToken);
    }

    private async Task<PlexPlayerInfo?> SelectPlayerAsync(string machineIdentifier, CancellationToken cancellationToken)
    {
        this._logger.LogInformation("Selecting player {Identifier}", machineIdentifier);
        if (await ResolvePlayerAsync().ConfigureAwait(false) is not { } player)
        {
            return null;
        }
        if (this._selectedPlayer.Value?.MachineIdentifier != machineIdentifier)
        {
            this._settingsManager.WriteAllBytes(this._fileName, JsonSerializer.SerializeToUtf8Bytes(player, JsonSerializerOptions.Web));
        }
        this._selectedPlayer.OnNext(player);
        return player;

        async ValueTask<PlexPlayerInfo?> ResolvePlayerAsync()
        {
            if (this._players.GetValueOrDefault(machineIdentifier) is { Player: { } player, IsAvailable: true })
            {
                return player;
            }
            PlexPlayerInfo[] players = await this.QueryPlayersAsync(cancellationToken).ConfigureAwait(false);
            int index = Array.FindIndex(players, player => player.MachineIdentifier == machineIdentifier);
            return index == -1 ? null : players[index];
        }
    }

    private void AddTokenHeaders(HttpRequestMessage request)
    {
        request.Headers.Add("X-Plex-Token", this._tokenStore.AuthToken);
        request.Headers.Add("X-Plex-Client-Identifier", this._tokenStore.ClientIdentifier);
    }

    private async Task ConnectAsync()
    {
        Uri uri = this.GetServerUri(":/websockets/notifications", "ws");
        while (!this._cancellationTokenSource.IsCancellationRequested)
        {
            // Disable keep-alive pings, Plex server does not support them.
            using ClientWebSocket webSocket = new() { Options = { KeepAliveInterval = Timeout.InfiniteTimeSpan } };
            webSocket.Options.SetRequestHeader("X-Plex-Token", this._tokenStore.AuthToken);
            try
            {
                this._logger.LogInformation("Connecting to Plex server '{Name}'...", this.Name);
                await webSocket.ConnectAsync(uri, this.CancellationToken).ConfigureAwait(false);
                this._logger.LogInformation("Connected to Plex server '{Name}'", this.Name);
                this._isConnected.OnNext(true);
                this._initialization.TrySetResult();
                if (this._selectedPlayer.Value is null && this._settingsManager.HasFile(this._fileName) && this._settingsManager.ReadAllBytes(this._fileName) is { } bytes)
                {
                    this._selectedPlayer.OnNext(JsonSerializer.Deserialize<PlexPlayerInfo>(bytes, JsonSerializerOptions.Web));
                }
                IObservable<PlexPlayerInfo[]> players = Observable.Interval(TimeSpan.FromSeconds(30d))
                    .StartWith(0L)
                    .SelectMany(_ => Observable.FromAsync(this.QueryPlayersAsync));
                using (players.Subscribe(onNext: this.UpdatePlayers))
                {
                    await JsonWebSocket.MessageLoop<ServerMessage>(
                        webSocket,
                        (message, _) =>
                        {
                            return message.Container is { Type: ServerNotificationType.Playing, Notifications: { Length: > 0 } notifications }
                                ? this.ProcessNotificationsAsync(notifications)
                                : Task.CompletedTask;
                        },
                        cancellationToken: this.CancellationToken
                    ).ConfigureAwait(false);
                }
            }
            catch (WebSocketException e)
            {
                this._logger.LogError(e, "Error on Plex server: {Name}", this.Name);
                await Task.Delay(TimeSpan.FromSeconds(5), this.CancellationToken);
            }
            catch (OperationCanceledException) when (this.CancellationToken.IsCancellationRequested)
            {
                return;
            }
            finally
            {
                this._isConnected.OnNext(false);
                this._initialization.TrySetResult();
            }
        }
    }

    private Uri GetServerUri(string pathSuffix, string protocol = "http", params (string, string)[] queryParameters)
    {
        StringBuilder builder = new(protocol);
        builder
            .Append("://")
            .Append(this.IPAddress)
            .Append(":32400/")
            .Append(pathSuffix);
        for (int index = 0; index < queryParameters.Length; index++)
        {
            (string key, string value) = queryParameters[index];
            builder
                .Append(index == 0 ? '?' : '&')
                .Append($"{key}={WebUtility.UrlEncode(value)}");
        }
        return new(builder.ToString());
    }

    private async Task ProcessNotificationsAsync(PlayStateNotification[] notifications)
    {
        if ((this.SelectedPlayer ?? await this.SelectPlayerAsync(notifications[0].ClientIdentifier, this.CancellationToken).ConfigureAwait(false)) is { } player)
        {
            foreach (PlayStateNotification notification in notifications)
            {
                if (notification.ClientIdentifier == player.MachineIdentifier)
                {
                    this._playState.OnNext(notification.State);
                    this._ratingKey.OnNext(notification.RatingKey);
                }
            }
        }
    }

    private async Task<MediaItem?> QueryMediaItemAsync(int ratingKey, CancellationToken cancellationToken)
    {
        Response<MediaItemDetailContainer> response = await this._httpClient.GetAsync<Response<MediaItemDetailContainer>>(
            this.GetServerUri($"library/metadata/{ratingKey}"),
            this.AddTokenHeaders,
            cancellationToken
        ).ConfigureAwait(false);
        if (response.MediaContainer.Metadata is not [{ } metadata, ..])
        {
            return null;
        }
        return new(
            metadata.RatingKey,
            metadata.Type,
            metadata.Title,
            metadata.Summary,
            metadata.Thumbnail is { Length: not 0 } url ? this.GetServerUri($"photo/:/transcode", queryParameters: ("url", url)) : null
        );
    }

    private async Task<PlexPlayerInfo[]> QueryPlayersAsync(CancellationToken cancellationToken)
    {
        Response<ClientsMediaContainer> response = await this._httpClient.GetAsync<Response<ClientsMediaContainer>>(
            this.GetServerUri("clients"),
            this.AddTokenHeaders,
            cancellationToken
        ).ConfigureAwait(false);
        if (response.MediaContainer.Clients is not { Length: > 0 } clients)
        {
            return [];
        }
        return [..
            from client in clients
            where (client.ProtocolCapabilities & PlayerCapabilities.Playback) == PlayerCapabilities.Playback
            select new PlexPlayerInfo(
                client.Name,
                client.MachineIdentifier,
                client.ProtocolCapabilities,
                IPAddress.Parse(client.Address),
                client.Port,
                client.Product
            )
        ];
    }

    private async Task SendPlayerCommandAsync(string command, (string, string)[]? queryParameters = null, CancellationToken cancellationToken = default)
    {
        if (this.SelectedPlayer is not { MachineIdentifier: { } machineIdentifier })
        {
            return;
        }
        Uri uri = this.GetServerUri($"player/{command}", queryParameters: queryParameters ?? []);
        try
        {
            using HttpRequestMessage request = new(HttpMethod.Get, uri) { Headers = { { "X-Plex-Target-Client-Identifier", machineIdentifier } } };
            this.AddTokenHeaders(request);
            using HttpResponseMessage response = await this._httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken).ConfigureAwait(false);
            if (!response.IsSuccessStatusCode)
            {
                this._logger.LogWarning("Plex command ({Command}) failed: {Status}", command, response.StatusCode);
            }
        }
        catch (Exception e)
        {
            this._logger.LogWarning(e, "Plex command failed: {Uri}", uri);
        }
    }

    private void UpdatePlayers(PlexPlayerInfo[] players)
    {
        this._lock.EnterUpgradeableReadLock();
        try
        {
            HashSet<string> keys = [.. this._players.Keys];
            List<PlexPlayerInfo> playersToUpdate = [];
            foreach (PlexPlayerInfo player in players)
            {
                if (this._players.GetValueOrDefault(player.MachineIdentifier) is { IsAvailable: true } info && info.Player.Equals(player))
                {
                    continue;
                }
                playersToUpdate.Add(player);
                keys.Remove(player.MachineIdentifier);
            }
            if (keys.Count == 0 && playersToUpdate.Count == 0)
            {
                return;
            }
            try
            {
                this._lock.EnterWriteLock();
                foreach (string key in keys)
                {
                    this._players[key].IsAvailable = false;
                }
                foreach (PlexPlayerInfo player in playersToUpdate)
                {
                    if (this._players.GetValueOrDefault(player.MachineIdentifier) is { } tuple)
                    {
                        (tuple.Player, tuple.IsAvailable) = (player, true);
                    }
                    else
                    {
                        this._players.Add(player.MachineIdentifier, new() { Player = player, IsAvailable = true });
                    }
                }
            }
            finally
            {
                this._lock.ExitWriteLock();
            }
        }
        finally
        {
            this._lock.ExitUpgradeableReadLock();
        }
    }

    private sealed class PlayerAvailability
    {
        public bool IsAvailable { get; set; }

        public required PlexPlayerInfo Player { get; set; }
    }
}
