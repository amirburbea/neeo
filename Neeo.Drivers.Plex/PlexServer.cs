using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Net.Http;
using System.Net.WebSockets;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Subjects;
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
    IMediaLibrary Library { get; }
    string MachineIdentifier { get; }
    string Name { get; }
    PlayState PlayState { get; }
    IObservable<PlayState> PlayStateChanged { get; }
    PlayerData? SelectedPlayer { get; }
    IObservable<PlayerData?> SelectedPlayerChanged { get; }
    string? SelectedPlayerName { get; }
    ServerData ServerData { get; }

    Task<PlayerData[]> GetPlayersAsync(bool refresh = false, CancellationToken cancellationToken = default);

    Task InitializeAsync(CancellationToken cancellationToken = default);

    Task PlayMediaAsync(MediaItem item, CancellationToken cancellationToken = default);

    Task PlayMediaAsync(int ratingKey, CancellationToken cancellationToken = default);

    Task SeekBackAsync(CancellationToken cancellationToken = default);

    Task SeekForwardAsync(CancellationToken cancellationToken = default);

    Task SelectPlayerAsync(string playerIdentifier, CancellationToken cancellationToken = default);

    Task SendPlaybackCommandAsync(PlaybackCommand command, CancellationToken cancellationToken = default);

    Task TogglePlayAsync(CancellationToken cancellationToken = default);
}

internal sealed partial class PlexServer : IPlexServer, IDisposable
{
    private readonly BehaviorSubject<MediaItem?> _activeMedia = new(null);
    private readonly CancellationTokenSource _cancellationTokenSource = new();
    private readonly CompositeDisposable _disposable;
    private readonly HttpClient _httpClient;
    private readonly TaskCompletionSource _initializationTaskSource = new();
    private readonly BehaviorSubject<bool> _isConnected = new(false);
    private readonly Subject<Unit> _isDisposed = new();
    private readonly MediaLibrary _library;
    private readonly ILogger _logger;
    private readonly Lazy<IDisposable> _playerDiscoverySubscription;
    private readonly TaskCompletionSource _playerDiscoveryTaskSource = new();
    private readonly BehaviorSubject<ImmutableDictionary<string, PlayerData>> _players = new([]);
    private readonly BehaviorSubject<PlayState> _playState = new(PlayState.Stopped);
    private readonly BehaviorSubject<int?> _ratingKey = new(null);
    private readonly BehaviorSubject<string?> _selectedPlayerId = new(null);
    private readonly Func<ServerData> _serverDataLookup;
    private readonly ISettingsStore _settings;
    private readonly ReconnectLoop _reconnectLoop;
    private readonly Lock _sessionLock = new();
    private readonly Lazy<Task> _socketTask;
    private readonly Lazy<Task> _timelineHeartbeatTask;
    private readonly IPlexTokenStore _tokenStore;
    private int _commandId;
    private Guid _sessionId;
    private Task _sessionTask = Task.CompletedTask;

    public PlexServer(
        string machineIdentifier,
        HttpClient httpClient,
        ISettingsStore settings,
        IPlexTokenStore tokenStore,
        Func<ServerData> lookupServerData,
        ILogger<PlexServer> logger
    )
    {
        this.MachineIdentifier = machineIdentifier;
        this._serverDataLookup = lookupServerData;
        this._tokenStore = tokenStore;
        this._httpClient = httpClient;
        this._settings = settings;
        this._logger = logger;
        this._library = new(this);
        this._disposable = new(
            this._activeMedia,
            this._isConnected,
            this._playState,
            this._ratingKey,
            this._players,
            this._selectedPlayerId,
            this._cancellationTokenSource,
            this._isDisposed
        );
        this._ratingKey
            .DistinctUntilChanged()
            .Select(value => value is { } key and > 0
                ? Observable.FromAsync(token => this.TryQueryMediaItemAsync(key, token))
                : Observable.Return(default(MediaItem?)))
            .Switch()
            .TakeUntil(this._cancellationTokenSource.Token)
            .Subscribe(
                this._activeMedia.OnNext,
                exception => this._logger.LogError(exception, "Plex active media updates stopped unexpectedly for server {MachineIdentifier}.", this.MachineIdentifier),
                this._activeMedia.OnCompleted
            );
        this.SelectedPlayerChanged = this._selectedPlayerId
            .CombineLatest(this._players, (id, players) => PlexServer.GetPlayerById(players, id))
            .DistinctUntilChanged()
            .TakeUntil(this._cancellationTokenSource.Token);
        this._playerDiscoverySubscription = new(
            () => Observable.Interval(TimeSpan.FromSeconds(30d))
                .StartWith(0L)
                .Select(_ => Observable.FromAsync(this.TryDiscoverPlayersAsync))
                .Switch()
                .Do(_ => this._playerDiscoveryTaskSource.TrySetResult())
                .TakeUntil(this._cancellationTokenSource.Token)
                .Subscribe(
                    static _ => { },
                    exception => this._logger.LogError(exception, "Plex player discovery stopped unexpectedly for server {MachineIdentifier}.", this.MachineIdentifier)
                ),
            LazyThreadSafetyMode.ExecutionAndPublication
        );
        this._reconnectLoop = new(TimeSpan.FromSeconds(5d), TimeSpan.FromMinutes(2d), this.TryConnectSocketAsync, logger);
        this._socketTask = new(this.ConnectSocketAsync, LazyThreadSafetyMode.ExecutionAndPublication);
        this._timelineHeartbeatTask = new(() => this.PollTimelineHeartbeatAsync(this._cancellationTokenSource.Token), LazyThreadSafetyMode.ExecutionAndPublication);
    }

    public MediaItem? ActiveMedia => this._activeMedia.Value;
    public IObservable<MediaItem?> ActiveMediaChanged => this._activeMedia;
    public IObservable<Unit> Disposed => this._isDisposed;
    public string HostName => $"{this.IPAddress.Replace('.', '-')}.{this.MachineIdentifier}.plex.direct";
    public bool IsConnected => this._isConnected.Value;
    public IObservable<bool> IsConnectedChanged => this._isConnected;
    public IMediaLibrary Library => this._library;
    public string MachineIdentifier { get; }
    public PlayState PlayState => this._playState.Value;
    public IObservable<PlayState> PlayStateChanged => this._playState;
    public PlayerData? SelectedPlayer => PlexServer.GetPlayerById(this._players.Value, this._selectedPlayerId.Value);
    public IObservable<PlayerData?> SelectedPlayerChanged { get; }
    public string? SelectedPlayerName => this.SelectedPlayer?.Name;
    public ServerData ServerData => this._serverDataLookup();
    public string IPAddress => this.ServerData.IPAddress;
    public string Name => this.ServerData.Name;
    public int Port => this.ServerData.Port;

    public async Task DiscoverPlayersAsync(CancellationToken cancellationToken)
    {
        if (await this._httpClient.GetAsync(this.GetUri("clients"), PlexJsonSerializerContext.Default.ResponseClientsMediaContainer, this.AddTokenHeaders, cancellationToken).ConfigureAwait(false) is not { MediaContainer.Clients: { } clients })
        {
            return;
        }
        ClientServer[] onlineClients = [.. clients.Where(client => client.ProtocolVersion == 1 && (client.ProtocolCapabilities & PlayerCapabilities.Playback) == PlayerCapabilities.Playback)];
        ImmutableDictionary<string, PlayerData> previous = this._players.Value;
        if (previous.Count == 0 && onlineClients.Length == 0)
        {
            return;
        }
        Dictionary<string, PlayerData> onlinePlayers = onlineClients.ToDictionary(
            client => client.MachineIdentifier,
            client => new PlayerData(
                client.Name,
                client.MachineIdentifier,
                client.ProtocolCapabilities,
                client.Address,
                client.Port,
                client.Product
            )
        );
        if (onlinePlayers.Keys.All(previous.ContainsKey) &&
            previous.Values.All(player => !onlinePlayers.TryGetValue(player.MachineIdentifier, out PlayerData onlinePlayer) ? !player.IsOnline : onlinePlayer.Equals(player)))
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
    }

    /// <summary>
    /// Discovers players for the background poll: a failure (the server being briefly unreachable, say) is logged and
    /// the next poll tries again, rather than terminating the poll.
    /// </summary>
    internal async Task TryDiscoverPlayersAsync(CancellationToken cancellationToken)
    {
        try
        {
            await this.DiscoverPlayersAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception e)
        {
            this._logger.LogWarning("Failed to discover Plex players on server {MachineIdentifier}: {Message}", this.MachineIdentifier, e.Message);
        }
    }

    /// <summary>
    /// Looks up a media item for the active media pipeline: a failure is logged and treated as "no media" rather than
    /// terminating the pipeline.
    /// </summary>
    internal async Task<MediaItem?> TryQueryMediaItemAsync(int ratingKey, CancellationToken cancellationToken)
    {
        try
        {
            return await this.QueryMediaItemAsync(ratingKey, cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception e)
        {
            this._logger.LogWarning("Failed to query Plex media item {RatingKey} on server {MachineIdentifier}: {Message}", ratingKey, this.MachineIdentifier, e.Message);
            return null;
        }
    }

    public void Dispose()
    {
        Task sessionTask;
        using (this._sessionLock.EnterScope())
        {
            this._cancellationTokenSource.Cancel();
            sessionTask = this._sessionTask;
        }
        this._reconnectLoop.Dispose();
        if (this._playerDiscoverySubscription.IsValueCreated)
        {
            this._playerDiscoverySubscription.Value.Dispose();
        }
        if (this._socketTask.IsValueCreated)
        {
            this._socketTask.Value.Wait();
        }
        sessionTask.Wait();
        if (this._timelineHeartbeatTask.IsValueCreated)
        {
            this._timelineHeartbeatTask.Value.Wait();
        }
        this._isDisposed.OnNext(default);
        this._disposable.Dispose();
    }

    public async Task<PlayerData[]> GetPlayersAsync(bool refresh, CancellationToken cancellationToken)
    {
        if (refresh)
        {
            await this.DiscoverPlayersAsync(cancellationToken).ConfigureAwait(false);
        }
        return [.. this._players.Value.Values];
    }

    public Task InitializeAsync(CancellationToken cancellationToken)
    {
        _ = this._socketTask.Value;
        _ = this._playerDiscoverySubscription.Value;
        _ = this._timelineHeartbeatTask.Value;
        return this._initializationTaskSource.Task.WaitAsync(cancellationToken);
    }

    public async Task PlayMediaAsync(MediaItem media, CancellationToken cancellationToken)
    {
        MediaItemMetadata? playingMedia = await this.QueryPlayerSessionAsync(cancellationToken).ConfigureAwait(false);
        // Twice, send a stop command followed by a 500ms wait, which hopefully is enough time to
        // guarantee the player responds.
        for (int times = 0; times < 2; times++)
        {
            await this.SendPlaybackCommandAsync(PlaybackCommand.Stop, playingMedia?.Type ?? (this._activeMedia.Value ?? media).Type, cancellationToken).ConfigureAwait(false);
            await Task.Delay(TimeSpan.FromSeconds(0.5d), cancellationToken).ConfigureAwait(false);
        }
        await this.SendPlayerCommandAsync(
            "playback/playMedia",
            [
                ("type", TextAttribute.GetText(media.Type)),
                ("providerIdentifier", "com.plexapp.plugins.library"),
                ("key", $"/library/metadata/{media.RatingKey}"),
                ("offset", "0"),
                ("machineIdentifier", this.MachineIdentifier),
                ("protocol", "https"),
                ("address", this.HostName),
                ("port", $"{this.Port}"),
                ("X-Plex-Model", "standalone"),
                ("X-Plex-Product", "Plex Remote for NEEO"),
                ("X-Plex-Features", "external-media,indirect-media,hub-style-list"),
                ("X-Plex-Session-Id", $"{this._sessionId}"),
                ("X-Plex-Playback-Session-Id", $"{Guid.NewGuid()}"),
                ("X-Plex-Playback-Id", $"{Guid.NewGuid()}"),
            ],
            cancellationToken
        ).ConfigureAwait(false);
    }

    public async Task PlayMediaAsync(int ratingKey, CancellationToken cancellationToken)
    {
        if (await this.QueryMediaItemAsync(ratingKey, cancellationToken).ConfigureAwait(false) is { } media)
        {
            await this.PlayMediaAsync(media, cancellationToken).ConfigureAwait(false);
        }
    }

    public Task SeekBackAsync(CancellationToken cancellationToken) => this.SeekAsync(-30, cancellationToken);

    public Task SeekForwardAsync(CancellationToken cancellationToken) => this.SeekAsync(30, cancellationToken);

    public async Task SendPlaybackCommandAsync(PlaybackCommand command, CancellationToken cancellationToken)
    {
        if (this._activeMedia.Value is { Type: { } type })
        {
            await this.SendPlaybackCommandAsync(command, type, cancellationToken).ConfigureAwait(false);
        }
    }

    public async Task TogglePlayAsync(CancellationToken cancellationToken)
    {
        await ToggleStateAsync(await this.QueryPlayerSessionAsync(cancellationToken).ConfigureAwait(false) switch
        {
            { Player.State: { } state } => state,
            _ => this._playState.Value,
        }).ConfigureAwait(false);

        Task ToggleStateAsync(PlayState state) => this.SendPlaybackCommandAsync(
            state is PlayState.Playing or PlayState.Buffering ? PlaybackCommand.Pause : PlaybackCommand.Play,
            cancellationToken
        );
    }

    private static MediaCategory GetMediaCategory(MediaType type) => type switch
    {
        MediaType.Episode or MediaType.Movie or MediaType.Video or MediaType.Show => MediaCategory.Video,
        MediaType.Music or MediaType.Album or MediaType.Track => MediaCategory.Music,
        MediaType.Photo => MediaCategory.Photo,
        _ => throw new NotSupportedException(),
    };

    private Uri GetUri(string path, params (string Key, string Value)[] queryParameters) => this.GetUri(Uri.UriSchemeHttps, path, queryParameters);

    /// <summary>
    /// Builds a URI targeting the player's own local address, rather than the Plex server. Direct
    /// client control is what the official Plex apps use by default (server-relayed control depends on
    /// the client keeping an active Companion session with the server, which is unreliable in practice).
    /// </summary>
    private static Uri GetPlayerUri(PlayerData player, string path, (string Key, string Value)[] queryParameters) => PlexServer.BuildUri(Uri.UriSchemeHttp, player.IPAddress, player.Port, path, queryParameters);

    Task IPlexServer.SelectPlayerAsync(string playerIdentifier, CancellationToken cancellationToken) => this.SelectPlayerAsync(playerIdentifier, cancellationToken);

    private static PlayerData? GetPlayerById(ImmutableDictionary<string, PlayerData> players, string? playerId)
    {
        return playerId != null && players.TryGetValue(playerId, out PlayerData player) ? player : null;
    }

    private void AddTokenHeaders(HttpRequestMessage request)
    {
        request.Headers.Add("X-Plex-Token", this._tokenStore.AuthToken);
        request.Headers.Add("X-Plex-Client-Identifier", this._tokenStore.ClientIdentifier);
    }

    private async Task<LibraryMediaContainer> BrowseLibraryAsync(string? suffix, PaginationParameters? pagination, CancellationToken cancellationToken)
    {
        Response<LibraryMediaContainer> response = await this._httpClient.GetAsync(
            this.GetUri(
                $"library/sections{(suffix == null ? string.Empty : $"/{suffix}")}",
                pagination is { } parameters
                    ? [("X-Plex-Container-Start", $"{parameters.Offset}"), ("X-Plex-Container-Size", $"{parameters.PageSize}")]
                    : []
            ),
            PlexJsonSerializerContext.Default.ResponseLibraryMediaContainer,
            this.AddTokenHeaders,
            cancellationToken
        ).ConfigureAwait(false);
        return response.MediaContainer;
    }

    /// <summary>
    /// Periodically pokes the selected player with a harmless <c>timeline/poll</c> request while it's
    /// selected. Some Plex clients silently let their Companion session go stale if nothing is sent to
    /// them for a while (a known Plex-side quirk, not something we can fix), which then makes them stop
    /// responding to real commands until something happens to revive the session. This keeps it warm.
    /// </summary>
    private async Task PollTimelineHeartbeatAsync(CancellationToken cancellationToken)
    {
        try
        {
            using PeriodicTimer timer = new(TimeSpan.FromSeconds(75d));
            while (await timer.WaitForNextTickAsync(cancellationToken).ConfigureAwait(false))
            {
                if (this.SelectedPlayer is { })
                {
                    await this.SendPlayerCommandAsync("timeline/poll", [("wait", "0")], cancellationToken).ConfigureAwait(false);
                }
            }
        }
        catch (OperationCanceledException)
        {
            // Expected during shutdown.
        }
    }

    private async Task ConnectSocketAsync()
    {
        CancellationToken cancellationToken = this._cancellationTokenSource.Token;
        bool connected = false;
        try
        {
            connected = await this.TryConnectSocketAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            return;
        }
        catch (Exception e)
        {
            this._logger.LogError(e, "Error on Plex server: {Name}", this.Name);
        }
        finally
        {
            if (!connected)
            {
                // Let initialization complete (disconnected) rather than wait for a reconnect.
                this._initializationTaskSource.TrySetResult();
            }
        }
        if (!connected)
        {
            this._reconnectLoop.Start(cancellationToken: cancellationToken);
        }
    }

    /// <summary>
    /// A single connection attempt (also the <see cref="ReconnectLoop"/> callback). On success, the session runs in the
    /// background until the socket closes, at which point the reconnect loop is started.
    /// </summary>
    private async Task<bool> TryConnectSocketAsync(CancellationToken cancellationToken)
    {
        Uri uri = this.GetUri(Uri.UriSchemeWss, ":/websockets/notifications");
        this._logger.LogInformation("Connecting to Plex server '{Name}'...", this.Name);
        // Disable keep-alive pings, Plex server does not support them.
        ClientWebSocket webSocket = new() { Options = { KeepAliveInterval = Timeout.InfiniteTimeSpan } };
        HttpMessageInvoker messageInvoker = new(PlexDirectConnect.CreateHttpHandler(), disposeHandler: true);
        try
        {
            webSocket.Options.SetRequestHeader("X-Plex-Token", this._tokenStore.AuthToken);
            await webSocket.ConnectAsync(uri, messageInvoker, cancellationToken).ConfigureAwait(false);
            // The session is bound to the server's lifetime, not to this attempt's token (which the reconnect loop
            // cancels once an attempt succeeds).
            CancellationToken sessionToken = this._cancellationTokenSource.Token;
            using (this._sessionLock.EnterScope())
            {
                // Checked under the lock Dispose cancels under, so a session is never started after Dispose waited for it.
                sessionToken.ThrowIfCancellationRequested();
                this._sessionTask = this.RunSessionAsync(webSocket, messageInvoker, sessionToken);
            }
            return true;
        }
        catch (Exception e)
        {
            webSocket.Dispose();
            messageInvoker.Dispose();
            if (e is WebSocketException)
            {
                this._logger.LogError(e, "Error on Plex server: {Name}", this.Name);
                return false;
            }
            throw;
        }
    }

    private async Task RunSessionAsync(ClientWebSocket webSocket, HttpMessageInvoker messageInvoker, CancellationToken cancellationToken)
    {
        await Task.Yield(); // Return to the caller as soon as the session is started.
        try
        {
            this._sessionId = Guid.NewGuid();
            this._logger.LogInformation("Connected to Plex server '{Name}'", this.Name);
            this._isConnected.OnNext(true);
            await this._playerDiscoveryTaskSource.Task.WaitAsync(cancellationToken).ConfigureAwait(false);
            this._initializationTaskSource.TrySetResult();
            if (this._selectedPlayerId.Value is null &&
                this._settings.TryGetValue(this.MachineIdentifier, out string? playerJson) &&
                JsonSerializer.Deserialize<PlayerData>(playerJson, PlexJsonSerializerOptions.Default) is { MachineIdentifier.Length: > 0 } player)
            {
                // If the player is not in the players dictionary, add it as offline.
                if (this._players.Value is { } players && !players.ContainsKey(player.MachineIdentifier))
                {
                    this._players.OnNext(players.Add(player.MachineIdentifier, player with { IsOnline = false }));
                }
                this._selectedPlayerId.OnNext(player.MachineIdentifier);
            }
            await JsonWebSocket.MessageLoop(webSocket, ProcessMessageAsync, PlexJsonSerializerContext.Default.ServerMessage, cancellationToken: cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            // Expected during shutdown.
        }
        catch (Exception e)
        {
            this._logger.LogError(e, "Error on Plex server: {Name}", this.Name);
        }
        finally
        {
            webSocket.Dispose();
            messageInvoker.Dispose();
            this._isConnected.OnNext(false);
            this._initializationTaskSource.TrySetResult();
        }
        // Whether the server closed the socket cleanly or it failed, reconnect with backoff (a no-op once disposed).
        this._reconnectLoop.Start(cancellationToken: cancellationToken);

        async ValueTask ProcessMessageAsync(ServerMessage message, CancellationToken cancellationToken)
        {
            if (message.Container is not { Type: ServerNotificationType.Playing, Notifications: { } element })
            {
                return;
            }
            if (element.Deserialize<PlayStateNotification[]>(PlexJsonSerializerOptions.Default) is not { Length: > 0 } array)
            {
                this._logger.LogDebug("Received empty playing notification from Plex server '{Name}'", this.Name);
                return;
            }
            int index = Array.FindIndex(array, notification => notification.ClientIdentifier == this._selectedPlayerId.Value);
            if (index is -1)
            {
                return;
            }
            PlayStateNotification notification = array[index];
            this._ratingKey.OnNext(notification.RatingKey);
            this._playState.OnNext(notification.State);
        }
    }

    private MediaItem CreateMediaItem(MediaItemMetadata metadata) => new(
        metadata.RatingKey,
        metadata.Type,
        metadata.Title,
        metadata.Summary,
        metadata.Thumbnail is { } url ? this.GetImageUri(url) : null
    );

    private Uri GetImageUri(string url) => this.GetUri($"photo/:/transcode", ("url", url));

    private Uri GetUri(string scheme, string path, params (string Key, string Value)[] queryParameters) => PlexServer.BuildUri(scheme, this.HostName, this.Port, path, queryParameters);

    private static Uri BuildUri(string scheme, string host, int port, string path, (string Key, string Value)[] queryParameters)
    {
        UriBuilder builder = new(scheme, host, port, path);
        if (queryParameters.Length != 0)
        {
            builder.Query = string.Join('&', queryParameters.Select(entry => $"{entry.Key}={Uri.EscapeDataString(entry.Value)}"));
        }
        return builder.Uri;
    }

    private async Task<MediaItem?> QueryMediaItemAsync(int ratingKey, CancellationToken cancellationToken)
    {
        Response<MediaItemDetailContainer> response = await this._httpClient.GetAsync(
            this.GetUri($"library/metadata/{ratingKey}"),
            PlexJsonSerializerContext.Default.ResponseMediaItemDetailContainer,
            this.AddTokenHeaders,
            cancellationToken
        ).ConfigureAwait(false);
        return response.MediaContainer.Metadata is [{ } metadata, ..] ? this.CreateMediaItem(metadata) : null;
    }

    private async Task<MediaItemMetadata?> QueryPlayerSessionAsync(CancellationToken cancellationToken = default)
    {
        if (this._selectedPlayerId.Value is not { } id)
        {
            return null;
        }
        Response<MediaItemDetailContainer> response = await this._httpClient.GetAsync(
            this.GetUri("status/sessions"),
            PlexJsonSerializerContext.Default.ResponseMediaItemDetailContainer,
            this.AddTokenHeaders,
            cancellationToken
        ).ConfigureAwait(false);
        return response.MediaContainer.Metadata is { } array && Array.FindIndex(array, media => media.Player?.MachineIdentifier == id) is int index and not -1
            ? array[index]
            : null;
    }

    private void SaveSelectedPlayer(PlayerData player) =>
        this._settings[this.MachineIdentifier] = JsonSerializer.Serialize(player, PlexJsonSerializerOptions.Default);

    private async Task SeekAsync(int seconds, CancellationToken cancellationToken)
    {
        if (await this.QueryPlayerSessionAsync(cancellationToken).ConfigureAwait(false) is not { ViewOffset: { } offset, Type: { } type, Duration: { } duration })
        {
            return;
        }
        await this.SendPlayerCommandAsync(
            "playback/seekTo",
            [
                ("type", TextAttribute.GetText(PlexServer.GetMediaCategory(type))),
                ("offset", $"{Math.Min(duration, Math.Max(0, offset + seconds * 1000))}")
            ],
            cancellationToken: cancellationToken
        ).ConfigureAwait(false);
    }

    private async Task<PlayerData?> SelectPlayerAsync(string playerIdentifier, CancellationToken cancellationToken)
    {
        this._logger.LogInformation("Selecting player {Identifier}", playerIdentifier);
        if (!this._players.Value.TryGetValue(playerIdentifier, out PlayerData player))
        {
            await this.DiscoverPlayersAsync(cancellationToken).ConfigureAwait(false);
            if (!this._players.Value.TryGetValue(playerIdentifier, out player))
            {
                this._logger.LogWarning("Can not select player {Id} - not found.", playerIdentifier);
                return null;
            }
        }
        if (this._selectedPlayerId.Value != playerIdentifier)
        {
            this.SaveSelectedPlayer(player);
            this._selectedPlayerId.OnNext(playerIdentifier);
        }
        return player;
    }

    private Task SendPlaybackCommandAsync(PlaybackCommand command, MediaType type, CancellationToken cancellationToken) => this.SendPlayerCommandAsync(
        $"playback/{TextAttribute.GetText(command)}",
        [("type", TextAttribute.GetText(PlexServer.GetMediaCategory(type)))],
        cancellationToken: cancellationToken
    );

    private async Task SendPlayerCommandAsync(string command, (string, string)[]? queryParameters, CancellationToken cancellationToken)
    {
        if (this.SelectedPlayer is not { Capabilities: { } capabilities, MachineIdentifier: { } targetId } player)
        {
            return;
        }
        if (TextAttribute.GetEnum<PlayerCapabilities>(command[..command.IndexOf('/')]) is { } capability && (capabilities & capability) != capability)
        {
            this._logger.LogWarning("Player does not support {Capability}, errors may occur.", Enum.GetName(capability));
        }
        (string, string)[] allParameters = [
            .. queryParameters ?? [],
            ("commandID", $"{Interlocked.Increment(ref this._commandId)}"),
            ("X-Plex-Target-Client-Identifier", targetId)
        ];
        // Prefer talking to the player directly - this is what the official Plex apps do by default, and
        // it doesn't depend on the client keeping an active (and unreliable in practice) Companion
        // session with the server alive. Fall back to relaying through the server if that fails (e.g.
        // the client isn't reachable directly on this network).
        if (await this.TrySendCommandAsync(PlexServer.GetPlayerUri(player, $"player/{command}", allParameters), command, cancellationToken).ConfigureAwait(false))
        {
            return;
        }
        await this.TrySendCommandAsync(this.GetUri($"player/{command}", allParameters), command, cancellationToken).ConfigureAwait(false);
    }

    private async Task<bool> TrySendCommandAsync(Uri uri, string command, CancellationToken cancellationToken)
    {
        try
        {
            using HttpRequestMessage request = new(HttpMethod.Get, uri);
            this.AddTokenHeaders(request);
            using HttpResponseMessage response = await this._httpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);
            if (response.IsSuccessStatusCode)
            {
                return true;
            }
            string text = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
            this._logger.LogWarning("Plex command ({Command}) failed via {Host}: {Code}.\n{Text}", command, uri.Host, response.StatusCode, text);
        }
        catch (Exception e)
        {
            this._logger.LogWarning("Plex command ({Command}) failed via {Host}: {Error}", command, uri.Host, e);
        }
        return false;
    }
}
