using System;
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

using PlayerStatus = (PlexPlayerData Player, bool IsOnline);

public interface IPlexServer : IDisposable
{
    MediaItem? ActiveMedia { get; }
    IObservable<MediaItem?> ActiveMediaChanged { get; }
    PlexServerData Data { get; }
    IObservable<Unit> Disposed { get; }
    bool IsConnected { get; }
    IObservable<bool> IsConnectedChanged { get; }
    string MachineIdentifier { get; }
    string Name { get; }
    PlayState PlayState { get; }
    IObservable<PlayState> PlayStateChanged { get; }
    PlayerStatus? SelectedPlayer { get; }
    IObservable<PlayerStatus?> SelectedPlayerChanged { get; }
    string? SelectedPlayerName { get; }

    Task<LibrarySection[]> GetLibrarySectionsAsync(CancellationToken cancellationToken = default);

    Task InitializeAsync(CancellationToken cancellationToken = default);

    Task<PlayerStatus[]> ListPlayersAsync(bool refresh = false, CancellationToken cancellationToken = default);

    Task SelectPlayerAsync(string playerIdentifier, CancellationToken cancellationToken = default);

    Task SendNavigationCommandAsync(NavigationCommand command, CancellationToken cancellationToken = default);

    Task SendPlaybackCommandAsync(PlaybackCommand command, CancellationToken cancellationToken = default);
}

internal sealed partial class PlexServer : IPlexServer, IPlexPlayerDiscovery, IDisposable
{
    private readonly BehaviorSubject<MediaItem?> _activeMedia = new(null);
    private readonly CancellationTokenSource _cancellationTokenSource = new();
    private readonly Lazy<Task> _connection;
    private readonly CompositeDisposable _disposable;
    private readonly string _fileName;
    private readonly HttpClient _httpClient;
    private readonly TaskCompletionSource _initialization = new();
    private readonly BehaviorSubject<bool> _isConnected = new(false);
    private readonly Subject<Unit> _isDisposed = new();
    private readonly ILogger _logger;
    private readonly IPlexPlayerManager _playerManager;
    private readonly BehaviorSubject<PlayState> _playState = new(PlayState.Stopped);
    private readonly BehaviorSubject<int?> _ratingKey = new(null);
    private readonly IPlexServerManager _serverManager;
    private readonly IPlexSettingsManager _settingsManager;
    private readonly IPlexTokenStore _tokenStore;

    public PlexServer(
        string machineIdentifier,
        IPlexServerManager serverManager,
        IPlexTokenStore tokenStore,
        IPlexSettingsManager settingsManager,
        IPlexPlayerManagerFactory playerManagerFactory,
        HttpClient httpClient,
        ILogger<PlexServer> logger
    )
    {
        this.MachineIdentifier = machineIdentifier;
        this._serverManager = serverManager;
        this._fileName = $"plex_{Convert.ToBase64String(Encoding.UTF8.GetBytes(machineIdentifier)).TrimEnd('=')}.json";
        this._tokenStore = tokenStore;
        this._httpClient = httpClient;
        this._settingsManager = settingsManager;
        this._logger = logger;
        this._connection = new(this.ConnectAsync, true);
        this._playerManager = playerManagerFactory.Create(this);
        this._disposable = new(
            this._activeMedia,
            this._isConnected,
            this._isDisposed,
            this._playState,
            this._ratingKey,
            this._playerManager
        );
        this._ratingKey
            .TakeUntil(this.CancellationToken)
            .DistinctUntilChanged()
            .Select(value => value is { } key and > 0
                ? Observable.FromAsync((cancellationToken) => this.QueryMediaItemAsync(key, cancellationToken))
                : Observable.Return(default(MediaItem?)))
            .Switch()
            .Subscribe(this._activeMedia);
    }

    public MediaItem? ActiveMedia => this._activeMedia.Value;
    public IObservable<MediaItem?> ActiveMediaChanged => this._activeMedia;
    public PlexServerData Data => this._serverManager.TryGetServerData(this.MachineIdentifier) is { } data ? data : default;
    public IObservable<Unit> Disposed => this._isDisposed;
    public bool IsConnected => this._isConnected.Value;
    public IObservable<bool> IsConnectedChanged => this._isConnected;
    public string MachineIdentifier { get; }
    public PlayState PlayState => this._playState.Value;
    public IObservable<PlayState> PlayStateChanged => this._playState;
    private IPAddress IPAddress => this.Data.IPAddress;
    public string Name => this.Data.Name;
    public int Port => this.Data.Port;
    public PlayerStatus? SelectedPlayer => this._playerManager.SelectedPlayer;
    public IObservable<PlayerStatus?> SelectedPlayerChanged => this._playerManager.SelectedPlayerChanged;
    public string? SelectedPlayerName => this.SelectedPlayer is { Player.Name: { } name } ? name : null;
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

    public async Task<PlayerStatus[]> ListPlayersAsync(bool refresh, CancellationToken cancellationToken)
    {
        if (refresh)
        {
            await this._playerManager.RefreshAsync(cancellationToken).ConfigureAwait(false);
        }
        return [.. this._playerManager.Players.Values];
    }

    public async Task<PlexPlayerData[]> DiscoverPlayersAsync(CancellationToken cancellationToken)
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
            select new PlexPlayerData(
                client.Name,
                client.MachineIdentifier,
                client.ProtocolCapabilities,
                IPAddress.Parse(client.Address),
                client.Port,
                client.Product
            )
        ];
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
            cancellationToken
        );
    }

    Task IPlexServer.SelectPlayerAsync(string playerIdentifier, CancellationToken cancellationToken) => this.SelectPlayerAsync(playerIdentifier, cancellationToken);

    private async Task<PlexPlayerData?> SelectPlayerAsync(string playerIdentifier, CancellationToken cancellationToken)
    {
        this._logger.LogInformation("Selecting player {Identifier}", playerIdentifier);
        if (await ResolvePlayerAsync().ConfigureAwait(false) is not { } player)
        {
            return default;
        }
        if (this._playerManager.SelectedPlayerId != playerIdentifier)
        {
            this._settingsManager.WriteAllBytes(this._fileName, JsonSerializer.SerializeToUtf8Bytes(player, JsonSerializerOptions.Web));
        }
        this._playerManager.SetSelectedPlayer(player);
        return player;

        async ValueTask<PlexPlayerData?> ResolvePlayerAsync()
        {
            if (this._playerManager.GetPlayerData(playerIdentifier) is { } data)
            {
                return data;
            }
            PlexPlayerData[] players = await this.DiscoverPlayersAsync(cancellationToken).ConfigureAwait(false);
            int index = Array.FindIndex(players, player => player.MachineIdentifier == playerIdentifier);
            return index == -1 ? default(PlexPlayerData?) : players[index];
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
                await this._playerManager.InitializeAsync().ConfigureAwait(false);
                this._initialization.TrySetResult();
                if (this._playerManager.SelectedPlayerId is null && this._settingsManager.HasFile(this._fileName) && this._settingsManager.ReadAllBytes(this._fileName) is { } bytes)
                {
                    this._playerManager.SetSelectedPlayer(JsonSerializer.Deserialize<PlexPlayerData>(bytes, JsonSerializerOptions.Web));
                }
                await JsonWebSocket.MessageLoop<ServerMessage>(
                    webSocket,
                    (message, _) => message.Container switch
                    {
                        { Type: ServerNotificationType.Playing, Notifications: { Length: > 0 } notifications } => this.ProcessNotificationsAsync(notifications),
                        _ => Task.CompletedTask
                    },
                    cancellationToken: this.CancellationToken
                ).ConfigureAwait(false);
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
        string clientId;
        if (this.SelectedPlayer is ({ MachineIdentifier: { } playerId }, _))
        {
            clientId = playerId;
        }
        else if (this._playerManager.GetPlayerData(clientId = notifications[0].ClientIdentifier) is { } data)
        {
            await this.SelectPlayerAsync(clientId, this.CancellationToken).ConfigureAwait(false);
        }
        else
        {
            return;
        }
        foreach (PlayStateNotification notification in notifications)
        {
            if (notification.ClientIdentifier != clientId)
            {
                continue;
            }
            this._playState.OnNext(notification.State);
            this._ratingKey.OnNext(notification.RatingKey);
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

    private async Task SendPlayerCommandAsync(string command, (string, string)[]? queryParameters = null, CancellationToken cancellationToken = default)
    {
        if (this.SelectedPlayer is not { Player.MachineIdentifier: { } machineIdentifier })
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
}
