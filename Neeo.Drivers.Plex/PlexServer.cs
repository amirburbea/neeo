using System;
using System.Collections.Generic;
using System.Collections.Immutable;
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
    bool IsConnected { get; }
    IObservable<bool> IsConnectedChanged { get; }
    string MachineIdentifier { get; }
    string Name { get; }
    PlayState PlayState { get; }
    IObservable<PlayState> PlayStateChanged { get; }
    PlayerData? SelectedPlayer { get; }
    IObservable<PlayerData?> SelectedPlayerChanged { get; }
    string? SelectedPlayerName { get; }
    ServerData ServerData { get; }

    Task<LibrarySectionInfo[]> GetLibrarySectionsAsync(CancellationToken cancellationToken = default);

    Task InitializeAsync(CancellationToken cancellationToken = default);

    Task<PlayerData[]> ListPlayersAsync(bool refresh = false, CancellationToken cancellationToken = default);

    Task PlayMediaAsync(MediaItem item, CancellationToken cancellationToken = default);

    Task PlayMediaAsync(int ratingKey, CancellationToken cancellationToken = default);

    Task SelectPlayerAsync(string playerIdentifier, CancellationToken cancellationToken = default);

    Task SendNavigationCommandAsync(NavigationCommand command, CancellationToken cancellationToken = default);

    Task SendPlaybackCommandAsync(PlaybackCommand command, CancellationToken cancellationToken = default);
}

public interface IPlexServerFactory
{
    IPlexServer Create(string machineIdentifier);
}

internal sealed partial class PlexServer : IPlexServer, IDisposable
{
    private readonly BehaviorSubject<MediaItem?> _activeMedia = new(null);
    private readonly CancellationTokenSource _cancellationTokenSource = new();
    private readonly Lazy<Task> _connectionTask;
    private readonly CompositeDisposable _disposable;
    private readonly string _fileName;
    private readonly HttpClient _httpClient;
    private readonly TaskCompletionSource _initializationTaskSource = new();
    private readonly BehaviorSubject<bool> _isConnected = new(false);
    private readonly Subject<Unit> _isDisposed = new();
    private readonly ILogger _logger;
    private readonly Lazy<IDisposable> _playerDiscoverySubscription;
    private readonly TaskCompletionSource _playerDiscoveryTaskSource = new();
    private readonly BehaviorSubject<ImmutableDictionary<string, PlayerData>> _players = new([]);
    private readonly BehaviorSubject<PlayState> _playState = new(PlayState.Stopped);
    private readonly BehaviorSubject<int?> _ratingKey = new(null);
    private readonly BehaviorSubject<string?> _selectedPlayerId = new(null);
    private readonly IPlexServerDiscovery _serverDiscovery;
    private readonly IPlexSettingsManager _settingsManager;
    private readonly IPlexTokenStore _tokenStore;
    private long _commandId;

    private PlexServer(
        string machineIdentifier,
        HttpClient httpClient,
        IPlexSettingsManager settingsManager,
        IPlexServerDiscovery serverDiscovery,
        IPlexTokenStore tokenStore,
        ILogger<PlexServer> logger
    )
    {
        this.MachineIdentifier = machineIdentifier;
        this._serverDiscovery = serverDiscovery;
        this._tokenStore = tokenStore;
        this._fileName = $"plex_{Convert.ToBase64String(Encoding.UTF8.GetBytes(machineIdentifier)).TrimEnd('=')}.json";
        this._httpClient = httpClient;
        this._settingsManager = settingsManager;
        this._logger = logger;
        this._disposable = new(
            this._activeMedia,
            this._isConnected,
            this._isDisposed,
            this._playState,
            this._ratingKey,
            this._players,
            this._selectedPlayerId,
            this._cancellationTokenSource
        );
        this._ratingKey
            .DistinctUntilChanged()
            .Select(value => value is { } key and > 0
                ? Observable.FromAsync(token => this.QueryMediaItemAsync(key, token))
                : Observable.Return(default(MediaItem?)))
            .Switch()
            .TakeUntil(this._cancellationTokenSource.Token)
            .Subscribe(this._activeMedia);
        this.SelectedPlayerChanged = this._selectedPlayerId
            .CombineLatest(this._players, (id, players) => PlexServer.GetPlayerById(players, id))
            .DistinctUntilChanged()
            .TakeUntil(this._cancellationTokenSource.Token);
        IObservable<Unit> interval = Observable.Interval(TimeSpan.FromSeconds(30d))
            .StartWith(0L)
            .Select(_ => Observable.FromAsync(this.DiscoverPlayersAsync))
            .Switch()
            .Do(_ => this._playerDiscoveryTaskSource.TrySetResult())
            .TakeUntil(this._cancellationTokenSource.Token);
        this._playerDiscoverySubscription = new(interval.Subscribe, LazyThreadSafetyMode.ExecutionAndPublication);
        this._connectionTask = new(this.ConnectSocketAsync, LazyThreadSafetyMode.ExecutionAndPublication);
    }

    public MediaItem? ActiveMedia => this._activeMedia.Value;
    public IObservable<MediaItem?> ActiveMediaChanged => this._activeMedia;
    public IObservable<Unit> Disposed => this._isDisposed;
    public bool IsConnected => this._isConnected.Value;
    public IObservable<bool> IsConnectedChanged => this._isConnected;
    public string MachineIdentifier { get; }
    public PlayState PlayState => this._playState.Value;
    public IObservable<PlayState> PlayStateChanged => this._playState;
    public PlayerData? SelectedPlayer => PlexServer.GetPlayerById(this._players.Value, this._selectedPlayerId.Value);
    public IObservable<PlayerData?> SelectedPlayerChanged { get; }
    public string? SelectedPlayerName => this.SelectedPlayer?.Name;
    public ServerData ServerData => this._serverDiscovery.Data[this.MachineIdentifier];
    public string IPAddress => this.ServerData.IPAddress;
    public string Name => this.ServerData.Name;
    public int Port => this.ServerData.Port;

    public void Dispose()
    {
        this._cancellationTokenSource.Cancel();
        if (this._playerDiscoverySubscription.IsValueCreated)
        {
            this._playerDiscoverySubscription.Value.Dispose();
        }
        if (this._connectionTask.IsValueCreated)
        {
            this._connectionTask.Value.Wait();
        }
        this._isDisposed.OnNext(default);
        this._disposable.Dispose();
    }

    public async Task<LibrarySectionInfo[]> GetLibrarySectionsAsync(CancellationToken cancellationToken)
    {
        Response<LibrarySectionsMediaContainer> container = await this._httpClient.GetAsync<Response<LibrarySectionsMediaContainer>>(
            this.GetUri("library/sections"),
            cancellationToken: cancellationToken
        ).ConfigureAwait(false);
        return container.MediaContainer.Sections;
    }

    public Task InitializeAsync(CancellationToken cancellationToken)
    {
        _ = this._connectionTask.Value;
        _ = this._playerDiscoverySubscription.Value;
        return this._initializationTaskSource.Task.WaitAsync(cancellationToken);
    }

    public async Task<PlayerData[]> ListPlayersAsync(bool refresh, CancellationToken cancellationToken)
    {
        if (refresh)
        {
            await this.DiscoverPlayersAsync(cancellationToken).ConfigureAwait(false);
        }
        return [.. this._players.Value.Values];
    }

    public async Task PlayMediaAsync(MediaItem media, CancellationToken cancellationToken)
    {
        await this.SendPlaybackCommandAsync(PlaybackCommand.Stop, cancellationToken).ConfigureAwait(false);
        int playQueueId = await this.CreatePlayQueueAsync(media, cancellationToken).ConfigureAwait(false);
        // The remaining commands switch to this instance's cancellation token.
        await this.SendPlayerCommandAsync(
            "playback/playMedia",
            [
                ("providerIdentifier", "com.plexapp.plugins.library"),
                ("machineIdentifier", this.MachineIdentifier),
                ("protocol", "http"),
                ("address", this.IPAddress),
                ("port", this.Port.ToString()),
                ("offset", "0"),
                ("key", $"/library/metadata/{media.RatingKey}"),
                ("type", TextAttribute.GetText(media.Type)),
                ("containerKey", $"/playQueues/{playQueueId}?window=100&own=1")
            ],
            this._cancellationTokenSource.Token
        ).ConfigureAwait(false);
        await this.SendPlayerCommandAsync("timeline/poll", [("wait", "1")], this._cancellationTokenSource.Token).ConfigureAwait(false);
    }

    public async Task PlayMediaAsync(int ratingKey, CancellationToken cancellationToken)
    {
        if (await this.QueryMediaItemAsync(ratingKey, cancellationToken).ConfigureAwait(false) is { } media)
        {
            await this.PlayMediaAsync(media, cancellationToken).ConfigureAwait(false);
        }
    }

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
            cancellationToken: cancellationToken
        );
    }

    Task IPlexServer.SelectPlayerAsync(string playerIdentifier, CancellationToken cancellationToken) => this.SelectPlayerAsync(playerIdentifier, cancellationToken);

    private async Task<PlayerData?> SelectPlayerAsync(string playerIdentifier, CancellationToken cancellationToken)
    {
        this._logger.LogInformation("Selecting player {Identifier}", playerIdentifier);
        if (await ResolvePlayerAsync().ConfigureAwait(false) is not { } player)
        {
            return default;
        }
        if (this._selectedPlayerId.Value != playerIdentifier)
        {
            this.SaveSelectedPlayer(player);
            this._selectedPlayerId.OnNext(playerIdentifier);
        }
        return player;

        async ValueTask<PlayerData?> ResolvePlayerAsync()
        {
            if (this._players.Value.TryGetValue(playerIdentifier, out PlayerData player))
            {
                return player;
            }
            await this.DiscoverPlayersAsync(cancellationToken).ConfigureAwait(false);
            return this._players.Value.TryGetValue(playerIdentifier, out player) ? player : default(PlayerData?);
        }
    }

    private static PlayerData? GetPlayerById(ImmutableDictionary<string, PlayerData> players, string? id) => id switch
    {
        { } playerId when players.TryGetValue(playerId, out PlayerData player) => player,
        _ => null
    };

    private async Task ConnectSocketAsync()
    {
        Uri uri = this.GetUri(":/websockets/notifications", "ws");
        while (!this._cancellationTokenSource.IsCancellationRequested)
        {
            // Disable keep-alive pings, Plex server does not support them.
            using ClientWebSocket webSocket = new() { Options = { KeepAliveInterval = Timeout.InfiniteTimeSpan } };
            webSocket.Options.SetRequestHeader("X-Plex-Token", this._tokenStore.AuthToken);
            try
            {
                this._logger.LogInformation("Connecting to Plex server '{Name}'...", this.Name);
                await webSocket.ConnectAsync(uri, this._cancellationTokenSource.Token).ConfigureAwait(false);
                this._logger.LogInformation("Connected to Plex server '{Name}'", this.Name);
                this._isConnected.OnNext(true);
                await this._playerDiscoveryTaskSource.Task.ConfigureAwait(false);
                this._initializationTaskSource.TrySetResult();
                if (this._selectedPlayerId.Value is null && this._settingsManager.HasFile(this._fileName) && this._settingsManager.ReadAllBytes(this._fileName) is { } bytes)
                {
                    PlayerData player = JsonSerializer.Deserialize<PlayerData>(bytes, JsonSerializerOptions.Web);
                    // If the player is not found, add it as offline.
                    if (this._players.Value is { } players && !players.ContainsKey(player.MachineIdentifier))
                    {
                        this._players.OnNext(players.Add(player.MachineIdentifier, player with { IsOnline = false }));
                    }
                    this._selectedPlayerId.OnNext(player.MachineIdentifier);
                }
                await JsonWebSocket.MessageLoop<ServerMessage>(
                    webSocket,
                    (message, _) => message.Container switch
                    {
                        { Type: ServerNotificationType.Playing, Notifications: { Length: > 0 } notifications } => this.ProcessNotificationsAsync(notifications),
                        _ => Task.CompletedTask
                    },
                    cancellationToken: this._cancellationTokenSource.Token
                ).ConfigureAwait(false);
            }
            catch (WebSocketException e)
            {
                this._logger.LogError(e, "Error on Plex server: {Name}", this.Name);
                await Task.Delay(TimeSpan.FromSeconds(5), this._cancellationTokenSource.Token);
            }
            catch (OperationCanceledException) when (this._cancellationTokenSource.IsCancellationRequested)
            {
                return;
            }
            finally
            {
                this._isConnected.OnNext(false);
                this._initializationTaskSource.TrySetResult();
            }
        }
    }

    private async Task<int> CreatePlayQueueAsync(MediaItem media, CancellationToken cancellationToken)
    {
        Uri uri = this.GetUri(
            "playQueues",
            queryParameters: [
                ("type", TextAttribute.GetText(media.Type)),
                ("shuffle", "0"),
                ("repeat", "0"),
                ("own", "1"),
                ("uri", $"server://{this.MachineIdentifier}/com.plexapp.plugins.library/library/metadata/{media.RatingKey}")
            ]
        );
        return await this._httpClient.PostAsync<Response<JsonElement>>(uri, null, cancellationToken).ConfigureAwait(false) switch
        {
            { MediaContainer: { } element } => element.GetProperty("playQueueID").GetInt32()
        };
    }

    private async Task DiscoverPlayersAsync(CancellationToken cancellationToken)
    {
        ClientServer[] onlineClients = await QueryClientsAsync(cancellationToken).ConfigureAwait(false);
        ImmutableDictionary<string, PlayerData> previous = this._players.Value;
        if (previous.Count == 0 && onlineClients.Length == 0)
        {
            return;
        }
        Dictionary<string, PlayerData> onlinePlayers = onlineClients.ToDictionary(client => client.MachineIdentifier, CreatePlayer);
        if (previous.Count == 0)
        {
            this._players.OnNext(onlinePlayers.ToImmutableDictionary());
            return;
        }
        if (!IsChanged(previous, onlinePlayers))
        {
            return;
        }
        Dictionary<string, PlayerData> next = [];
        foreach ((string id, PlayerData previousPlayer) in previous)
        {
            if (!onlinePlayers.ContainsKey(id))
            {
                next.Add(id, previousPlayer with { IsOnline = false });
            }
        }
        foreach ((string id, PlayerData player) in onlinePlayers)
        {
            next.Add(id, player);
        }
        this._players.OnNext(next.ToImmutableDictionary());

        async Task<ClientServer[]> QueryClientsAsync(CancellationToken cancellationToken)
        {
            return await this._httpClient.GetAsync<Response<ClientsMediaContainer>>(this.GetUri("clients"), null, cancellationToken).ConfigureAwait(false) is { MediaContainer.Clients: { } clients }
                ? [.. clients.Where(client => client.ProtocolVersion == 1 && (client.ProtocolCapabilities & PlayerCapabilities.Playback) == PlayerCapabilities.Playback)]
                : [];
        }

        static bool IsChanged(IReadOnlyDictionary<string, PlayerData> current, IReadOnlyDictionary<string, PlayerData> onlinePlayers) => (
            onlinePlayers.Keys.Any(key => !current.ContainsKey(key)) ||
            current.Values.Any(current => onlinePlayers.TryGetValue(current.MachineIdentifier, out PlayerData next) ? !current.Equals(next) : current.IsOnline)
        );

        static PlayerData CreatePlayer(ClientServer client) => new(
            client.Name,
            client.MachineIdentifier,
            client.ProtocolCapabilities,
            client.Address,
            client.Port,
            client.Product
        );
    }

    private Uri GetUri(string pathSuffix, string protocol = "http", params (string, string)[] queryParameters)
    {
        StringBuilder builder = new StringBuilder(protocol)
            .Append("://")
            .Append(this.IPAddress)
            .Append(':')
            .Append(this.Port)
            .Append('/')
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
        if (this._selectedPlayerId.Value is not { } playerId)
        {
            string id = notifications[0].ClientIdentifier;
            if (await this.SelectPlayerAsync(id, this._cancellationTokenSource.Token).ConfigureAwait(false) is null)
            {
                // Notification is for a player we don't know about.
                return;
            }
            playerId = id;
        }
        if (Array.FindIndex(notifications, notification => notification.ClientIdentifier == playerId) is int index and not -1)
        {
            PlayStateNotification notification = notifications[index];
            this._playState.OnNext(notification.State);
            this._ratingKey.OnNext(notification.RatingKey);
        }
    }

    private async Task<MediaItem?> QueryMediaItemAsync(int ratingKey, CancellationToken cancellationToken)
    {
        string json = (await this._httpClient.GetAsync<JsonElement>(
            this.GetUri($"library/metadata/{ratingKey}"),
            cancellationToken: cancellationToken
        ).ConfigureAwait(false)).ToString();

        Response<MediaItemDetailContainer> response = await this._httpClient.GetAsync<Response<MediaItemDetailContainer>>(
            this.GetUri($"library/metadata/{ratingKey}"),
            cancellationToken: cancellationToken
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
            metadata.Thumbnail is { Length: not 0 } url ? this.GetUri($"photo/:/transcode", queryParameters: ("url", url)) : null
        );
    }

    private void SaveSelectedPlayer(PlayerData player)
    {
        this._settingsManager.WriteAllBytes(this._fileName, JsonSerializer.SerializeToUtf8Bytes(player, JsonSerializerOptions.Web));
    }

    private async Task SendPlayerCommandAsync(string command, (string, string)[]? queryParameters = null, CancellationToken cancellationToken = default)
    {
        if (this.SelectedPlayer is not { } player)
        {
            return;
        }
        if (TextAttribute.GetEnum<PlayerCapabilities>(command[..command.IndexOf('/')]) is { } capability && (player.Capabilities & capability) != capability)
        {
            this._logger.LogWarning("Player does not support {Capability}, errors may occur.", Enum.GetName(capability));
        }
        long commandId = Interlocked.Increment(ref this._commandId);
        Uri uri = this.GetUri($"player/{command}", queryParameters: [.. queryParameters ?? [], ("commandID", commandId.ToString())]);
        try
        {
            using HttpRequestMessage request = new(HttpMethod.Get, uri)
            {
                Headers =
                {
                    { "X-Plex-Target-Client-Identifier", player.MachineIdentifier }
                }
            };
            using HttpResponseMessage response = await this._httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken).ConfigureAwait(false);
            if (!response.IsSuccessStatusCode)
            {
                this._logger.LogWarning("Plex command ({Command}) failed: {Code}", command, response.StatusCode);
            }
        }
        catch (Exception e)
        {
            this._logger.LogWarning("Plex command ({Command}) failed: {Error}", command, e);
        }
    }
}
